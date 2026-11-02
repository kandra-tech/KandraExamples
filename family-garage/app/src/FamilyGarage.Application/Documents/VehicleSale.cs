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

[KandraDocumentBehavior(typeof(VehicleSaleDto))]
[RegisterTransactions(typeof(OdometerRecord))]
[RegisterTransactions(typeof(VehicleCostsMovement))]
public class VehicleSaleBehavior(
    ISimpleNumberingSystem numbering,
    IConstantsService constants,
    IStringLocalizer localizer) : IDocumentBehavior<VehicleSale>
{
    public async ValueTask OnNewAsync(VehicleSale entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var yy = targetDate.ToString("yy");
        entity.Date = targetDate;
        entity.Code = await numbering.GetNextAsync(
            $"{NumberingBuckets.VehicleSales}:{yy}",
            raw => $"SAL-{yy}-{raw:D4}",
            requestorTag: "VehicleSale:OnNew",
            cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(VehicleSale entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
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
                        Category = CostCategory.Purchase
                    },
                    Resources = new VehicleCostsResources { Amount = entity.Price },
                    IsExpense = true, // Outflow: sale price reduces costs
                    Details = new VehicleCostsDetails { IsSale = true }
                }
            ],
            cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(VehicleSale entity, ISubmitScope registers, CancellationToken cancellationToken)
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
        VehicleSale entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var vehicles = await lookup.GetDictionaryAsync<Vehicle>([entity.VehicleId], cancellationToken);

        var form = new VehicleSalePrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            Vehicle = vehicles.FirstOrDefault()?.Name ?? string.Empty,
            Buyer = entity.Buyer,
            Odometer = entity.Odometer,
            Total = entity.Price,
            CurrencyCode = (await constants.GetAsync<CurrencyCodeConstant>(entity.Date, cancellationToken)).Value
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class VehicleSaleMapper : IEntityMapper<VehicleSale, VehicleSaleDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial VehicleSaleDto ToDto(VehicleSale entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial VehicleSale ToEntity(VehicleSaleDto dto);
}

public sealed class VehicleSaleQueryDtoValidator : PagedQueryDtoValidator<VehicleSaleQueryDto>
{
}
