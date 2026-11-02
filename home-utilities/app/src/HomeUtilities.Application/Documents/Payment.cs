using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Constants;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
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

[KandraDocumentBehavior(typeof(PaymentDto))]
[RegisterTransactions(typeof(SettlementMovement))]
public class PaymentBehavior(
    ISimpleNumberingSystem numbering,
    IDocumentReaderService documentReader,
    IEntityRepository<Provider> providersRepository,
    IEntityRepository<Property> propertiesRepository,
    IConstantsService constants,
    ICurrentTimeZone? currentTimeZone = null,
    IAuthorizationChecker? authorizationChecker = null) : IDocumentBehavior<Payment>
{
    public async ValueTask OnNewAsync(Payment entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        var localDate = TimeZoneHelper.ToLocalDate(entity.Date, currentTimeZone?.TimeZone);
        var yy = localDate.ToString("yy");
        entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.Payment}:{yy}",
            raw => $"PAY-{yy}-{raw:D4}",
            requestorTag: "Payment:OnNew",
            cancellationToken: cancellationToken);

        if (sourceDocumentId.HasValue)
        {
            var doc = await documentReader.GetAsync(sourceDocumentId.Value, cancellationToken);
            if (doc is Invoice invoice)
            {
                entity.ProviderId = invoice.ProviderId;
                entity.PropertyId = invoice.PropertyId;
                entity.Amount = invoice.TotalAmount;
                entity.InvoiceId = invoice.Id;
                entity.PaymentDescription = invoice.PaymentDescription;
                entity.Comment = !string.IsNullOrWhiteSpace(invoice.PaymentDescription)
                    ? invoice.PaymentDescription
                    : $"Payment for Invoice {invoice.Code}";
            }
        }
    }

    public async ValueTask OnSaveAsync(Payment entity, bool isInsert, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entity.PaymentDescription) && entity.ProviderId != Guid.Empty && entity.PropertyId != Guid.Empty)
        {
            var prov = await providersRepository.GetAsync(entity.ProviderId, false, cancellationToken);
            var prop = await propertiesRepository.GetAsync(entity.PropertyId, false, cancellationToken);
            entity.PaymentDescription = PaymentDescriptionFormatter.Format(
                prov?.PaymentDescriptionTemplate,
                prov,
                prop,
                entity.Date,
                entity.Code);
            if (string.IsNullOrWhiteSpace(entity.Comment))
            {
                entity.Comment = entity.PaymentDescription;
            }
        }
    }

    public async ValueTask OnSubmitAsync(Payment entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var movements = new List<SettlementMovement>
        {
            new()
            {
                LineId = Guid.Empty,
                IsExpense = true, // decreases debt
                Dimensions = new SettlementDimensions
                {
                    ProviderId = entity.ProviderId,
                    PropertyId = entity.PropertyId,
                },
                Resources = new SettlementResources
                {
                    Amount = entity.Amount,
                },
            }
        };

        var writer = registers.GetWriter<IBalanceRegisterWriter<SettlementMovement, SettlementBalance>>();
        await writer.PostAsync(entity.Id, entity.Date, movements, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(Payment entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var writer = registers.GetWriter<IBalanceRegisterWriter<SettlementMovement, SettlementBalance>>();
        await writer.UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var writer = registers.GetWriter<IBalanceRegisterWriter<SettlementMovement, SettlementBalance>>();
        await writer.DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(Payment entity, ExportFormat format, string? layoutTag,
        ILookupRepository lookup, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var prov = await providersRepository.GetAsync(entity.ProviderId, false, cancellationToken);
        var providerName = prov?.Name ?? entity.ProviderId.ToString();

        var prop = await propertiesRepository.GetAsync(entity.PropertyId, false, cancellationToken);
        var propertyName = prop?.Name ?? entity.PropertyId.ToString();

        var currency = (await constants.GetAsync<CurrencyCodeConstant>(entity.Date, cancellationToken))?.Value ?? "UAH";

        var form = new PaymentPrintForm
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
            PaymentDescription = entity.PaymentDescription ?? entity.Comment,
            Comment = entity.Comment,
            Total = entity.Amount,
            CurrencyCode = currency,
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class PaymentMapper : IEntityMapper<Payment, PaymentDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    [MapperIgnoreSource(nameof(Payment.InvoiceId))]
    public partial PaymentDto ToDto(Payment entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreTarget(nameof(Payment.InvoiceId))]
    public partial Payment ToEntity(PaymentDto dto);
}

public class PaymentQueryDtoValidator : PagedQueryDtoValidator<PaymentQueryDto>;
