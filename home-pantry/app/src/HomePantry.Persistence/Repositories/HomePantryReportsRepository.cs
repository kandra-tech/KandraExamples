using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Entities.Documents;
using HomePantry.Domain.Entities.ReportingModels;
using HomePantry.Domain.Entities.Registers;
using HomePantry.Enums;
using HomePantry.Persistence.Contexts;
using Kandra.Persistence.Abstractions.Registers;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Persistence.Repositories;

internal class HomePantryReportsRepository(
    HomePantryDbContext dbContext,
    IBalanceReader<StockMovement, StockBalance> stockReader,
    IInfoRegisterReader<LastPriceRecord> priceReader,
    IInfoRegisterReader<ChoreLogRecord> choreLogReader,
    IInfoRegisterReader<BatteryLogRecord> batteryLogReader,
    ITurnoverReader<BatteryCyclesMovement> batteryCyclesReader,
    IInfoRegisterReader<PlannedMealsRecord> plannedMealsReader) : IReportsRepository
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

    public Task<IReadOnlyList<ShoppingNeedsLine>> GetShoppingNeedsAsync(Guid? recipeId, decimal? servings, DateTime asOf, CancellationToken cancellationToken)
        => GetShoppingNeedsAsync(recipeId, servings, asOf, null, null, cancellationToken);

    public async Task<IReadOnlyList<ShoppingNeedsLine>> GetShoppingNeedsAsync(Guid? recipeId, decimal? servings, DateTime asOf, DateTime? mealPlanWeek, Guid? mealPlanId, CancellationToken cancellationToken)
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

        if (mealPlanId.HasValue && mealPlanId.Value != Guid.Empty)
        {
            var planDoc = await dbContext.Set<MealPlan>().AsNoTracking().FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == mealPlanId.Value, cancellationToken);
            if (planDoc != null)
            {
                mealPlanWeek = planDoc.WeekStartDate;
            }
        }

        var mealPlanLinesByProduct = new Dictionary<Guid, decimal>();
        if (mealPlanWeek.HasValue)
        {
            var d = mealPlanWeek.Value.Date;
            var diff = (7 + (int)d.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var monday = d.AddDays(-diff);
            var sunday = monday.AddDays(7).AddTicks(-1);

            var plannedMeals = await plannedMealsReader.Records(
                DateTime.MinValue,
                DateTime.MaxValue,
                q => q.Where(m => m.Dimensions.Date >= monday && m.Dimensions.Date <= sunday),
                cancellationToken);
            var planRecipeIds = plannedMeals.Select(p => p.Dimensions.RecipeId).Distinct().ToList();
            var planRecipes = await dbContext.Dict_Recipes
                .Include(r => r.Lines)
                .AsNoTracking()
                .Where(r => !r.IsDeleted && planRecipeIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, cancellationToken);

            foreach (var pm in plannedMeals)
            {
                if (planRecipes.TryGetValue(pm.Dimensions.RecipeId, out var planRecipe))
                {
                    var basePlanServings = planRecipe.Servings > 0 ? planRecipe.Servings : 1m;
                    var planScale = pm.Details.Servings / basePlanServings;
                    foreach (var pl in planRecipe.Lines)
                    {
                        mealPlanLinesByProduct[pl.ProductId] = mealPlanLinesByProduct.GetValueOrDefault(pl.ProductId, 0m) + (pl.Quantity * planScale);
                    }
                }
            }
        }

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
            var mealPlanNeeded = mealPlanLinesByProduct.GetValueOrDefault(product.Id, 0m);
            var totalAdditionalNeeded = recipeNeeded + mealPlanNeeded;

            var freeOnHand = Math.Max(0m, onHand - product.MinimumStock);
            var additionalShortfall = Math.Max(0m, totalAdditionalNeeded - freeOnHand);

            var totalNeeded = minShortfall + additionalShortfall;

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
                    RecipeNeededQuantity = recipeNeeded + mealPlanNeeded,
                    RecipeAdditionalShortfall = additionalShortfall,
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

            foreach (var group in r.Lines.GroupBy(l => l.ProductId))
            {
                var needed = group.Sum(l => l.Quantity);
                var onHand = balancesByProduct.GetValueOrDefault(group.Key, 0m);
                if (onHand < needed)
                {
                    canCook = false;
                    var pName = products.GetValueOrDefault(group.Key)?.Name ?? "";
                    missingList.Add(new MissingIngredientItem(pName, needed, onHand));
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

    public async Task<IReadOnlyList<ChoresReportLine>> GetChoresAsync(DateTime asOf, CancellationToken cancellationToken)
    {
        var sliceAt = asOf.TimeOfDay == TimeSpan.Zero ? asOf.Date.AddDays(1).AddTicks(-1) : asOf;
        var chores = await dbContext.Set<Chore>().Include(c => c.AssignedTo).AsNoTracking().Where(c => !c.IsDeleted).ToListAsync(cancellationToken);
        var members = await dbContext.Set<HouseholdMember>().AsNoTracking().Where(m => !m.IsDeleted).ToDictionaryAsync(m => m.Id, cancellationToken);
        var lastLogs = await choreLogReader.SliceOfLast(sliceAt, q => q, cancellationToken);
        var lastLogByChore = lastLogs.GroupBy(l => l.Dimensions.ChoreId).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Period).First());

        var result = new List<ChoresReportLine>();
        foreach (var chore in chores)
        {
            lastLogByChore.TryGetValue(chore.Id, out var lastLog);
            DateTime? lastDoneDate = lastLog?.Period;
            string? doneByName = null;
            if (lastLog != null && members.TryGetValue(lastLog.Details.DoneById, out var doneBy))
            {
                doneByName = doneBy.Name;
            }

            DateTime nextDueDate;
            if (lastDoneDate.HasValue)
            {
                nextDueDate = chore.Recurrence switch
                {
                    ChoreRecurrence.Daily => lastDoneDate.Value.Date.AddDays(1),
                    ChoreRecurrence.Weekly => lastDoneDate.Value.Date.AddDays(7),
                    ChoreRecurrence.Monthly => lastDoneDate.Value.Date.AddMonths(1),
                    ChoreRecurrence.EveryNDays => lastDoneDate.Value.Date.AddDays(chore.EveryNDays > 0 ? chore.EveryNDays : 1),
                    _ => lastDoneDate.Value.Date.AddDays(1)
                };
            }
            else
            {
                var createdDate = chore.Created == default ? asOf.Date : chore.Created.Date;
                nextDueDate = createdDate;
            }

            var isOverdue = nextDueDate < asOf.Date;

            result.Add(new ChoresReportLine
            {
                ChoreId = chore.Id,
                ChoreCode = chore.Code ?? "",
                ChoreName = chore.Name ?? "",
                Recurrence = chore.Recurrence,
                LastDoneDate = lastDoneDate,
                DoneByName = doneByName,
                NextDueDate = nextDueDate,
                IsOverdue = isOverdue
            });
        }
        return result.OrderBy(x => x.NextDueDate).ToList();
    }

    public async Task<IReadOnlyList<BatteriesReportLine>> GetBatteriesAsync(DateTime asOf, CancellationToken cancellationToken)
    {
        var sliceAt = asOf.TimeOfDay == TimeSpan.Zero ? asOf.Date.AddDays(1).AddTicks(-1) : asOf;
        var batteries = await dbContext.Set<Battery>().AsNoTracking().Where(b => !b.IsDeleted).ToListAsync(cancellationToken);
        var lastLogs = await batteryLogReader.SliceOfLast(sliceAt, q => q, cancellationToken);
        var lastLogByBattery = lastLogs.GroupBy(l => l.Dimensions.BatteryId).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Period).First());

        var movements = await batteryCyclesReader.Movements(DateTime.MinValue, sliceAt, q => q, cancellationToken);
        var cyclesByBattery = movements
            .GroupBy(m => m.Dimensions.BatteryId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.IsExpense ? -x.Resources.Cycles : x.Resources.Cycles));

        var result = new List<BatteriesReportLine>();
        foreach (var b in batteries)
        {
            lastLogByBattery.TryGetValue(b.Id, out var lastLog);
            DateTime? lastCharge = lastLog?.Period;
            DateTime nextDue = lastCharge.HasValue
                ? lastCharge.Value.Date.AddDays(b.ChargeIntervalDays)
                : (b.Created == default ? asOf.Date : b.Created.Date).AddDays(b.ChargeIntervalDays);

            var isOverdue = nextDue < asOf.Date;
            var totalCycles = cyclesByBattery.GetValueOrDefault(b.Id, 0m);

            result.Add(new BatteriesReportLine
            {
                BatteryId = b.Id,
                BatteryCode = b.Code ?? "",
                BatteryName = b.Name ?? "",
                UsedIn = b.UsedIn,
                BatteryKind = b.BatteryKind,
                ChargeIntervalDays = b.ChargeIntervalDays,
                LastChargeDate = lastCharge,
                NextDueDate = nextDue,
                IsOverdue = isOverdue,
                TotalCycles = totalCycles
            });
        }
        return result.OrderBy(x => x.NextDueDate).ToList();
    }

    public async Task<IReadOnlyList<EquipmentReportLine>> GetEquipmentWarrantiesAsync(int withinDays, DateTime asOf, CancellationToken cancellationToken)
    {
        var threshold = asOf.Date.AddDays(withinDays);
        var equipments = await dbContext.Set<Equipment>().Include(e => e.Location).AsNoTracking().Where(e => !e.IsDeleted).ToListAsync(cancellationToken);
        var filtered = equipments.Where(e => e.WarrantyUntil.HasValue && e.WarrantyUntil.Value.Date <= threshold).ToList();

        var result = new List<EquipmentReportLine>();
        foreach (var e in filtered)
        {
            var daysLeft = (int)(e.WarrantyUntil!.Value.Date - asOf.Date).TotalDays;
            result.Add(new EquipmentReportLine
            {
                EquipmentId = e.Id,
                EquipmentCode = e.Code ?? "",
                EquipmentName = e.Name ?? "",
                LocationId = e.LocationId,
                LocationName = e.Location?.Name ?? "",
                BoughtOn = e.BoughtOn,
                WarrantyUntil = e.WarrantyUntil,
                DaysLeft = daysLeft,
                HasManual = e.ManualFileId.HasValue && e.ManualFileId.Value != Guid.Empty,
                HasPhoto = e.PhotoFileId.HasValue && e.PhotoFileId.Value != Guid.Empty
            });
        }
        return result.OrderBy(x => x.WarrantyUntil).ToList();
    }

    public async Task<IReadOnlyList<WeekPlanReportLine>> GetWeekPlanAsync(DateTime weekStart, CancellationToken cancellationToken)
    {
        var d = weekStart.Date;
        var diff = (7 + (int)d.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        var monday = d.AddDays(-diff);
        var sunday = monday.AddDays(7).AddTicks(-1);

        var plannedMeals = await plannedMealsReader.Records(
            DateTime.MinValue,
            DateTime.MaxValue,
            q => q.Where(m => m.Dimensions.Date >= monday && m.Dimensions.Date <= sunday),
            cancellationToken);
        var recipeIds = plannedMeals.Select(p => p.Dimensions.RecipeId).Distinct().ToList();
        var recipes = await dbContext.Dict_Recipes.Include(r => r.Lines).AsNoTracking().Where(r => !r.IsDeleted && recipeIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, cancellationToken);

        var balances = await stockReader.Balances(q => q.Where(b => b.Resources.Quantity > 0), cancellationToken);
        var balancesByProduct = balances.GroupBy(b => b.Dimensions.ProductId).ToDictionary(g => g.Key, g => g.Sum(x => x.Resources.Quantity));

        var result = new List<WeekPlanReportLine>();
        foreach (var meal in plannedMeals.OrderBy(m => m.Dimensions.Date))
        {
            recipes.TryGetValue(meal.Dimensions.RecipeId, out var recipe);
            var canCook = true;
            if (recipe != null)
            {
                var baseServings = recipe.Servings > 0 ? recipe.Servings : 1m;
                var scale = meal.Details.Servings / baseServings;
                foreach (var group in recipe.Lines.GroupBy(l => l.ProductId))
                {
                    var needed = group.Sum(l => l.Quantity * scale);
                    var onHand = balancesByProduct.GetValueOrDefault(group.Key, 0m);
                    if (onHand < needed)
                    {
                        canCook = false;
                        break;
                    }
                }
            }
            else
            {
                canCook = false;
            }

            var mealDate = meal.Dimensions.Date;
            var dayEnum = (MealPlanDay)(((int)mealDate.DayOfWeek == 0 ? 7 : (int)mealDate.DayOfWeek));

            result.Add(new WeekPlanReportLine
            {
                Date = mealDate,
                Day = dayEnum,
                RecipeId = meal.Dimensions.RecipeId,
                RecipeCode = recipe?.Code ?? "",
                RecipeName = recipe?.Name ?? "",
                Servings = meal.Details.Servings,
                CanCook = canCook
            });
        }
        return result;
    }

    public async Task<IReadOnlyList<CalendarReportLine>> GetCalendarAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var fromDate = from.Date;
        var toUpper = to.Date.AddDays(1).AddTicks(-1);
        var chores = await GetChoresAsync(toUpper, cancellationToken);
        var batteries = await GetBatteriesAsync(toUpper, cancellationToken);
        var plannedMeals = await plannedMealsReader.Records(
            DateTime.MinValue,
            DateTime.MaxValue,
            q => q.Where(m => m.Dimensions.Date >= fromDate && m.Dimensions.Date <= toUpper),
            cancellationToken);
        var recipeIds = plannedMeals.Select(p => p.Dimensions.RecipeId).Distinct().ToList();
        var recipes = await dbContext.Dict_Recipes.AsNoTracking().Where(r => !r.IsDeleted && recipeIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, cancellationToken);

        var result = new List<CalendarReportLine>();
        for (var day = fromDate; day <= to.Date; day = day.AddDays(1))
        {
            var dayChores = chores.Where(c => c.NextDueDate.Date == day).Select(c => c.ChoreName).ToList();
            var dayBatteries = batteries.Where(b => b.NextDueDate.Date == day).Select(b => b.BatteryName).ToList();
            var dayMeals = plannedMeals.Where(m => m.Dimensions.Date.Date == day)
                .Select(m =>
                {
                    var name = recipes.GetValueOrDefault(m.Dimensions.RecipeId)?.Name ?? "";
                    return $"{name} (×{m.Details.Servings:0.##})";
                }).ToList();

            result.Add(new CalendarReportLine
            {
                Date = day,
                DayOfWeek = day.DayOfWeek.ToString(),
                ChoresDue = string.Join(", ", dayChores),
                BatteriesDue = string.Join(", ", dayBatteries),
                PlannedMeals = string.Join(", ", dayMeals)
            });
        }
        return result;
    }

    public async Task<int> GetOverdueChoresCountAsync(DateTime asOf, CancellationToken cancellationToken)
    {
        var chores = await GetChoresAsync(asOf, cancellationToken);
        return chores.Count(c => c.IsOverdue);
    }

    public async Task<int> GetBatteriesDueCountAsync(DateTime asOf, CancellationToken cancellationToken)
    {
        var batteries = await GetBatteriesAsync(asOf, cancellationToken);
        return batteries.Count(b => b.NextDueDate.Date <= asOf.Date);
    }

    public async Task<IReadOnlyList<TodayScheduleItem>> GetTodayScheduleAsync(DateTime today, CancellationToken cancellationToken)
    {
        var todayStart = today.Date;
        var todayEnd = today.Date.AddDays(1).AddTicks(-1);
        var chores = await GetChoresAsync(today, cancellationToken);
        var todayChores = chores.Where(c => c.NextDueDate.Date <= today.Date).Select(c => new TodayScheduleItem
        {
            IsMeal = false,
            ItemName = c.ChoreName,
            DoneByName = c.DoneByName,
            IsOverdue = c.IsOverdue
        });

        var plannedMeals = await plannedMealsReader.Records(
            DateTime.MinValue,
            DateTime.MaxValue,
            q => q.Where(m => m.Dimensions.Date >= todayStart && m.Dimensions.Date <= todayEnd),
            cancellationToken);
        var recipeIds = plannedMeals.Select(p => p.Dimensions.RecipeId).Distinct().ToList();
        var recipes = await dbContext.Dict_Recipes.AsNoTracking().Where(r => !r.IsDeleted && recipeIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, cancellationToken);
        var todayMeals = plannedMeals.Select(m => new TodayScheduleItem
        {
            IsMeal = true,
            ItemName = recipes.GetValueOrDefault(m.Dimensions.RecipeId)?.Name ?? string.Empty,
            Servings = m.Details.Servings
        });

        return todayChores.Concat(todayMeals).ToList();
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
