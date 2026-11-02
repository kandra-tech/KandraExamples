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

[KandraDocumentBehavior(typeof(CookRecipeDto))]
[RegisterTransactions(typeof(StockMovement))]
public sealed class CookRecipeBehavior(
    ISimpleNumberingSystem numbering,
    IEntityRepository<Recipe> recipeRepo,
    IStringLocalizer localizer) : IDocumentBehavior<CookRecipe>
{
    public async ValueTask OnNewAsync(CookRecipe entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.CookRecipe, raw => $"CKR-{raw:D4}", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSaveAsync(CookRecipe entity, bool isInsert, CancellationToken cancellationToken)
    {
        if (entity.Lines.Count == 0 && entity.RecipeId != Guid.Empty && entity.Servings > 0)
        {
            var recipe = await recipeRepo.GetAsync(entity.RecipeId, true, cancellationToken);
            if (recipe != null)
            {
                var baseServings = recipe.Servings > 0 ? recipe.Servings : 1m;
                var scale = entity.Servings / baseServings;
                foreach (var line in recipe.Lines)
                {
                    entity.Lines.Add(new CookRecipeLine
                    {
                        Id = Guid.NewGuid(),
                        ProductId = line.ProductId,
                        Quantity = Math.Round(line.Quantity * scale, 3)
                    });
                }
            }
        }
    }

    public async ValueTask OnSubmitAsync(CookRecipe entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
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

        var groups = entity.Lines.GroupBy(l => l.ProductId);
        foreach (var group in groups)
        {
            var totalNeeded = group.Sum(l => l.Quantity);
            if (totalNeeded <= 0) continue;

            var matchingBalances = currentBalances
                .Where(b => b.Dimensions.ProductId == group.Key && b.Dimensions.LocationId == entity.LocationId)
                .Select(b => new
                {
                    b.Dimensions.BestBeforeDate,
                    Quantity = b.Resources.Quantity,
                    Value = b.Resources.Value
                })
                .ToList();

            var matchingPriors = priorMovements
                .Where(m => m.Dimensions.ProductId == group.Key && m.Dimensions.LocationId == entity.LocationId)
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
                        ProductId = group.Key,
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

        await stockWriter.PostAsync(entity.Id, entity.Date, movements, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(CookRecipe entity, ISubmitScope registers, CancellationToken cancellationToken)
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
        CookRecipe entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var recipes = (await lookup.GetDictionaryAsync<Recipe>(new[] { entity.RecipeId }, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
        var locations = (await lookup.GetDictionaryAsync<LocationNode>(new[] { entity.LocationId }, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
        var productIds = entity.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = (await lookup.GetDictionaryAsync<ProductNode>(productIds, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);

        var form = new CookRecipePrintForm
        {
            Number = entity.Code ?? "",
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            RecipeName = recipes.GetValueOrDefault(entity.RecipeId) ?? "",
            Servings = entity.Servings,
            LocationName = locations.GetValueOrDefault(entity.LocationId) ?? "",
            Lines = entity.Lines.Select(l => new CookRecipePrintLine
            {
                ProductName = products.GetValueOrDefault(l.ProductId) ?? "",
                Quantity = l.Quantity
            }).ToList()
        };

        return await renderer.RenderAsync(form, localizer, timeZone ?? TimeZoneInfo.Utc, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class CookRecipeMapper : IEntityMapper<CookRecipe, CookRecipeDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial CookRecipeDto ToDto(CookRecipe entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial CookRecipe ToEntity(CookRecipeDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial CookRecipeLineDto ToDto(CookRecipeLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private partial CookRecipeLine ToEntity(CookRecipeLineDto line);
}

public class CookRecipeQueryDtoValidator : PagedQueryDtoValidator<CookRecipeQueryDto>;
