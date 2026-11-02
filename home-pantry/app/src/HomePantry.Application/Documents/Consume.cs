using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Domain.Exceptions;
using Kandra.Domain.Registers;
using Kandra.Persistence.Abstractions.Registers;
using HomePantry.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Registers;
using HomePantry.Domain.Numbering;
using HomePantry.Forms.Documents;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Documents;

[KandraDocumentBehavior(typeof(ConsumeDto))]
[RegisterTransactions(typeof(StockMovement))]
public sealed class ConsumeBehavior(
    ISimpleNumberingSystem numbering,
    IStringLocalizer localizer) : IDocumentBehavior<Consume>
{
    public async ValueTask OnNewAsync(Consume entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Consume, raw => $"CON-{raw:D4}", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(Consume entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var stockWriter = registers.GetWriter<IBalanceRegisterWriter<StockMovement, StockBalance>>();
        var stockReader = registers.GetReader<IBalanceReader<StockMovement, StockBalance>>();
        var turnoverReader = registers.GetReader<ITurnoverReader<StockMovement>>();

        var priorMovements = isInsert
            ? new List<StockMovement>()
            : (await turnoverReader.Movements(DateTime.MinValue, DateTime.MaxValue, q => q.Where(m => m.DocumentId == entity.Id && m.IsExpense && m.Period <= entity.Date), cancellationToken)).ToList();

        var currentBalances = (await stockReader.Balances(entity.Date, null, null, cancellationToken))
            .Where(b => b.Resources.Quantity > 0)
            .ToList();

        var movements = new List<StockMovement>();

        var groups = entity.Lines.GroupBy(l => new { l.ProductId, l.LocationId });
        foreach (var group in groups)
        {
            var totalNeeded = group.Sum(l => l.Quantity);
            if (totalNeeded <= 0) continue;

            var matchingBalances = currentBalances
                .Where(b => b.Dimensions.ProductId == group.Key.ProductId && b.Dimensions.LocationId == group.Key.LocationId)
                .Select(b => new
                {
                    b.Dimensions.BestBeforeDate,
                    Quantity = b.Resources.Quantity,
                    Value = b.Resources.Value
                })
                .ToList();

            var matchingPriors = priorMovements
                .Where(m => m.Dimensions.ProductId == group.Key.ProductId && m.Dimensions.LocationId == group.Key.LocationId)
                .ToList();

            var batches = new Dictionary<DateTime, (decimal Quantity, decimal Value)>();
            foreach (var b in matchingBalances)
            {
                batches[b.BestBeforeDate] = (b.Quantity, b.Value);
            }
            foreach (var p in matchingPriors)
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

                movements.Add(new StockMovement
                {
                    LineId = Guid.NewGuid(),
                    IsExpense = true,
                    Dimensions = new StockDimensions
                    {
                        ProductId = group.Key.ProductId,
                        LocationId = group.Key.LocationId,
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

    public async ValueTask OnUnsubmitAsync(Consume entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var stockWriter = registers.GetWriter<IBalanceRegisterWriter<StockMovement, StockBalance>>();
        await stockWriter.UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var stockWriter = registers.GetWriter<IBalanceRegisterWriter<StockMovement, StockBalance>>();
        await stockWriter.DeleteAsync(id, cancellationToken);
    }
}

[Mapper]
public partial class ConsumeMapper : IEntityMapper<Consume, ConsumeDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial ConsumeDto ToDto(Consume entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Consume ToEntity(ConsumeDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial ConsumeLineDto ToDto(ConsumeLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private partial ConsumeLine ToEntity(ConsumeLineDto line);
}

public class ConsumeQueryDtoValidator : PagedQueryDtoValidator<ConsumeQueryDto>;
