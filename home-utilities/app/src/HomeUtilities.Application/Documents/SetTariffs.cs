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
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Domain.Entities.Documents;
using HomeUtilities.Domain.Entities.Registers;
using HomeUtilities.Domain.Numbering;
using HomeUtilities.Forms.Documents;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomeUtilities.Application.Documents;

[KandraDocumentBehavior(typeof(SetTariffsDto))]
[RegisterTransactions(typeof(TariffRecord))]
public class SetTariffsBehavior(
    ISimpleNumberingSystem numbering,
    IConstantsService constants,
    ICurrentTimeZone? currentTimeZone = null,
    IAuthorizationChecker? authorizationChecker = null) : IDocumentBehavior<SetTariffs>
{
    public async ValueTask OnNewAsync(SetTariffs entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        var localDate = TimeZoneHelper.ToLocalDate(entity.Date, currentTimeZone?.TimeZone);
        var yy = localDate.ToString("yy");
        entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.SetTariffs}:{yy}",
            raw => $"TRF-{yy}-{raw:D4}",
            requestorTag: "SetTariffs:OnNew",
            cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(SetTariffs entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var records = entity.Lines.Select(line => new TariffRecord
        {
            LineId = line.Id,
            Dimensions = new TariffDimensions
            {
                ServiceId = line.ServiceId,
                Zone = line.Zone,
            },
            Details = new TariffDetails
            {
                Price = line.Price,
            },
        }).ToList();

        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<TariffRecord>>();
        await writer.PostAsync(entity.Id, entity.Date, records, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(SetTariffs entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<TariffRecord>>();
        await writer.UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<TariffRecord>>();
        await writer.DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(SetTariffs entity, ExportFormat format, string? layoutTag,
        ILookupRepository lookup, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var serviceIds = entity.Lines.Select(l => l.ServiceId).Distinct().ToList();
        var services = (await lookup.GetDictionaryAsync<ServiceNode>(serviceIds, cancellationToken))
            .ToDictionary(s => s.Id, s => s.Name);

        var currency = (await constants.GetAsync<CurrencyCodeConstant>(entity.Date, cancellationToken))?.Value ?? "UAH";

        var form = new SetTariffsPrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = authorizationChecker is not null
                ? await Reports.PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken)
                : null,
            CurrencyCode = currency,
            Lines = entity.Lines.Select(l => new SetTariffsPrintLine
            {
                ServiceName = services.TryGetValue(l.ServiceId, out var sName) ? sName : l.ServiceId.ToString(),
                Zone = l.Zone,
                Price = l.Price,
            }).ToList(),
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class SetTariffsMapper : IEntityMapper<SetTariffs, SetTariffsDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial SetTariffsDto ToDto(SetTariffs entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial SetTariffs ToEntity(SetTariffsDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial SetTariffsLineDto ToLineDto(SetTariffsLine entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial SetTariffsLine ToLineEntity(SetTariffsLineDto dto);
}

public class SetTariffsQueryDtoValidator : PagedQueryDtoValidator<SetTariffsQueryDto>;
