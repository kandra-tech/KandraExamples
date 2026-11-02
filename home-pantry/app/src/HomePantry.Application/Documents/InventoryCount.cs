using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Domain.Registers;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Kandra.Printing;
using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Registers;
using HomePantry.Domain.Numbering;
using HomePantry.Forms.Documents;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Documents;

[KandraDocumentBehavior(typeof(InventoryCountDto))]
[RegisterTransactions(typeof(StockMovement))]
public sealed class InventoryCountBehavior(
    ISimpleNumberingSystem numbering,
    IEntityRepository<ProductNode> productRepo,
    ICurrentTimeZone timeZone) : IDocumentBehavior<InventoryCount>
{
    public async ValueTask OnNewAsync(InventoryCount entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.InventoryCount, raw => $"INV-{raw:D4}", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(InventoryCount entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var stockWriter = registers.GetWriter<IBalanceRegisterWriter<StockMovement, StockBalance>>();
        var stockReader = registers.GetReader<IBalanceReader<StockMovement, StockBalance>>();
        var turnoverReader = registers.GetReader<ITurnoverReader<StockMovement>>();
        var priceReader = registers.GetReader<IInfoRegisterReader<LastPriceRecord>>();

        var priorMovements = isInsert
            ? new List<StockMovement>()
            : (await turnoverReader.Movements(DateTime.MinValue, DateTime.MaxValue, q => q.Where(m => m.DocumentId == entity.Id && m.Period <= entity.Date), cancellationToken)).ToList();

        var currentBalances = (await stockReader.Balances(entity.Date, null, null, cancellationToken))
            .Where(b => b.Resources.Quantity > 0)
            .ToList();

        var productIds = entity.Lines.Select(l => l.ProductId).Distinct().ToList();
        var productNodes = new Dictionary<Guid, ProductNode>();
        foreach (var pid in productIds)
        {
            var p = await productRepo.GetAsync(pid, false, cancellationToken);
            if (p != null) productNodes[pid] = p;
        }

        var movements = new List<StockMovement>();

        foreach (var lineGroup in entity.Lines.GroupBy(l => l.ProductId))
        {
            var productId = lineGroup.Key;
            var countedQuantity = lineGroup.Sum(l => l.CountedQuantity);

            var matchingBalances = currentBalances
                .Where(b => b.Dimensions.ProductId == productId && b.Dimensions.LocationId == entity.LocationId)
                .Select(b => new
                {
                    b.Dimensions.BestBeforeDate,
                    Quantity = b.Resources.Quantity,
                    Value = b.Resources.Value
                })
                .ToList();

            var matchingPriors = priorMovements
                .Where(m => m.Dimensions.ProductId == productId && m.Dimensions.LocationId == entity.LocationId)
                .ToList();

            var batches = new Dictionary<DateTime, (decimal Quantity, decimal Value)>();
            foreach (var b in matchingBalances)
            {
                batches[b.BestBeforeDate] = (b.Quantity, b.Value);
            }
            foreach (var p in matchingPriors)
            {
                var sign = p.IsExpense ? 1 : -1;
                if (batches.TryGetValue(p.Dimensions.BestBeforeDate, out var existing))
                {
                    batches[p.Dimensions.BestBeforeDate] = (existing.Quantity + sign * p.Resources.Quantity, existing.Value + sign * p.Resources.Value);
                }
                else
                {
                    batches[p.Dimensions.BestBeforeDate] = (sign * p.Resources.Quantity, sign * p.Resources.Value);
                }
            }

            var totalOnHand = batches.Values.Sum(b => b.Quantity);
            var totalValue = batches.Values.Sum(b => b.Value);
            var diff = countedQuantity - totalOnHand;

            if (diff > 0)
            {
                // Surplus
                var product = productNodes.GetValueOrDefault(productId) as Product;
                var bestBefore = LocalCalendar.BestBeforeKey(
                    LocalCalendar.DefaultBestBefore(entity.Date, product?.DefaultShelfLifeDays ?? 0, timeZone.TimeZone), timeZone.TimeZone);

                var lastPrices = await priceReader.SliceOfLast(entity.Date, q => q.Where(p => p.Dimensions.ProductId == productId), cancellationToken);
                var unitPrice = lastPrices.OrderByDescending(p => p.Period).FirstOrDefault()?.Details.Price
                    ?? (totalOnHand > 0 ? Math.Round(totalValue / totalOnHand, 2) : 0m);

                movements.Add(new StockMovement
                {
                    LineId = Guid.NewGuid(),
                    IsExpense = false,
                    Dimensions = new StockDimensions
                    {
                        ProductId = productId,
                        LocationId = entity.LocationId,
                        BestBeforeDate = bestBefore
                    },
                    Resources = new StockResources
                    {
                        Quantity = diff,
                        Value = Math.Round(diff * unitPrice, 2)
                    }
                });
            }
            else if (diff < 0)
            {
                // Shortage: take earliest batch first
                var shortage = -diff;
                var sortedBatches = batches.OrderBy(kv => kv.Key).ToList();
                var remaining = shortage;

                foreach (var batch in sortedBatches)
                {
                    if (remaining <= 0) break;
                    if (batch.Value.Quantity <= 0) continue;

                    var takeQty = Math.Min(remaining, batch.Value.Quantity);
                    var takeVal = takeQty == batch.Value.Quantity
                        ? batch.Value.Value
                        : Math.Round(takeQty * (batch.Value.Value / batch.Value.Quantity), 2, MidpointRounding.AwayFromZero);

                    movements.Add(new StockMovement
                    {
                        LineId = Guid.NewGuid(),
                        IsExpense = true,
                        Dimensions = new StockDimensions
                        {
                            ProductId = productId,
                            LocationId = entity.LocationId,
                            BestBeforeDate = batch.Key
                        },
                        Resources = new StockResources
                        {
                            Quantity = takeQty,
                            Value = takeVal
                        }
                    });

                    remaining -= takeQty;
                }
            }
        }

        await stockWriter.PostAsync(entity.Id, entity.Date, movements, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(InventoryCount entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var stockWriter = registers.GetWriter<IBalanceRegisterWriter<StockMovement, StockBalance>>();
        await stockWriter.UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var stockWriter = registers.GetWriter<IBalanceRegisterWriter<StockMovement, StockBalance>>();
        await stockWriter.DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(
        InventoryCount entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var locations = (await lookup.GetDictionaryAsync<LocationNode>(new[] { entity.LocationId }, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
        var productIds = entity.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = (await lookup.GetDictionaryAsync<ProductNode>(productIds, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);

        var form = new InventoryCountPrintForm
        {
            Number = entity.Code ?? "",
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            LocationName = locations.GetValueOrDefault(entity.LocationId) ?? "",
            Lines = entity.Lines.Select(l => new InventoryCountPrintLine
            {
                ProductName = products.GetValueOrDefault(l.ProductId) ?? "",
                CountedQuantity = l.CountedQuantity
            }).ToList()
        };

        return await renderer.RenderAsync(form, localizer, timeZone ?? TimeZoneInfo.Utc, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class InventoryCountMapper : IEntityMapper<InventoryCount, InventoryCountDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial InventoryCountDto ToDto(InventoryCount entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial InventoryCount ToEntity(InventoryCountDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial InventoryCountLineDto ToDto(InventoryCountLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private partial InventoryCountLine ToEntity(InventoryCountLineDto line);
}

public class InventoryCountQueryDtoValidator : PagedQueryDtoValidator<InventoryCountQueryDto>;
