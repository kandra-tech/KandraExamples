using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Domain.Exceptions;
using Kandra.Domain.Registers;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Kandra.Printing;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Registers;
using HomePantry.Domain.Numbering;
using HomePantry.Forms.Documents;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Documents;

[KandraDocumentBehavior(typeof(TransferDto))]
[RegisterTransactions(typeof(StockMovement))]
public sealed class TransferBehavior(
    ISimpleNumberingSystem numbering,
    IStringLocalizer localizer) : IDocumentBehavior<Transfer>
{
    public async ValueTask OnNewAsync(Transfer entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Transfer, raw => $"TRF-{raw:D4}", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(Transfer entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var stockWriter = registers.GetWriter<IBalanceRegisterWriter<StockMovement, StockBalance>>();
        var stockReader = registers.GetReader<IBalanceReader<StockMovement, StockBalance>>();
        var turnoverReader = registers.GetReader<ITurnoverReader<StockMovement>>();

        var priorMovements = isInsert
            ? new List<StockMovement>()
            : (await turnoverReader.Movements(DateTime.MinValue, DateTime.MaxValue, q => q.Where(m => m.DocumentId == entity.Id && m.Period <= entity.Date), cancellationToken)).ToList();

        var currentBalances = (await stockReader.Balances(entity.Date, null, null, cancellationToken))
            .Where(b => b.Resources.Quantity > 0)
            .ToList();

        var movements = new List<StockMovement>();

        var groups = entity.Lines.GroupBy(l => new { l.ProductId, l.FromLocationId, l.ToLocationId });
        foreach (var group in groups)
        {
            var totalNeeded = group.Sum(l => l.Quantity);
            if (totalNeeded <= 0) continue;

            var matchingBalances = currentBalances
                .Where(b => b.Dimensions.ProductId == group.Key.ProductId && b.Dimensions.LocationId == group.Key.FromLocationId)
                .Select(b => new
                {
                    b.Dimensions.BestBeforeDate,
                    Quantity = b.Resources.Quantity,
                    Value = b.Resources.Value
                })
                .ToList();

            var matchingPriorExpenses = priorMovements
                .Where(m => m.IsExpense && m.Dimensions.ProductId == group.Key.ProductId && m.Dimensions.LocationId == group.Key.FromLocationId)
                .ToList();

            var batches = new Dictionary<DateTime, (decimal Quantity, decimal Value)>();
            foreach (var b in matchingBalances)
            {
                batches[b.BestBeforeDate] = (b.Quantity, b.Value);
            }
            foreach (var p in matchingPriorExpenses)
            {
                if (batches.TryGetValue(p.Dimensions.BestBeforeDate, out var existing))
                {
                    batches[p.Dimensions.BestBeforeDate] = (existing.Quantity + p.Resources.Quantity, existing.Value + p.Resources.Value);
                }
                else
                {
                    batches[p.Dimensions.BestBeforeDate] = (p.Resources.Quantity, p.Resources.Value);
                }
            }

            var availableTotal = batches.Values.Sum(b => b.Quantity);
            if (availableTotal < totalNeeded)
            {
                throw new InvalidStateException(localizer["Stock_InsufficientBalance"].Value);
            }

            var sortedBatches = batches.OrderBy(kv => kv.Key).ToList();
            var remaining = totalNeeded;

            foreach (var batch in sortedBatches)
            {
                if (remaining <= 0) break;
                if (batch.Value.Quantity <= 0) continue;

                var takeQty = Math.Min(remaining, batch.Value.Quantity);
                var takeVal = takeQty == batch.Value.Quantity
                    ? batch.Value.Value
                    : Math.Round(takeQty * (batch.Value.Value / batch.Value.Quantity), 2, MidpointRounding.AwayFromZero);

                var lineId = Guid.NewGuid();

                // Out leg
                movements.Add(new StockMovement
                {
                    LineId = lineId,
                    IsExpense = true,
                    Dimensions = new StockDimensions
                    {
                        ProductId = group.Key.ProductId,
                        LocationId = group.Key.FromLocationId,
                        BestBeforeDate = batch.Key
                    },
                    Resources = new StockResources
                    {
                        Quantity = takeQty,
                        Value = takeVal
                    }
                });

                // In leg
                movements.Add(new StockMovement
                {
                    LineId = Guid.NewGuid(),
                    IsExpense = false,
                    Dimensions = new StockDimensions
                    {
                        ProductId = group.Key.ProductId,
                        LocationId = group.Key.ToLocationId,
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

        await stockWriter.PostAsync(entity.Id, entity.Date, movements, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(Transfer entity, ISubmitScope registers, CancellationToken cancellationToken)
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
        Transfer entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var productIds = entity.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = (await lookup.GetDictionaryAsync<ProductNode>(productIds, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
        var locationIds = entity.Lines.Select(l => l.FromLocationId).Concat(entity.Lines.Select(l => l.ToLocationId)).Distinct().ToList();
        var locations = (await lookup.GetDictionaryAsync<LocationNode>(locationIds, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);

        var form = new TransferPrintForm
        {
            Number = entity.Code ?? "",
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            Lines = entity.Lines.Select(l => new TransferPrintLine
            {
                ProductName = products.GetValueOrDefault(l.ProductId) ?? "",
                FromLocationName = locations.GetValueOrDefault(l.FromLocationId) ?? "",
                ToLocationName = locations.GetValueOrDefault(l.ToLocationId) ?? "",
                Quantity = l.Quantity
            }).ToList()
        };

        return await renderer.RenderAsync(form, localizer, timeZone ?? TimeZoneInfo.Utc, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class TransferMapper : IEntityMapper<Transfer, TransferDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial TransferDto ToDto(Transfer entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Transfer ToEntity(TransferDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial TransferLineDto ToDto(TransferLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private partial TransferLine ToEntity(TransferLineDto line);
}

public class TransferQueryDtoValidator : PagedQueryDtoValidator<TransferQueryDto>;
