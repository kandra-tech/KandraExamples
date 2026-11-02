using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Constants;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Domain.Exceptions;
using Kandra.Forms.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Kandra.Printing;
using HomeUtilities.Application.Common;
using HomeUtilities.Application.Constants;
using HomeUtilities.Application.Services;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Domain.Entities.Documents;
using HomeUtilities.Domain.Entities.Registers;
using HomeUtilities.Domain.Numbering;
using HomeUtilities.Forms.Documents;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomeUtilities.Application.Documents;

[KandraDocumentBehavior(typeof(InvoiceDto))]
[RegisterTransactions(typeof(SettlementMovement))]
[RegisterTransactions(typeof(UtilityCostMovement))]
public class InvoiceBehavior(
    ISimpleNumberingSystem numbering,
    IEntityRepository<Invoice> invoicesRepository,
    IEntityRepository<Provider> providersRepository,
    IEntityRepository<Property> propertiesRepository,
    IConstantsService constants,
    IStringLocalizer localizer,
    ICurrentTimeZone? currentTimeZone = null,
    IAuthorizationChecker? authorizationChecker = null) : IDocumentBehavior<Invoice>
{
    public async ValueTask OnNewAsync(Invoice entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        var localDate = TimeZoneHelper.ToLocalDate(entity.Date, currentTimeZone?.TimeZone);
        var yy = localDate.ToString("yy");
        entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.Invoice}:{yy}",
            raw => $"INV-{yy}-{raw:D4}",
            requestorTag: "Invoice:OnNew",
            cancellationToken: cancellationToken);
        entity.Month = TimeZoneHelper.LocalMonthBoundsUtc(entity.Date, currentTimeZone?.TimeZone).MonthStartUtc;
    }

    public async ValueTask OnSaveAsync(Invoice entity, bool isInsert, CancellationToken cancellationToken)
    {
        // Month is the instant the billing month starts in the user's time zone: it survives the client's UTC <-> local
        // conversion and normalising it again is a no-op. The month itself is MonthKey (the 1st, UTC kind).
        entity.Month = TimeZoneHelper.LocalMonthBoundsUtc(entity.Month, currentTimeZone?.TimeZone).MonthStartUtc;

        // The provider's invoice is what counts: an amount the user typed stays; only an empty one is computed.
        foreach (var line in entity.Lines.Where(l => l.Amount == 0m))
        {
            line.Amount = Math.Round(line.Quantity * line.Price, 2);
        }
        entity.TotalAmount = entity.Lines.Sum(l => l.Amount);

        if (string.IsNullOrWhiteSpace(entity.PaymentDescription) && entity.ProviderId != Guid.Empty && entity.PropertyId != Guid.Empty)
        {
            var prov = await providersRepository.GetAsync(entity.ProviderId, false, cancellationToken);
            var prop = await propertiesRepository.GetAsync(entity.PropertyId, false, cancellationToken);
            entity.PaymentDescription = PaymentDescriptionFormatter.Format(
                prov?.PaymentDescriptionTemplate,
                prov,
                prop,
                MonthKey(entity.Month),
                localizer,
                entity.Code);
        }
    }

    public async ValueTask OnSubmitAsync(Invoice entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var normMonth = MonthKey(entity.Month);
        var duplicateExists = false;
        await foreach (var other in invoicesRepository.GetAllAsync(q => q.Where(i =>
            i.Id != entity.Id &&
            i.IsActive &&
            i.ProviderId == entity.ProviderId &&
            i.PropertyId == entity.PropertyId)))
        {
            duplicateExists |= MonthKey(other.Month) == normMonth;
        }

        if (duplicateExists)
        {
            throw new InvalidStateException(localizer["DuplicateInvoiceForMonth", normMonth.ToString("yyyy-MM")].Value);
        }

        entity.TotalAmount = entity.Lines.Sum(l => l.Amount);

        // 1. Settlements (+TotalAmount to provider)
        var settlementMovements = new List<SettlementMovement>
        {
            new()
            {
                LineId = Guid.Empty,
                IsExpense = false,
                Dimensions = new SettlementDimensions
                {
                    ProviderId = entity.ProviderId,
                    PropertyId = entity.PropertyId,
                },
                Resources = new SettlementResources
                {
                    Amount = entity.TotalAmount,
                },
            }
        };

        var settlementsWriter = registers.GetWriter<IBalanceRegisterWriter<SettlementMovement, SettlementBalance>>();
        await settlementsWriter.PostAsync(entity.Id, entity.Date, settlementMovements, cancellationToken);

        // 2. Utility costs (posted to the normalized billing month)
        var costMovements = entity.Lines.Select(line => new UtilityCostMovement
        {
            LineId = line.Id,
            IsExpense = false,
            Dimensions = new UtilityCostDimensions
            {
                PropertyId = entity.PropertyId,
                ServiceId = line.ServiceId,
                Zone = line.Zone,
            },
            Resources = new UtilityCostResources
            {
                Quantity = line.Quantity,
                Amount = line.Amount,
            },
        }).ToList();

        var costsWriter = registers.GetWriter<ITurnoverRegisterWriter<UtilityCostMovement>>();
        await costsWriter.PostAsync(entity.Id, normMonth, costMovements, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(Invoice entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var settlementsWriter = registers.GetWriter<IBalanceRegisterWriter<SettlementMovement, SettlementBalance>>();
        await settlementsWriter.UnpostAsync(entity.Id, cancellationToken);

        var costsWriter = registers.GetWriter<ITurnoverRegisterWriter<UtilityCostMovement>>();
        await costsWriter.UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var settlementsWriter = registers.GetWriter<IBalanceRegisterWriter<SettlementMovement, SettlementBalance>>();
        await settlementsWriter.DeleteAsync(id, cancellationToken);

        var costsWriter = registers.GetWriter<ITurnoverRegisterWriter<UtilityCostMovement>>();
        await costsWriter.DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(Invoice entity, ExportFormat format, string? layoutTag,
        ILookupRepository lookup, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var prov = await providersRepository.GetAsync(entity.ProviderId, false, cancellationToken);
        var providerName = prov?.Name ?? entity.ProviderId.ToString();

        var prop = await propertiesRepository.GetAsync(entity.PropertyId, false, cancellationToken);
        var propertyName = prop?.Name ?? entity.PropertyId.ToString();

        var serviceIds = entity.Lines.Select(l => l.ServiceId).Distinct().ToList();
        var services = (await lookup.GetDictionaryAsync<ServiceNode>(serviceIds, cancellationToken))
            .ToDictionary(s => s.Id, s => s.Name);

        var meterIds = entity.Lines.Where(l => l.MeterId.HasValue).Select(l => l.MeterId!.Value).Distinct().ToList();
        var meters = (await lookup.GetDictionaryAsync<Meter>(meterIds, cancellationToken))
            .ToDictionary(m => m.Id, m => m.Name);

        var currency = (await constants.GetAsync<CurrencyCodeConstant>(entity.Date, cancellationToken)).Value;

        var form = new InvoicePrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = authorizationChecker is not null
                ? await Reports.PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken)
                : null,
            ProviderName = providerName,
            ProviderIban = prov?.Iban,
            ProviderEdrpou = prov?.Edrpou,
            ProviderBankName = prov?.BankName,
            PropertyName = propertyName,
            PropertyOwner = prop?.Owner,
            BillingMonth = MonthKey(entity.Month).ToString("MMMM yyyy"),
            PaymentDescription = entity.PaymentDescription,
            Total = entity.TotalAmount,
            CurrencyCode = currency,
            Lines = entity.Lines.Select(l => new InvoicePrintLine
            {
                ServiceName = services.TryGetValue(l.ServiceId, out var sName) ? sName : l.ServiceId.ToString(),
                MeterName = l.MeterId.HasValue && meters.TryGetValue(l.MeterId.Value, out var mName) ? mName : null,
                Zone = l.Zone,
                Quantity = l.Quantity,
                Price = l.Price,
                Amount = l.Amount,
            }).ToList(),
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }

    /// <summary>The billing month as the register key: the 1st of the month in the user's time zone, UTC kind.</summary>
    private DateTime MonthKey(DateTime month) => TimeZoneHelper.ToMonthStartUtc(month, currentTimeZone?.TimeZone);
}

[Mapper]
public partial class InvoiceMapper : IEntityMapper<Invoice, InvoiceDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    [MapperIgnoreTarget(nameof(InvoiceDto.CalculateTrigger))]
    public partial InvoiceDto ToDto(Invoice entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreSource(nameof(InvoiceDto.CalculateTrigger))]
    public partial Invoice ToEntity(InvoiceDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial InvoiceLineDto ToLineDto(InvoiceLine entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial InvoiceLine ToLineEntity(InvoiceLineDto dto);
}

public class InvoiceQueryDtoValidator : PagedQueryDtoValidator<InvoiceQueryDto>;
