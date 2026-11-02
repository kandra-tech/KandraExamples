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
    public async Task<IReadOnlyList<StockOverviewLine>> GetStockOverviewAsync(Guid? locationId, Guid? productGroupId, TimeZoneInfo? zone, CancellationToken cancellationToken)
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
                var perishable = pBalances.Where(b => !LocalCalendar.IsNoExpiry(b.Dimensions.BestBeforeDate)).ToList();
                if (perishable.Count != 0)
                {
                    nearestDate = LocalCalendar.BestBeforeShown(perishable.Min(b => b.Dimensions.BestBeforeDate), zone);
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

    public async Task<IReadOnlyList<ExpiringLine>> GetExpiringAsync(int lookAheadDays, DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var today = LocalCalendar.Today(asOf, zone);
        var beyond = LocalCalendar.Key(today.AddDays(lookAheadDays + 1));

        var balances = (await stockReader.Balances(
            q => q.Where(b => b.Resources.Quantity > 0 && b.Dimensions.BestBeforeDate < beyond),
            cancellationToken)).OrderBy(b => b.Dimensions.BestBeforeDate).ToList();

        return await ToExpiringLinesAsync(balances, today, zone, cancellationToken);
    }

    public Task<IReadOnlyList<ShoppingNeedsLine>> GetShoppingNeedsAsync(Guid? recipeId, decimal? servings, DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken)
        => GetShoppingNeedsAsync(recipeId, servings, asOf, null, null, zone, cancellationToken);

    public async Task<IReadOnlyList<ShoppingNeedsLine>> GetShoppingNeedsAsync(Guid? recipeId, decimal? servings, DateTime asOf, DateTime? mealPlanWeek, Guid? mealPlanId, TimeZoneInfo? zone, CancellationToken cancellationToken)
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
            var monday = LocalCalendar.MondayOf(LocalCalendar.DateOf(mealPlanWeek.Value, zone));
            var plannedMeals = await PlannedMealsAsync(monday, monday.AddDays(6), cancellationToken);
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

    public async Task<IReadOnlyList<SpendingLine>> GetSpendingAsync(DateTime? from, DateTime? to, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var purchasesQuery = dbContext.Doc_Purchases.Include(p => p.Lines).AsNoTracking().Where(p => p.IsActive && !p.IsDeleted);

        // A period the user picks is local days: from local midnight of the first to local midnight after the last.
        if (from.HasValue)
        {
            var start = LocalCalendar.MidnightUtc(LocalCalendar.DateOf(from.Value, zone), zone);
            purchasesQuery = purchasesQuery.Where(p => p.Date >= start);
        }
        if (to.HasValue)
        {
            var end = LocalCalendar.MidnightUtc(LocalCalendar.DateOf(to.Value, zone).AddDays(1), zone);
            purchasesQuery = purchasesQuery.Where(p => p.Date < end);
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
        var overview = await GetStockOverviewAsync(null, null, null, cancellationToken);
        return overview.Count(o => o.BelowMinimum);
    }

    public async Task<int> GetExpiringBatchesCountAsync(int lookAheadDays, DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var expiring = await GetExpiringAsync(lookAheadDays, asOf, zone, cancellationToken);
        return expiring.Count;
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

    public async Task<IReadOnlyList<ExpiringLine>> GetNextExpiringBatchesAsync(DateTime asOf, int count, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var balances = (await stockReader.Balances(
            q => q.Where(b => b.Resources.Quantity > 0 && b.Dimensions.BestBeforeDate < LocalCalendar.NoExpiry),
            cancellationToken))
            .OrderBy(b => b.Dimensions.BestBeforeDate)
            .Take(count)
            .ToList();

        return await ToExpiringLinesAsync(balances, LocalCalendar.Today(asOf, zone), zone, cancellationToken);
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

    public async Task<IReadOnlyList<ChoresReportLine>> GetChoresAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken) =>
        (await ChoreDueDatesAsync(asOf, zone, cancellationToken)).Select(c => c.Line).OrderBy(x => x.NextDueDate).ToList();


    public async Task<IReadOnlyList<BatteriesReportLine>> GetBatteriesAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken) =>
        (await BatteryDueDatesAsync(asOf, zone, cancellationToken)).Select(b => b.Line).OrderBy(x => x.NextDueDate).ToList();


    public async Task<IReadOnlyList<EquipmentReportLine>> GetEquipmentWarrantiesAsync(int withinDays, DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var today = LocalCalendar.Today(asOf, zone);
        var threshold = today.AddDays(withinDays);
        var equipments = await dbContext.Set<Equipment>().Include(e => e.Location).AsNoTracking().Where(e => !e.IsDeleted).ToListAsync(cancellationToken);
        var filtered = equipments.Where(e => e.WarrantyUntil.HasValue && LocalCalendar.DateOf(e.WarrantyUntil.Value, zone) <= threshold).ToList();

        var result = new List<EquipmentReportLine>();
        foreach (var e in filtered)
        {
            var daysLeft = (LocalCalendar.DateOf(e.WarrantyUntil!.Value, zone) - today).Days;
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

    public async Task<IReadOnlyList<WeekPlanReportLine>> GetWeekPlanAsync(DateTime weekStart, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var monday = LocalCalendar.MondayOf(LocalCalendar.DateOf(weekStart, zone));
        var plannedMeals = await PlannedMealsAsync(monday, monday.AddDays(6), cancellationToken);
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

            // The dimension is the key of the meal's calendar day.
            var mealDate = meal.Dimensions.Date.Date;
            var dayEnum = (MealPlanDay)(((int)mealDate.DayOfWeek == 0 ? 7 : (int)mealDate.DayOfWeek));

            result.Add(new WeekPlanReportLine
            {
                Date = LocalCalendar.MidnightUtc(mealDate, zone),
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

    public async Task<IReadOnlyList<CalendarReportLine>> GetCalendarAsync(DateTime from, DateTime to, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var fromDate = LocalCalendar.DateOf(from, zone);
        var toDate = LocalCalendar.DateOf(to, zone);
        var endOfPeriod = LocalCalendar.MidnightUtc(toDate.AddDays(1), zone).AddTicks(-1);

        // Every day a chore or a battery falls due in the period, on the assumption that it is done when due; one already
        // overdue is shown on the first day.
        var due = new Dictionary<DateTime, (List<string> Chores, List<string> Batteries)>();
        void Add(DateTime day, string name, bool isChore)
        {
            if (!due.TryGetValue(day, out var lists)) due[day] = lists = ([], []);
            (isChore ? lists.Chores : lists.Batteries).Add(name);
        }
        foreach (var (line, nextDue, next) in await ChoreDueDatesAsync(endOfPeriod, zone, cancellationToken))
        {
            for (var day = nextDue < fromDate ? fromDate : nextDue; day <= toDate; day = next(day))
                Add(day, line.ChoreName, isChore: true);
        }
        foreach (var (line, nextDue, intervalDays) in await BatteryDueDatesAsync(endOfPeriod, zone, cancellationToken))
        {
            for (var day = nextDue < fromDate ? fromDate : nextDue; day <= toDate; day = day.AddDays(Math.Max(intervalDays, 1)))
                Add(day, line.BatteryName, isChore: false);
        }

        var plannedMeals = await PlannedMealsAsync(fromDate, toDate, cancellationToken);
        var recipeIds = plannedMeals.Select(p => p.Dimensions.RecipeId).Distinct().ToList();
        var recipes = await dbContext.Dict_Recipes.AsNoTracking().Where(r => !r.IsDeleted && recipeIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, cancellationToken);

        var result = new List<CalendarReportLine>();
        for (var day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            due.TryGetValue(day, out var dayDue);
            var dayMeals = plannedMeals.Where(m => m.Dimensions.Date.Date == day)
                .Select(m =>
                {
                    var name = recipes.GetValueOrDefault(m.Dimensions.RecipeId)?.Name ?? "";
                    return $"{name} (×{m.Details.Servings:0.##})";
                }).ToList();

            result.Add(new CalendarReportLine
            {
                Date = LocalCalendar.MidnightUtc(day, zone),
                DayOfWeek = day.DayOfWeek.ToString(),
                ChoresDue = string.Join(", ", dayDue.Chores ?? []),
                BatteriesDue = string.Join(", ", dayDue.Batteries ?? []),
                PlannedMeals = string.Join(", ", dayMeals)
            });
        }
        return result;
    }

    public async Task<int> GetOverdueChoresCountAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var chores = await GetChoresAsync(asOf, zone, cancellationToken);
        return chores.Count(c => c.IsOverdue);
    }

    public async Task<int> GetBatteriesDueCountAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var today = LocalCalendar.Today(asOf, zone);
        return (await BatteryDueDatesAsync(asOf, zone, cancellationToken)).Count(b => b.NextDue <= today);
    }

    public async Task<IReadOnlyList<TodayScheduleItem>> GetTodayScheduleAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var today = LocalCalendar.Today(asOf, zone);
        var todayChores = (await ChoreDueDatesAsync(asOf, zone, cancellationToken))
            .Where(c => c.NextDue <= today)
            .Select(c => new TodayScheduleItem
            {
                IsMeal = false,
                ItemName = c.Line.ChoreName,
                DoneByName = c.Line.DoneByName,
                IsOverdue = c.Line.IsOverdue
            });

        var plannedMeals = await PlannedMealsAsync(today, today, cancellationToken);
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

    public async Task<IReadOnlyList<decimal>> GetSpendingPerPeriodAsync(IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> periods, CancellationToken cancellationToken)
    {
        if (periods.Count == 0) return [];
        var from = periods.Min(p => p.StartUtc);
        var to = periods.Max(p => p.EndUtc);

        var purchases = await dbContext.Doc_Purchases
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted && p.Date >= from && p.Date < to)
            .Select(p => new { p.Date, p.Total })
            .ToListAsync(cancellationToken);

        return periods.Select(period => purchases.Where(p => p.Date >= period.StartUtc && p.Date < period.EndUtc).Sum(p => p.Total)).ToList();
    }

    private async Task<IReadOnlyList<ExpiringLine>> ToExpiringLinesAsync(List<StockBalance> balances, DateTime today, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
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

            // The dimension is the key of a calendar date (midnight UTC), so its date is the local best-before date.
            var daysLeft = (b.Dimensions.BestBeforeDate.Date - today).Days;
            result.Add(new ExpiringLine
            {
                ProductId = b.Dimensions.ProductId,
                ProductCode = p?.Code ?? "",
                ProductName = p?.Name ?? "",
                LocationId = b.Dimensions.LocationId,
                LocationCode = loc?.Code ?? "",
                LocationName = loc?.Name ?? "",
                BestBeforeDate = LocalCalendar.BestBeforeShown(b.Dimensions.BestBeforeDate, zone),
                Quantity = b.Resources.Quantity,
                DaysLeft = daysLeft,
                IsExpired = daysLeft < 0
            });
        }

        return result.OrderBy(r => r.BestBeforeDate).ToList();
    }

    private async Task<List<(ChoresReportLine Line, DateTime NextDue, Func<DateTime, DateTime> Next)>> ChoreDueDatesAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var today = LocalCalendar.Today(asOf, zone);
        var sliceAt = LocalCalendar.MidnightUtc(today.AddDays(1), zone).AddTicks(-1);
        var chores = await dbContext.Set<Chore>().Include(c => c.AssignedTo).AsNoTracking().Where(c => !c.IsDeleted).ToListAsync(cancellationToken);
        var members = await dbContext.Set<HouseholdMember>().AsNoTracking().Where(m => !m.IsDeleted).ToDictionaryAsync(m => m.Id, cancellationToken);
        var lastLogs = await choreLogReader.SliceOfLast(sliceAt, q => q, cancellationToken);
        var lastLogByChore = lastLogs.GroupBy(l => l.Dimensions.ChoreId).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Period).First());

        var result = new List<(ChoresReportLine, DateTime, Func<DateTime, DateTime>)>();
        foreach (var chore in chores)
        {
            lastLogByChore.TryGetValue(chore.Id, out var lastLog);
            DateTime? lastDoneDate = lastLog?.Period;
            string? doneByName = null;
            if (lastLog != null && members.TryGetValue(lastLog.Details.DoneById, out var doneBy))
            {
                doneByName = doneBy.Name;
            }

            Func<DateTime, DateTime> next = chore.Recurrence switch
            {
                ChoreRecurrence.Weekly => d => d.AddDays(7),
                ChoreRecurrence.Monthly => d => d.AddMonths(1),
                ChoreRecurrence.EveryNDays => d => d.AddDays(chore.EveryNDays > 0 ? chore.EveryNDays : 1),
                _ => d => d.AddDays(1)
            };

            // Never done: due from the local day it was created.
            var nextDue = lastDoneDate.HasValue
                ? next(LocalCalendar.DateOf(lastDoneDate.Value, zone))
                : chore.Created == default ? today : LocalCalendar.DateOf(chore.Created, zone);

            result.Add((new ChoresReportLine
            {
                ChoreId = chore.Id,
                ChoreCode = chore.Code ?? "",
                ChoreName = chore.Name ?? "",
                Recurrence = chore.Recurrence,
                LastDoneDate = lastDoneDate,
                DoneByName = doneByName,
                NextDueDate = LocalCalendar.MidnightUtc(nextDue, zone),
                IsOverdue = nextDue < today
            }, nextDue, next));
        }
        return result;
    }

    private async Task<List<(BatteriesReportLine Line, DateTime NextDue, int IntervalDays)>> BatteryDueDatesAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken)
    {
        var today = LocalCalendar.Today(asOf, zone);
        var sliceAt = LocalCalendar.MidnightUtc(today.AddDays(1), zone).AddTicks(-1);
        var batteries = await dbContext.Set<Battery>().AsNoTracking().Where(b => !b.IsDeleted).ToListAsync(cancellationToken);
        var lastLogs = await batteryLogReader.SliceOfLast(sliceAt, q => q, cancellationToken);
        var lastLogByBattery = lastLogs.GroupBy(l => l.Dimensions.BatteryId).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Period).First());

        var movements = await batteryCyclesReader.Movements(DateTime.MinValue, sliceAt, q => q, cancellationToken);
        var cyclesByBattery = movements
            .GroupBy(m => m.Dimensions.BatteryId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.IsExpense ? -x.Resources.Cycles : x.Resources.Cycles));

        var result = new List<(BatteriesReportLine, DateTime, int)>();
        foreach (var b in batteries)
        {
            lastLogByBattery.TryGetValue(b.Id, out var lastLog);
            DateTime? lastCharge = lastLog?.Period;
            var from = lastCharge.HasValue
                ? LocalCalendar.DateOf(lastCharge.Value, zone)
                : b.Created == default ? today : LocalCalendar.DateOf(b.Created, zone);
            var nextDue = from.AddDays(b.ChargeIntervalDays);

            result.Add((new BatteriesReportLine
            {
                BatteryId = b.Id,
                BatteryCode = b.Code ?? "",
                BatteryName = b.Name ?? "",
                UsedIn = b.UsedIn,
                BatteryKind = b.BatteryKind,
                ChargeIntervalDays = b.ChargeIntervalDays,
                LastChargeDate = lastCharge,
                NextDueDate = LocalCalendar.MidnightUtc(nextDue, zone),
                IsOverdue = nextDue < today,
                TotalCycles = cyclesByBattery.GetValueOrDefault(b.Id, 0m)
            }, nextDue, b.ChargeIntervalDays));
        }
        return result;
    }

    private async Task<IReadOnlyList<PlannedMealsRecord>> PlannedMealsAsync(DateTime first, DateTime last, CancellationToken cancellationToken)
    {
        var from = LocalCalendar.Key(first);
        var to = LocalCalendar.Key(last);
        return (await plannedMealsReader.Records(
            DateTime.MinValue,
            DateTime.MaxValue,
            q => q.Where(m => m.Dimensions.Date >= from && m.Dimensions.Date <= to),
            cancellationToken)).ToList();
    }
}
