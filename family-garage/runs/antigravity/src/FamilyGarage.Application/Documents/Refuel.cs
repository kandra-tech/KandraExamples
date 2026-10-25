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
using FamilyGarage.Enums;
using FamilyGarage.Forms.Documents;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;
using Kandra.Application.Abstractions.Constants;
using Kandra.Localization;
using FamilyGarage.Application.Constants;

namespace FamilyGarage.Application.Documents;

[KandraDocumentBehavior(typeof(RefuelDto))]
[RegisterTransactions(typeof(OdometerRecord))]
[RegisterTransactions(typeof(VehicleCostsMovement))]
public class RefuelBehavior(
    ISimpleNumberingSystem numbering,
    IConstantsService constants,
    IStringLocalizer localizer) : IDocumentBehavior<Refuel>
{
    public async ValueTask OnNewAsync(Refuel entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var yy = targetDate.ToString("yy");
        entity.Date = targetDate;
        entity.Code = await numbering.GetNextAsync(
            $"{NumberingBuckets.Refuels}:{yy}",
            raw => $"REF-{yy}-{raw:D4}",
            requestorTag: "Refuel:OnNew",
            cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(Refuel entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
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

        var costsWriter = registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>();
        await costsWriter.PostAsync(
            entity.Id,
            entity.Date,
            [
                new VehicleCostsMovement
                {
                    Dimensions = new VehicleCostsDimensions
                    {
                        VehicleId = entity.VehicleId,
                        Category = CostCategory.Fuel
                    },
                    Resources = new VehicleCostsResources { Amount = entity.Amount },
                    IsExpense = false,
                    Details = new VehicleCostsDetails
                    {
                        Litres = entity.Litres,
                        FullTank = entity.FullTank
                    }
                }
            ],
            cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(Refuel entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>().UnpostAsync(entity.Id, cancellationToken);
        await registers.GetWriter<IDocumentInfoRegisterWriter<OdometerRecord>>().UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>().DeleteAsync(id, cancellationToken);
        await registers.GetWriter<IDocumentInfoRegisterWriter<OdometerRecord>>().DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(
        Refuel entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var vehicles = await lookup.GetDictionaryAsync<Vehicle>([entity.VehicleId], cancellationToken);
        var stations = await lookup.GetDictionaryAsync<Provider>([entity.FuelStationId], cancellationToken);

        var form = new RefuelPrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            Vehicle = vehicles.FirstOrDefault()?.Name ?? string.Empty,
            FuelStation = stations.FirstOrDefault()?.Name ?? string.Empty,
            Litres = entity.Litres,
            Odometer = entity.Odometer,
            FullTank = entity.FullTank,
            Total = entity.Amount,
            CurrencyCode = (await constants.GetAsync<CurrencyCodeConstant>(entity.Date, cancellationToken)).Value
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class RefuelMapper : IEntityMapper<Refuel, RefuelDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial RefuelDto ToDto(Refuel entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Refuel ToEntity(RefuelDto dto);
}

public sealed class RefuelQueryDtoValidator : PagedQueryDtoValidator<RefuelQueryDto>
{
}
