using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Entities.ReportingModels;
using HomePantry.Domain.Entities.Registers;
using HomePantry.Persistence.Contexts;
using Kandra.Persistence.Abstractions.Registers;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Persistence.Repositories;

internal class HomePantryReportsRepository(
    HomePantryDbContext dbContext,
    IBalanceReader<StockMovement, StockBalance> stockReader,
    IInfoRegisterReader<LastPriceRecord> priceReader) : IReportsRepository
{
    public async Task<IReadOnlyList<StockOverviewLine>> GetStockOverviewAsync(Guid? locationId, Guid? productGroupId, CancellationToken cancellationToken)
    {
        var productsQuery = dbContext.Dict_Products.OfType<Product>().AsNoTracking().Where(p => !p.IsDeleted);

        if (productGroupId.HasValue && productGroupId.Value != Guid.Empty)
        {
            var groupIds = await GetDescendantGroupIdsAsync(productGroupId.Value, cancellationToken);
            groupIds.Add(productGroupId.Value);
            productsQuery = productsQuery.Where(p => p.ParentId.HasValue && groupIds.Contains(p.ParentId.Value));
        }

        var products = await productsQuery.ToListAsync(cancellationToken);

        List<Guid>? locationIds = null;
        if (locationId.HasValue && locationId.Value != Guid.Empty)
        {
            locationIds = await GetDescendantLocationIdsAsync(locationId.Value, cancellationToken);
            locationIds.Add(locationId.Value);
        }

        var balances = (await stockReader.Balances(
            q => locationIds != null
                ? q.Where(b => b.Resources.Quantity > 0 && locationIds.Contains(b.Dimensions.LocationId))
                : q.Where(b => b.Resources.Quantity > 0),
            cancellationToken)).ToList();

        var balancesByProduct = balances.GroupBy(b => b.Dimensions.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<StockOverviewLine>();

        foreach (var product in products)
        {
            balancesByProduct.TryGetValue(product.Id, out var pBalances);
            var onHand = pBalances?.Sum(b => b.Resources.Quantity) ?? 0m;
            var val = pBalances?.Sum(b => b.Resources.Value) ?? 0m;
            DateTime? nearestDate = null;
            if (pBalances != null)
            {
                var perishable = pBalances.Where(b => b.Dimensions.BestBeforeDate.Year < 9999).ToList();
                if (perishable.Count != 0)
                {
                    nearestDate = perishable.Min(b => b.Dimensions.BestBeforeDate);
                }
            }

            var belowMin = product.MinimumStock > 0 && onHand < product.MinimumStock;

            if (locationId.HasValue && onHand <= 0 && product.MinimumStock <= 0)
            {
                continue;
            }

            result.Add(new StockOverviewLine
            {
                ProductId = product.Id,
                ProductCode = product.Code ?? "",
                ProductName = product.Name ?? "",
                ProductUnit = product.Unit,
                LocationId = locationId,
                Quantity = onHand,
                Value = val,
                NearestBestBeforeDate = nearestDate,
                MinimumStock = product.MinimumStock,
                BelowMinimum = belowMin
            });
        }

        return result.OrderBy(r => r.ProductName).ToList();
    }

    public async Task<IReadOnlyList<ExpiringLine>> GetExpiringAsync(int lookAheadDays, DateTime asOf, CancellationToken cancellationToken)
    {
        var threshold = asOf.Date.AddDays(lookAheadDays);

        var balances = (await stockReader.Balances(
            q => q.Where(b => b.Resources.Quantity > 0 && b.Dimensions.BestBeforeDate.Year < 9999 && b.Dimensions.BestBeforeDate.Date <= threshold),
            cancellationToken)).OrderBy(b => b.Dimensions.BestBeforeDate).ToList();

        if (balances.Count == 0) return [];

        var productIds = balances.Select(b => b.Dimensions.ProductId).Distinct().ToList();
        var locationIds = balances.Select(b => b.Dimensions.LocationId).Distinct().ToList();

        var products = await dbContext.Dict_Products.AsNoTracking().Where(p => !p.IsDeleted && productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var locations = await dbContext.Dict_Locations.AsNoTracking().Where(l => !l.IsDeleted && locationIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, cancellationToken);

        var result = new List<ExpiringLine>();
        foreach (var b in balances)
        {
            var p = products.GetValueOrDefault(b.Dimensions.ProductId);
            var loc = locations.GetValueOrDefault(b.Dimensions.LocationId);

            var daysLeft = (b.Dimensions.BestBeforeDate.Date - asOf.Date).Days;
            result.Add(new ExpiringLine
            {
                ProductId = b.Dimensions.ProductId,
                ProductCode = p?.Code ?? "",
                ProductName = p?.Name ?? "",
                LocationId = b.Dimensions.LocationId,
                LocationCode = loc?.Code ?? "",
                LocationName = loc?.Name ?? "",
                BestBeforeDate = b.Dimensions.BestBeforeDate,
                Quantity = b.Resources.Quantity,
                DaysLeft = daysLeft,
                IsExpired = daysLeft < 0
            });
        }

        return result.OrderBy(r => r.BestBeforeDate).ToList();
    }

    public async Task<IReadOnlyList<ShoppingNeedsLine>> GetShoppingNeedsAsync(Guid? recipeId, decimal? servings, DateTime asOf, CancellationToken cancellationToken)
    {
        var products = await dbContext.Dict_Products.OfType<Product>().AsNoTracking().Where(p => !p.IsDeleted).ToListAsync(cancellationToken);
        var balances = await stockReader.Balances(q => q.Where(b => b.Resources.Quantity > 0), cancellationToken);
        var balancesByProduct = balances.GroupBy(b => b.Dimensions.ProductId).ToDictionary(g => g.Key, g => g.Sum(x => x.Resources.Quantity));

        Recipe? recipe = null;
        if (recipeId.HasValue && recipeId.Value != Guid.Empty)
        {
            recipe = await dbContext.Dict_Recipes
                .Include(r => r.Lines)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => !r.IsDeleted && r.Id == recipeId.Value, cancellationToken);
        }

        var recipeScale = 1m;
        if (recipe != null)
        {
            var baseServings = recipe.Servings > 0 ? recipe.Servings : 1m;
            recipeScale = (servings ?? baseServings) / baseServings;
        }

        var recipeLinesByProduct = recipe?.Lines.GroupBy(l => l.ProductId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity * recipeScale))
            ?? new Dictionary<Guid, decimal>();

        var lastPrices = await priceReader.SliceOfLast(asOf, q => q, cancellationToken);
        var priceByProduct = lastPrices
            .GroupBy(p => p.Dimensions.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Period).First().Details.Price);

        var result = new List<ShoppingNeedsLine>();

        foreach (var product in products)
        {
            var onHand = balancesByProduct.GetValueOrDefault(product.Id, 0m);
            var minShortfall = Math.Max(0m, product.MinimumStock - onHand);

            var recipeNeeded = recipeLinesByProduct.GetValueOrDefault(product.Id, 0m);
            var freeOnHand = Math.Max(0m, onHand - product.MinimumStock);
            var recipeShortfall = Math.Max(0m, recipeNeeded - freeOnHand);

            var totalNeeded = minShortfall + recipeShortfall;

            if (totalNeeded > 0)
            {
                var price = priceByProduct.GetValueOrDefault(product.Id, 0m);
                result.Add(new ShoppingNeedsLine
                {
                    ProductId = product.Id,
                    ProductCode = product.Code ?? "",
                    ProductName = product.Name ?? "",
                    OnHandQuantity = onHand,
                    MinimumStock = product.MinimumStock,
                    ShortfallQuantity = minShortfall,
                    RecipeNeededQuantity = recipeNeeded,
                    RecipeAdditionalShortfall = recipeShortfall,
                    TotalNeededQuantity = totalNeeded,
                    LastPrice = price,
                    EstimatedCost = Math.Round(totalNeeded * price, 2)
                });
            }
        }

        return result.OrderBy(r => r.ProductName).ToList();
    }

    public async Task<IReadOnlyList<CanICookItLine>> GetCanICookItAsync(CancellationToken cancellationToken)
    {
        var recipes = await dbContext.Dict_Recipes.Include(r => r.Lines).AsNoTracking().Where(r => !r.IsDeleted).ToListAsync(cancellationToken);
        var balances = await stockReader.Balances(q => q.Where(b => b.Resources.Quantity > 0), cancellationToken);
        var balancesByProduct = balances.GroupBy(b => b.Dimensions.ProductId).ToDictionary(g => g.Key, g => g.Sum(x => x.Resources.Quantity));
        var products = await dbContext.Dict_Products.AsNoTracking().Where(p => !p.IsDeleted).ToDictionaryAsync(p => p.Id, cancellationToken);

        var result = new List<CanICookItLine>();

        foreach (var r in recipes)
        {
            var missingList = new List<MissingIngredientItem>();
            var canCook = true;

            foreach (var line in r.Lines)
            {
                var onHand = balancesByProduct.GetValueOrDefault(line.ProductId, 0m);
                if (onHand < line.Quantity)
                {
                    canCook = false;
                    var pName = products.GetValueOrDefault(line.ProductId)?.Name ?? "";
                    missingList.Add(new MissingIngredientItem(pName, line.Quantity, onHand));
                }
            }

            result.Add(new CanICookItLine
            {
                RecipeId = r.Id,
                RecipeCode = r.Code ?? "",
                RecipeName = r.Name ?? "",
                Servings = r.Servings,
                CanCook = canCook,
                MissingItems = missingList
            });
        }

        return result.OrderBy(r => r.RecipeName).ToList();
    }

    public async Task<IReadOnlyList<SpendingLine>> GetSpendingAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var purchasesQuery = dbContext.Doc_Purchases.Include(p => p.Lines).AsNoTracking().Where(p => p.IsActive && !p.IsDeleted);

        if (from.HasValue) purchasesQuery = purchasesQuery.Where(p => p.Date >= from.Value);
        if (to.HasValue)
        {
            var toUpper = to.Value.TimeOfDay == TimeSpan.Zero ? to.Value.Date.AddDays(1).AddTicks(-1) : to.Value;
            purchasesQuery = purchasesQuery.Where(p => p.Date <= toUpper);
        }

        var purchases = await purchasesQuery.ToListAsync(cancellationToken);
        var stores = await dbContext.Dict_Stores.AsNoTracking().Where(s => !s.IsDeleted).ToDictionaryAsync(s => s.Id, cancellationToken);
        var products = await dbContext.Dict_Products.OfType<Product>().AsNoTracking().Where(p => !p.IsDeleted).ToDictionaryAsync(p => p.Id, cancellationToken);
        var folders = await dbContext.Dict_Products.OfType<ProductFolder>().AsNoTracking().Where(f => !f.IsDeleted).ToDictionaryAsync(f => f.Id, cancellationToken);

        var grouped = purchases
            .SelectMany(p => p.Lines.Select(l => new { Purchase = p, Line = l }))
            .GroupBy(x => new
            {
                StoreId = x.Purchase.StoreId,
                ProductGroupId = ResolveRootFolderId(products.GetValueOrDefault(x.Line.ProductId)?.ParentId, folders)
            });

        var result = new List<SpendingLine>();

        foreach (var g in grouped)
        {
            var store = stores.GetValueOrDefault(g.Key.StoreId);
            var folder = folders.GetValueOrDefault(g.Key.ProductGroupId);

            result.Add(new SpendingLine
            {
                StoreId = g.Key.StoreId,
                StoreCode = store?.Code ?? "",
                StoreName = store?.Name ?? "",
                ProductGroupId = g.Key.ProductGroupId,
                ProductGroupCode = folder?.Code ?? "",
                ProductGroupName = folder?.Name ?? "",
                PurchasedAmount = g.Sum(x => x.Line.Amount)
            });
        }

        return result.OrderBy(r => r.ProductGroupName).ThenBy(r => r.StoreName).ToList();
    }

    public async Task<decimal> GetTotalStockValueAsync(CancellationToken cancellationToken)
    {
        var balances = await stockReader.Balances(q => q.Where(b => b.Resources.Quantity > 0), cancellationToken);
        return balances.Sum(b => b.Resources.Value);
    }

    public async Task<int> GetBelowMinimumCountAsync(CancellationToken cancellationToken)
    {
        var overview = await GetStockOverviewAsync(null, null, cancellationToken);
        return overview.Count(o => o.BelowMinimum);
    }

    public async Task<int> GetExpiringBatchesCountAsync(int lookAheadDays, DateTime asOf, CancellationToken cancellationToken)
    {
        var expiring = await GetExpiringAsync(lookAheadDays, asOf, cancellationToken);
        return expiring.Count;
    }

    public async Task<(decimal thisMonth, decimal lastMonth)> GetMonthlySpendingComparisonAsync(DateTime asOf, CancellationToken cancellationToken)
    {
        var thisMonthStart = new DateTime(asOf.Year, asOf.Month, 1);
        var thisMonthEnd = thisMonthStart.AddMonths(1);

        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart;

        var thisMonthSpending = await dbContext.Doc_Purchases
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted && p.Date >= thisMonthStart && p.Date < thisMonthEnd)
            .SumAsync(p => p.Total, cancellationToken);

        var lastMonthSpending = await dbContext.Doc_Purchases
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted && p.Date >= lastMonthStart && p.Date < lastMonthEnd)
            .SumAsync(p => p.Total, cancellationToken);

        return (thisMonthSpending, lastMonthSpending);
    }

    public async Task<IReadOnlyList<(string MonthKey, string MonthLabel, decimal Amount)>> GetSpendingHistoryAsync(DateTime asOf, int months, CancellationToken cancellationToken)
    {
        var startMonth = new DateTime(asOf.Year, asOf.Month, 1).AddMonths(-(months - 1));
        var endMonth = new DateTime(asOf.Year, asOf.Month, 1).AddMonths(1);

        var purchases = await dbContext.Doc_Purchases
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted && p.Date >= startMonth && p.Date < endMonth)
            .ToListAsync(cancellationToken);

        var result = new List<(string MonthKey, string MonthLabel, decimal Amount)>();
        for (int i = 0; i < months; i++)
        {
            var m = startMonth.AddMonths(i);
            var mNext = m.AddMonths(1);
            var total = purchases.Where(p => p.Date >= m && p.Date < mNext).Sum(p => p.Total);
            result.Add((m.ToString("yyyy-MM"), m.ToString("yyyy-MM"), total));
        }

        return result;
    }

    public async Task<IReadOnlyList<(string GroupName, decimal Value)>> GetStockValueByGroupAsync(CancellationToken cancellationToken)
    {
        var balances = (await stockReader.Balances(q => q.Where(b => b.Resources.Quantity > 0), cancellationToken)).ToList();

        if (balances.Count == 0) return [];

        var productIds = balances.Select(b => b.Dimensions.ProductId).Distinct().ToList();
        var products = await dbContext.Dict_Products.OfType<Product>().AsNoTracking().Where(p => !p.IsDeleted && productIds.Contains(p.Id)).ToListAsync(cancellationToken);
        var folders = await dbContext.Dict_Products.OfType<ProductFolder>().AsNoTracking().Where(f => !f.IsDeleted).ToDictionaryAsync(f => f.Id, cancellationToken);

        var productToGroup = products.ToDictionary(
            p => p.Id,
            p => folders.TryGetValue(ResolveRootFolderId(p.ParentId, folders), out var f) ? f.Name ?? "" : "");

        var groupValues = balances
            .GroupBy(b => productToGroup.GetValueOrDefault(b.Dimensions.ProductId, ""))
            .Select(g => (GroupName: g.Key, Value: g.Sum(b => b.Resources.Value)))
            .OrderByDescending(x => x.Value)
            .ToList();

        if (groupValues.Count <= 5) return groupValues;

        var top5 = groupValues.Take(5).ToList();
        var othersValue = groupValues.Skip(5).Sum(x => x.Value);
        if (othersValue > 0)
        {
            top5.Add(("", othersValue));
        }

        return top5;
    }

    public async Task<IReadOnlyList<ExpiringLine>> GetNextExpiringBatchesAsync(DateTime asOf, int count, CancellationToken cancellationToken)
    {
        var balances = (await stockReader.Balances(
            q => q.Where(b => b.Resources.Quantity > 0 && b.Dimensions.BestBeforeDate.Year < 9999),
            cancellationToken))
            .OrderBy(b => b.Dimensions.BestBeforeDate)
            .Take(count)
            .ToList();

        if (balances.Count == 0) return [];

        var productIds = balances.Select(b => b.Dimensions.ProductId).Distinct().ToList();
        var locationIds = balances.Select(b => b.Dimensions.LocationId).Distinct().ToList();

        var products = await dbContext.Dict_Products.AsNoTracking().Where(p => !p.IsDeleted && productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var locations = await dbContext.Dict_Locations.AsNoTracking().Where(l => !l.IsDeleted && locationIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, cancellationToken);

        var result = new List<ExpiringLine>();
        foreach (var b in balances)
        {
            var p = products.GetValueOrDefault(b.Dimensions.ProductId);
            var loc = locations.GetValueOrDefault(b.Dimensions.LocationId);

            var daysLeft = (b.Dimensions.BestBeforeDate.Date - asOf.Date).Days;
            result.Add(new ExpiringLine
            {
                ProductId = b.Dimensions.ProductId,
                ProductCode = p?.Code ?? "",
                ProductName = p?.Name ?? "",
                LocationId = b.Dimensions.LocationId,
                LocationCode = loc?.Code ?? "",
                LocationName = loc?.Name ?? "",
                BestBeforeDate = b.Dimensions.BestBeforeDate,
                Quantity = b.Resources.Quantity,
                DaysLeft = daysLeft,
                IsExpired = daysLeft < 0
            });
        }

        return result;
    }

    private static Guid ResolveRootFolderId(Guid? parentId, IReadOnlyDictionary<Guid, ProductFolder> folders)
    {
        if (!parentId.HasValue || parentId.Value == Guid.Empty) return Guid.Empty;
        var currentId = parentId.Value;
        var visited = new HashSet<Guid> { currentId };
        while (folders.TryGetValue(currentId, out var folder) &&
               folder.ParentId is { } nextId &&
               nextId != Guid.Empty &&
               folders.ContainsKey(nextId) &&
               visited.Add(nextId))
        {
            currentId = nextId;
        }
        return currentId;
    }

    private async Task<List<Guid>> GetDescendantGroupIdsAsync(Guid parentId, CancellationToken cancellationToken)
    {
        var childFolders = await dbContext.Dict_Products
            .OfType<ProductFolder>()
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.ParentId == parentId)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        var allIds = new List<Guid>(childFolders);
        foreach (var childId in childFolders)
        {
            allIds.AddRange(await GetDescendantGroupIdsAsync(childId, cancellationToken));
        }

        return allIds;
    }

    private async Task<List<Guid>> GetDescendantLocationIdsAsync(Guid parentId, CancellationToken cancellationToken)
    {
        var children = await dbContext.Dict_Locations
            .AsNoTracking()
            .Where(l => !l.IsDeleted && l.ParentId == parentId)
            .Select(l => new { l.Id, IsFolder = l is LocationFolder })
            .ToListAsync(cancellationToken);

        var allIds = new List<Guid>(children.Select(c => c.Id));
        foreach (var child in children.Where(c => c.IsFolder))
        {
            allIds.AddRange(await GetDescendantLocationIdsAsync(child.Id, cancellationToken));
        }

        return allIds;
    }
}
