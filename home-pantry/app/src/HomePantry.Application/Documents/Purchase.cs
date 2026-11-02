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
using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Registers;
using HomePantry.Domain.Numbering;
using HomePantry.Forms.Documents;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Documents;

[KandraDocumentBehavior(typeof(PurchaseDto))]
[RegisterTransactions(typeof(StockMovement))]
[RegisterTransactions(typeof(LastPriceRecord))]
public sealed class PurchaseBehavior(
    ISimpleNumberingSystem numbering,
    IDocumentReaderService documentReader,
    IDocumentLinkService documentLinkService,
    IEntityRepository<ProductNode> productRepo,
    IInfoRegisterReader<LastPriceRecord> priceReader,
    IStringLocalizer localizer,
    ICurrentTimeZone timeZone) : IDocumentBehavior<Purchase>
{
    public async ValueTask OnNewAsync(Purchase entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Purchase, raw => $"PUR-{raw:D4}", cancellationToken: cancellationToken);

        if (sourceDocumentId.HasValue)
        {
            var source = await documentReader.GetAsync(sourceDocumentId.Value, cancellationToken);
            if (source is ShoppingList shoppingList)
            {
                entity.ShoppingListId = shoppingList.Id;
                if (shoppingList.StoreId.HasValue)
                {
                    entity.StoreId = shoppingList.StoreId.Value;
                }

                var lastPrices = (await priceReader.SliceOfLast(entity.Date, q => q, cancellationToken))
                    .OrderByDescending(p => p.Period)
                    .ToList();

                foreach (var sl in shoppingList.Lines.Where(l => !l.Bought))
                {
                    var product = await productRepo.GetAsync(sl.ProductId, false, cancellationToken) as Product;
                    var locId = product?.DefaultLocationId ?? Guid.Empty;
                    var bestBefore = LocalCalendar.DefaultBestBefore(entity.Date, product?.DefaultShelfLifeDays ?? 0, timeZone.TimeZone);

                    var price = (entity.StoreId != Guid.Empty
                        ? lastPrices.FirstOrDefault(p => p.Dimensions.ProductId == sl.ProductId && p.Dimensions.StoreId == entity.StoreId)?.Details.Price
                        : null)
                        ?? lastPrices.FirstOrDefault(p => p.Dimensions.ProductId == sl.ProductId)?.Details.Price
                        ?? 0m;
                    var amount = Math.Round(sl.Quantity * price, 2);

                    entity.Lines.Add(new PurchaseLine
                    {
                        Id = Guid.NewGuid(),
                        ProductId = sl.ProductId,
                        Quantity = sl.Quantity,
                        Price = price,
                        Amount = amount,
                        LocationId = locId,
                        BestBeforeDate = bestBefore
                    });
                }

                entity.Total = entity.Lines.Sum(l => l.Amount);
            }
        }
    }

    public async ValueTask OnSaveAsync(Purchase entity, bool isInsert, CancellationToken cancellationToken)
    {
        foreach (var line in entity.Lines)
        {
            line.Amount = Math.Round(line.Quantity * line.Price, 2);
        }
        entity.Total = entity.Lines.Sum(l => l.Amount);

        IReadOnlyCollection<DocumentLinkSyncItem> links = entity.ShoppingListId is { } slId && slId != Guid.Empty
            ? [new DocumentLinkSyncItem(slId, ShoppingList.TypeId, "ShoppingList")]
            : [];
        await documentLinkService.SyncLinksAsync(entity.Id, Purchase.TypeId, links, cancellationToken);
    }

    public async ValueTask OnSubmitAsync(Purchase entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        if (!isInsert)
        {
            var stockReader = registers.GetReader<IBalanceReader<StockMovement, StockBalance>>();
            var turnoverReader = registers.GetReader<ITurnoverReader<StockMovement>>();
            var currentBalances = await stockReader.Balances(q => q, cancellationToken);
            var priorMovements = await turnoverReader.Movements(DateTime.MinValue, DateTime.MaxValue, q => q.Where(m => m.DocumentId == entity.Id && !m.IsExpense), cancellationToken);

            var priorByBatch = priorMovements
                .GroupBy(m => (m.Dimensions.ProductId, m.Dimensions.LocationId, m.Dimensions.BestBeforeDate))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Resources.Quantity));

            var newByBatch = entity.Lines
                .GroupBy(l => (l.ProductId, l.LocationId, BestBeforeDate: BatchDate(l)))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            foreach (var (batchKey, priorQty) in priorByBatch)
            {
                var newQty = newByBatch.GetValueOrDefault(batchKey, 0m);
                var reduction = priorQty - newQty;
                if (reduction > 0)
                {
                    var currentQty = currentBalances
                        .Where(b => b.Dimensions.ProductId == batchKey.ProductId &&
                                    b.Dimensions.LocationId == batchKey.LocationId &&
                                    b.Dimensions.BestBeforeDate == batchKey.BestBeforeDate)
                        .Sum(b => b.Resources.Quantity);
                    if (currentQty < reduction)
                    {
                        throw new InvalidStateException(localizer["Purchase_CannotUnpostConsumed"].Value);
                    }
                }
            }
        }

        var stockWriter = registers.GetWriter<IBalanceRegisterWriter<StockMovement, StockBalance>>();
        var movements = entity.Lines.Select(line => new StockMovement
        {
            LineId = line.Id,
            IsExpense = false,
            Dimensions = new StockDimensions
            {
                ProductId = line.ProductId,
                LocationId = line.LocationId,
                BestBeforeDate = BatchDate(line)
            },
            Resources = new StockResources
            {
                Quantity = line.Quantity,
                Value = line.Amount
            }
        }).ToList();
        await stockWriter.PostAsync(entity.Id, entity.Date, movements, cancellationToken);

        var priceWriter = registers.GetWriter<IDocumentInfoRegisterWriter<LastPriceRecord>>();
        var priceRecords = entity.Lines
            .Where(line => line.Price > 0)
            .Select(line => new LastPriceRecord
            {
                LineId = line.Id,
                Dimensions = new LastPriceDimensions
                {
                    ProductId = line.ProductId,
                    StoreId = entity.StoreId
                },
                Details = new LastPriceDetails
                {
                    Price = line.Price
                }
            }).ToList();
        await priceWriter.PostAsync(entity.Id, entity.Date, priceRecords, cancellationToken);
    }

    /// <summary>
    /// The batch of a line: its best-before date as the key of the user's calendar day. A form sends a picked date as local
    /// midnight (the previous UTC day east of UTC), so the same day is one batch whichever way it was entered.
    /// </summary>
    private DateTime BatchDate(PurchaseLine line) => LocalCalendar.BestBeforeKey(line.BestBeforeDate, timeZone.TimeZone);

    public async ValueTask OnUnsubmitAsync(Purchase entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var stockReader = registers.GetReader<IBalanceReader<StockMovement, StockBalance>>();
        var currentBalances = await stockReader.Balances(q => q.Where(b => b.Resources.Quantity > 0), cancellationToken);

        var linesByBatch = entity.Lines
            .GroupBy(l => (l.ProductId, l.LocationId, BestBeforeDate: BatchDate(l)))
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.LocationId,
                g.Key.BestBeforeDate,
                Quantity = g.Sum(x => x.Quantity)
            });

        foreach (var batch in linesByBatch)
        {
            var match = currentBalances.FirstOrDefault(b =>
                b.Dimensions.ProductId == batch.ProductId &&
                b.Dimensions.LocationId == batch.LocationId &&
                b.Dimensions.BestBeforeDate == batch.BestBeforeDate);

            if (match == null || match.Resources.Quantity < batch.Quantity)
            {
                throw new InvalidStateException(localizer["Purchase_CannotUnpostConsumed"].Value);
            }
        }

        var stockWriter = registers.GetWriter<IBalanceRegisterWriter<StockMovement, StockBalance>>();
        await stockWriter.UnpostAsync(entity.Id, cancellationToken);

        var priceWriter = registers.GetWriter<IDocumentInfoRegisterWriter<LastPriceRecord>>();
        await priceWriter.UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var stockWriter = registers.GetWriter<IBalanceRegisterWriter<StockMovement, StockBalance>>();
        await stockWriter.DeleteAsync(id, cancellationToken);

        var priceWriter = registers.GetWriter<IDocumentInfoRegisterWriter<LastPriceRecord>>();
        await priceWriter.DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(
        Purchase entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var stores = (await lookup.GetDictionaryAsync<Store>(new[] { entity.StoreId }, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
        var productIds = entity.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = (await lookup.GetDictionaryAsync<ProductNode>(productIds, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
        var locationIds = entity.Lines.Select(l => l.LocationId).Distinct().ToList();
        var locations = (await lookup.GetDictionaryAsync<LocationNode>(locationIds, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);

        var form = new PurchasePrintForm
        {
            Number = entity.Code ?? "",
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            StoreName = stores.GetValueOrDefault(entity.StoreId) ?? "",
            Total = entity.Total,
            Lines = entity.Lines.Select(l => new PurchasePrintLine
            {
                ProductName = products.GetValueOrDefault(l.ProductId) ?? "",
                Quantity = l.Quantity,
                Price = l.Price,
                Amount = l.Amount,
                LocationName = locations.GetValueOrDefault(l.LocationId) ?? ""
            }).ToList()
        };

        return await renderer.RenderAsync(form, localizer, timeZone ?? TimeZoneInfo.Utc, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class PurchaseMapper : IEntityMapper<Purchase, PurchaseDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial PurchaseDto ToDto(Purchase entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Purchase ToEntity(PurchaseDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial PurchaseLineDto ToDto(PurchaseLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private partial PurchaseLine ToEntity(PurchaseLineDto line);
}

public class PurchaseQueryDtoValidator : PagedQueryDtoValidator<PurchaseQueryDto>;
