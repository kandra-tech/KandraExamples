using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Domain.Numbering;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Kandra.Printing;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Domain.Entities.Documents;
using FamilyGarage.Domain.Entities.Registers;
using FamilyGarage.Domain.Numbering;
using FamilyGarage.Forms.Documents;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;
using Kandra.Application.Abstractions.Constants;
using Kandra.Localization;
using FamilyGarage.Application.Constants;

namespace FamilyGarage.Application.Documents;

[KandraDocumentBehavior(typeof(AccidentDto))]
[RegisterTransactions(typeof(OdometerRecord))]
public class AccidentBehavior(
    ISimpleNumberingSystem numbering,
    IConstantsService constants,
    IStringLocalizer localizer) : IDocumentBehavior<Accident>
{
    public async ValueTask OnNewAsync(Accident entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var yy = targetDate.ToString("yy");
        entity.Date = targetDate;
        entity.Code = await numbering.GetNextAsync(
            $"{NumberingBuckets.Accidents}:{yy}",
            raw => $"ACC-{yy}-{raw:D4}",
            requestorTag: "Accident:OnNew",
            cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(Accident entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await OdometerPostingGuard.EnsureOdometerValidAsync(
            registers,
            entity.VehicleId,
            entity.Date,
            entity.Id,
            entity.Odometer,
            localizer,
            cancellationToken);

        var odoWriter = registers.GetWriter<IDocumentInfoRegisterWriter<OdometerRecord>>();
        await odoWriter.PostAsync(
            entity.Id,
            entity.Date,
            [
                new OdometerRecord
                {
                    Dimensions = new OdometerDimensions { VehicleId = entity.VehicleId },
                    Details = new OdometerDetails { Kilometres = entity.Odometer }
                }
            ],
            cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(Accident entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<IDocumentInfoRegisterWriter<OdometerRecord>>().UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<IDocumentInfoRegisterWriter<OdometerRecord>>().DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(
        Accident entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var vehicles = await lookup.GetDictionaryAsync<Vehicle>([entity.VehicleId], cancellationToken);

        var form = new AccidentPrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            Vehicle = vehicles.FirstOrDefault()?.Name ?? string.Empty,
            Odometer = entity.Odometer,
            Place = entity.Place,
            Description = entity.Description,
            Total = entity.EstimatedDamage,
            CurrencyCode = (await constants.GetAsync<CurrencyCodeConstant>(entity.Date, cancellationToken)).Value
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class AccidentMapper : IEntityMapper<Accident, AccidentDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial AccidentDto ToDto(Accident entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Accident ToEntity(AccidentDto dto);
}

public sealed class AccidentQueryDtoValidator : PagedQueryDtoValidator<AccidentQueryDto>
{
}
