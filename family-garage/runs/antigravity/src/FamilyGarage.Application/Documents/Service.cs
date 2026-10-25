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
using FamilyGarage.Domain.Commons;
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

[KandraDocumentBehavior(typeof(ServiceDto))]
[RegisterTransactions(typeof(OdometerRecord))]
[RegisterTransactions(typeof(VehicleCostsMovement))]
[RegisterTransactions(typeof(ServiceLinesMovement))]
[RegisterTransactions(typeof(LastDoneRecord))]
public class ServiceBehavior(
    ISimpleNumberingSystem numbering,
    IConstantsService constants,
    IReportsRepository reportsRepo,
    IStringLocalizer localizer) : IDocumentBehavior<Service>
{
    public async ValueTask OnNewAsync(Service entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var yy = targetDate.ToString("yy");
        entity.Date = targetDate;
        entity.Code = await numbering.GetNextAsync(
            $"{NumberingBuckets.Services}:{yy}",
            raw => $"SRV-{yy}-{raw:D4}",
            requestorTag: "Service:OnNew",
            cancellationToken: cancellationToken);

        if (sourceDocumentId.HasValue)
        {
            var accident = await reportsRepo.GetAccidentAsync(sourceDocumentId.Value, cancellationToken);
            if (accident != null)
            {
                entity.VehicleId = accident.VehicleId;
                entity.Odometer = accident.Odometer;
                entity.Kind = ServiceKind.Repair;
                entity.AccidentId = accident.Id;
            }
        }
    }

    public ValueTask OnSaveAsync(Service entity, bool isInsert, CancellationToken cancellationToken)
    {
        if (entity.Detailed)
        {
            foreach (var line in entity.Lines)
            {
                line.Amount = line.Quantity * line.Price;
            }
            entity.Amount = entity.Lines.Sum(l => l.Amount);
        }
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnSubmitAsync(Service entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
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

        var costCategory = entity.Kind == ServiceKind.Maintenance
            ? CostCategory.Maintenance
            : CostCategory.Repair;

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
                        Category = costCategory
                    },
                    Resources = new VehicleCostsResources { Amount = entity.Amount },
                    IsExpense = false,
                    Details = new VehicleCostsDetails()
                }
            ],
            cancellationToken);

        var serviceLinesWriter = registers.GetWriter<ITurnoverRegisterWriter<ServiceLinesMovement>>();
        var lastDoneWriter = registers.GetWriter<IDocumentInfoRegisterWriter<LastDoneRecord>>();

        if (entity.Detailed && entity.Lines.Count > 0)
        {
            // Every record of a document needs its own LineId (the line's): a document-bound register is unique by
            // (DocumentId, LineId), so a second work item on the same service would otherwise refuse the post.
            var lineMovements = entity.Lines.Select(line => new ServiceLinesMovement
            {
                LineId = line.Id,
                Dimensions = new ServiceLinesDimensions
                {
                    VehicleId = entity.VehicleId,
                    ItemId = line.ItemId
                },
                Resources = new ServiceLinesResources
                {
                    Quantity = line.Quantity,
                    Amount = line.Amount
                },
                IsExpense = false,
                Details = new ServiceLinesDetails
                {
                    Odometer = entity.Odometer
                }
            }).ToList();
            await serviceLinesWriter.PostAsync(entity.Id, entity.Date, lineMovements, cancellationToken);

            var workIntervals = await reportsRepo.GetWorkItemIntervalsAsync(
                entity.Lines.Select(l => l.ItemId),
                cancellationToken);

            var lastDoneRecords = entity.Lines
                .Where(l => workIntervals.ContainsKey(l.ItemId))
                .Select(l => new LastDoneRecord
                {
                    LineId = l.Id,
                    Dimensions = new LastDoneDimensions
                    {
                        VehicleId = entity.VehicleId,
                        ItemId = l.ItemId
                    },
                    Details = new LastDoneDetails
                    {
                        Odometer = entity.Odometer,
                        Date = entity.Date
                    }
                }).ToList();

            await lastDoneWriter.PostAsync(entity.Id, entity.Date, lastDoneRecords, cancellationToken);
        }
        else
        {
            await serviceLinesWriter.PostAsync(entity.Id, entity.Date, [], cancellationToken);
            await lastDoneWriter.PostAsync(entity.Id, entity.Date, [], cancellationToken);
        }
    }

    public async ValueTask OnUnsubmitAsync(Service entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<IDocumentInfoRegisterWriter<LastDoneRecord>>().UnpostAsync(entity.Id, cancellationToken);
        await registers.GetWriter<ITurnoverRegisterWriter<ServiceLinesMovement>>().UnpostAsync(entity.Id, cancellationToken);
        await registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>().UnpostAsync(entity.Id, cancellationToken);
        await registers.GetWriter<IDocumentInfoRegisterWriter<OdometerRecord>>().UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<IDocumentInfoRegisterWriter<LastDoneRecord>>().DeleteAsync(id, cancellationToken);
        await registers.GetWriter<ITurnoverRegisterWriter<ServiceLinesMovement>>().DeleteAsync(id, cancellationToken);
        await registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>().DeleteAsync(id, cancellationToken);
        await registers.GetWriter<IDocumentInfoRegisterWriter<OdometerRecord>>().DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(
        Service entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var vehicles = await lookup.GetDictionaryAsync<Vehicle>([entity.VehicleId], cancellationToken);
        var stations = await lookup.GetDictionaryAsync<Provider>([entity.ServiceStationId], cancellationToken);

        var itemIds = entity.Lines.Select(l => l.ItemId).Distinct().ToList();
        var itemNames = (await lookup.GetDictionaryAsync<ItemNode>(itemIds, cancellationToken))
            .ToDictionary(i => i.Id, i => i.Name);

        var printLines = entity.Lines.Select(l => new ServicePrintLine
        {
            ItemName = itemNames.GetValueOrDefault(l.ItemId, string.Empty),
            Quantity = l.Quantity,
            Price = l.Price,
            Amount = l.Amount
        }).ToList();

        var form = new ServicePrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            Vehicle = vehicles.FirstOrDefault()?.Name ?? string.Empty,
            ServiceStation = stations.FirstOrDefault()?.Name ?? string.Empty,
            Kind = entity.Kind.GetCaption(localizer),
            Odometer = entity.Odometer,
            Comment = entity.Comment,
            Lines = printLines,
            Total = entity.Amount,
            CurrencyCode = (await constants.GetAsync<CurrencyCodeConstant>(entity.Date, cancellationToken)).Value
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class ServiceMapper : IEntityMapper<Service, ServiceDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial ServiceDto ToDto(Service entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Service ToEntity(ServiceDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial ServiceLineDto ToDto(ServiceLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private partial ServiceLine ToEntity(ServiceLineDto line);
}

public sealed class ServiceQueryDtoValidator : PagedQueryDtoValidator<ServiceQueryDto>
{
}
