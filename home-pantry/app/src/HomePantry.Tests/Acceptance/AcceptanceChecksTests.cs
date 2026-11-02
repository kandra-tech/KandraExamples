using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Services;
using Kandra.DataImport;
using Kandra.Domain.Exceptions;
using Kandra.Forms.Querying;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using HomePantry.Application.Dashboards;
using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.Registers;
using HomePantry.Enums;
using HomePantry.Forms.Dashboards;
using HomePantry.Forms.Dictionaries;
using HomePantry.Forms.Documents;
using HomePantry.Forms.Handlers;
using HomePantry.Forms.Reports;
using HomePantry.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace HomePantry.Tests.Acceptance;

[Collection("DataImport")]
public sealed class AcceptanceChecksTests : IDisposable
{
    private readonly HomePantryTestHost _host = new();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public void Dispose() => _host.Dispose();

    private static DateTime Day(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static IHierarchicalDictionaryCrudService<ProductNodeDto, ProductQueryDto> Products(IServiceProvider sp) =>
        sp.GetRequiredService<IHierarchicalDictionaryCrudService<ProductNodeDto, ProductQueryDto>>();

    private static IHierarchicalDictionaryCrudService<LocationNodeDto, LocationQueryDto> Locations(IServiceProvider sp) =>
        sp.GetRequiredService<IHierarchicalDictionaryCrudService<LocationNodeDto, LocationQueryDto>>();

    private static IDictionaryCrudService<StoreDto, StoreQueryDto> Stores(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<StoreDto, StoreQueryDto>>();

    private static IDictionaryCrudService<RecipeDto, RecipeQueryDto> Recipes(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<RecipeDto, RecipeQueryDto>>();

    private static IDocumentCrudService<PurchaseDto, PurchaseQueryDto> Purchases(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<PurchaseDto, PurchaseQueryDto>>();

    private static IDocumentCrudService<ConsumeDto, ConsumeQueryDto> Consumes(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<ConsumeDto, ConsumeQueryDto>>();

    private static IDocumentCrudService<CookRecipeDto, CookRecipeQueryDto> CookRecipes(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<CookRecipeDto, CookRecipeQueryDto>>();

    private static IDocumentCrudService<ShoppingListDto, ShoppingListQueryDto> ShoppingLists(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<ShoppingListDto, ShoppingListQueryDto>>();

    private static IDocumentCrudService<InventoryCountDto, InventoryCountQueryDto> InventoryCounts(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<InventoryCountDto, InventoryCountQueryDto>>();

    // 1. A Purchase adds a batch per line with its best-before date and records the last price.
    [Fact]
    public async Task Check1_PurchaseAddsBatchPerLineWithBestBeforeAndRecordsLastPrice()
    {
        var store = await _host.InScopeAsync(sp => Stores(sp).InsertAsync(new StoreDto { Code = "S1", Name = "Store 1" }, Ct));
        var loc = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationDto { Code = "L1", Name = "Kitchen" }, Ct));
        var prod = await _host.InScopeAsync(sp => Products(sp).InsertAsync(new ProductDto
        {
            Code = "P1",
            Name = "Milk",
            Unit = ProductUnit.L,
            DefaultLocationId = loc.Id,
            DefaultShelfLifeDays = 14
        }, Ct));

        var docDate = Day(2026, 11, 1);
        var bestBefore = Day(2026, 11, 15);

        var purchase = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-01",
            Date = docDate,
            StoreId = store.Id,
            Lines =
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = prod.Id,
                    Quantity = 5m,
                    Price = 35m,
                    Amount = 175m,
                    LocationId = loc.Id,
                    BestBeforeDate = bestBefore
                }
            ]
        }, Ct));

        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(purchase.Id, Ct));

        // Verify stock batch
        await _host.InScopeAsync(async sp =>
        {
            var stockReader = sp.GetRequiredService<IBalanceReader<StockMovement, StockBalance>>();
            var balances = await stockReader.Balances(q => q.Where(b => b.Dimensions.ProductId == prod.Id), Ct);

            var batch = Assert.Single(balances);
            Assert.Equal(loc.Id, batch.Dimensions.LocationId);
            Assert.Equal(bestBefore, batch.Dimensions.BestBeforeDate);
            Assert.Equal(5m, batch.Resources.Quantity);
            Assert.Equal(175m, batch.Resources.Value);
        });

        // Verify last price recorded
        await _host.InScopeAsync(async sp =>
        {
            var priceReader = sp.GetRequiredService<IInfoRegisterReader<LastPriceRecord>>();
            var slice = await priceReader.SliceOfLast(docDate, q => q.Where(r => r.Dimensions.ProductId == prod.Id && r.Dimensions.StoreId == store.Id), Ct);

            var record = Assert.Single(slice);
            Assert.Equal(35m, record.Details.Price);
        });
    }

    // 2. Consume takes the batch with the earliest best-before date first and splits across batches when needed.
    [Fact]
    public async Task Check2_ConsumeTakesEarliestBestBeforeFirstAndSplitsAcrossBatches()
    {
        var store = await _host.InScopeAsync(sp => Stores(sp).InsertAsync(new StoreDto { Code = "S2", Name = "Store 2" }, Ct));
        var loc = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationDto { Code = "L2", Name = "Pantry" }, Ct));
        var prod = await _host.InScopeAsync(sp => Products(sp).InsertAsync(new ProductDto
        {
            Code = "P2",
            Name = "Yogurt",
            Unit = ProductUnit.Pcs,
            DefaultLocationId = loc.Id
        }, Ct));

        var batch1Date = Day(2026, 11, 10);
        var batch2Date = Day(2026, 11, 20);

        // Batch 1: 5 pcs @ 10 = 50, BB: Nov 10
        var p1 = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-2A",
            Date = Day(2026, 11, 1),
            StoreId = store.Id,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, Quantity = 5m, Price = 10m, Amount = 50m, LocationId = loc.Id, BestBeforeDate = batch1Date }]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(p1.Id, Ct));

        // Batch 2: 10 pcs @ 20 = 200, BB: Nov 20
        var p2 = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-2B",
            Date = Day(2026, 11, 2),
            StoreId = store.Id,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, Quantity = 10m, Price = 20m, Amount = 200m, LocationId = loc.Id, BestBeforeDate = batch2Date }]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(p2.Id, Ct));

        // Consume 8 pcs (all 5 from batch 1, and 3 from batch 2)
        var c1 = await _host.InScopeAsync(sp => Consumes(sp).InsertAsync(new ConsumeDto
        {
            Code = "CON-2",
            Date = Day(2026, 11, 3),
            Reason = ConsumeReason.Eaten,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, LocationId = loc.Id, Quantity = 8m }]
        }, Ct));
        await _host.InScopeAsync(sp => Consumes(sp).SubmitAsync(c1.Id, Ct));

        // Check balances: Batch 1 has 0 remaining, Batch 2 has 7 remaining (7 * 20 = 140 value)
        await _host.InScopeAsync(async sp =>
        {
            var stockReader = sp.GetRequiredService<IBalanceReader<StockMovement, StockBalance>>();
            var balances = (await stockReader.Balances(q => q.Where(b => b.Dimensions.ProductId == prod.Id && b.Resources.Quantity > 0), Ct)).ToList();

            var remainingBatch = Assert.Single(balances);
            Assert.Equal(batch2Date, remainingBatch.Dimensions.BestBeforeDate);
            Assert.Equal(7m, remainingBatch.Resources.Quantity);
            Assert.Equal(140m, remainingBatch.Resources.Value);
        });
    }

    // 3. Consuming more than is on hand refuses the post and leaves stock unchanged.
    [Fact]
    public async Task Check3_ConsumingMoreThanOnHandRefusesPostAndLeavesStockUnchanged()
    {
        var store = await _host.InScopeAsync(sp => Stores(sp).InsertAsync(new StoreDto { Code = "S3", Name = "Store 3" }, Ct));
        var loc = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationDto { Code = "L3", Name = "Fridge" }, Ct));
        var prod = await _host.InScopeAsync(sp => Products(sp).InsertAsync(new ProductDto
        {
            Code = "P3",
            Name = "Cheese",
            Unit = ProductUnit.Kg,
            DefaultLocationId = loc.Id
        }, Ct));

        // Purchase 5 kg
        var p = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-3",
            Date = Day(2026, 11, 1),
            StoreId = store.Id,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, Quantity = 5m, Price = 100m, Amount = 500m, LocationId = loc.Id, BestBeforeDate = Day(2026, 11, 30) }]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(p.Id, Ct));

        // Attempt to consume 6 kg
        var c = await _host.InScopeAsync(sp => Consumes(sp).InsertAsync(new ConsumeDto
        {
            Code = "CON-3",
            Date = Day(2026, 11, 2),
            Reason = ConsumeReason.Eaten,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, LocationId = loc.Id, Quantity = 6m }]
        }, Ct));

        await _host.InScopeAsync(async sp =>
        {
            await Assert.ThrowsAsync<InvalidStateException>(() => Consumes(sp).SubmitAsync(c.Id, Ct));
        });

        // Verify stock is untouched (still 5 kg)
        await _host.InScopeAsync(async sp =>
        {
            var stockReader = sp.GetRequiredService<IBalanceReader<StockMovement, StockBalance>>();
            var balances = await stockReader.Balances(q => q.Where(b => b.Dimensions.ProductId == prod.Id), Ct);
            var balance = Assert.Single(balances);
            Assert.Equal(5m, balance.Resources.Quantity);
        });
    }

    // 4. Cook recipe consumes the recipe's ingredients scaled to the document's servings.
    [Fact]
    public async Task Check4_CookRecipeConsumesIngredientsScaledToServings()
    {
        var loc = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationDto { Code = "L4", Name = "Kitchen" }, Ct));
        var store = await _host.InScopeAsync(sp => Stores(sp).InsertAsync(new StoreDto { Code = "S4", Name = "Store 4" }, Ct));

        var eggs = await _host.InScopeAsync(sp => Products(sp).InsertAsync(new ProductDto { Code = "EGGS", Name = "Eggs", Unit = ProductUnit.Pcs }, Ct));
        var milk = await _host.InScopeAsync(sp => Products(sp).InsertAsync(new ProductDto { Code = "MILK", Name = "Milk", Unit = ProductUnit.L }, Ct));

        // Recipe for 2 servings: Eggs = 4 pcs, Milk = 0.2 l
        var recipe = await _host.InScopeAsync(sp => Recipes(sp).InsertAsync(new RecipeDto
        {
            Code = "OMELETTE",
            Name = "Omelette",
            Servings = 2,
            Lines =
            [
                new() { Id = Guid.NewGuid(), ProductId = eggs.Id, Quantity = 4m },
                new() { Id = Guid.NewGuid(), ProductId = milk.Id, Quantity = 0.2m }
            ]
        }, Ct));

        // Stock up: 20 eggs, 2 l milk
        var p = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-4",
            Date = Day(2026, 11, 1),
            StoreId = store.Id,
            Lines =
            [
                new() { Id = Guid.NewGuid(), ProductId = eggs.Id, Quantity = 20m, Price = 5m, Amount = 100m, LocationId = loc.Id, BestBeforeDate = Day(2026, 11, 20) },
                new() { Id = Guid.NewGuid(), ProductId = milk.Id, Quantity = 2m, Price = 30m, Amount = 60m, LocationId = loc.Id, BestBeforeDate = Day(2026, 11, 20) }
            ]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(p.Id, Ct));

        // Cook for 4 servings (scaling factor = 2): should consume 8 eggs, 0.4 l milk
        var cook = await _host.InScopeAsync(sp => CookRecipes(sp).InsertAsync(new CookRecipeDto
        {
            Code = "COOK-4",
            Date = Day(2026, 11, 2),
            RecipeId = recipe.Id,
            Servings = 4m,
            LocationId = loc.Id,
            Lines =
            [
                new() { Id = Guid.NewGuid(), ProductId = eggs.Id, Quantity = 8m },
                new() { Id = Guid.NewGuid(), ProductId = milk.Id, Quantity = 0.4m }
            ]
        }, Ct));
        await _host.InScopeAsync(sp => CookRecipes(sp).SubmitAsync(cook.Id, Ct));

        // Check stock: Eggs: 20 - 8 = 12 pcs, Milk: 2 - 0.4 = 1.6 l
        await _host.InScopeAsync(async sp =>
        {
            var stockReader = sp.GetRequiredService<IBalanceReader<StockMovement, StockBalance>>();
            var balances = (await stockReader.Balances(q => q.Where(b => b.Dimensions.LocationId == loc.Id && b.Resources.Quantity > 0), Ct)).ToList();

            var eggsBal = balances.First(b => b.Dimensions.ProductId == eggs.Id);
            var milkBal = balances.First(b => b.Dimensions.ProductId == milk.Id);

            Assert.Equal(12m, eggsBal.Resources.Quantity);
            Assert.Equal(1.6m, milkBal.Resources.Quantity);
        });
    }

    // 5. Changing a posted Purchase, un-posting it, or deleting it updates stock correctly; a Purchase whose stock was already consumed cannot be un-posted.
    [Fact]
    public async Task Check5_ChangingPostedPurchaseUnpostingOrDeletingUpdatesStock_AndCannotUnpostConsumed()
    {
        var store = await _host.InScopeAsync(sp => Stores(sp).InsertAsync(new StoreDto { Code = "S5", Name = "Store 5" }, Ct));
        var loc = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationDto { Code = "L5", Name = "Cellar" }, Ct));
        var prod = await _host.InScopeAsync(sp => Products(sp).InsertAsync(new ProductDto { Code = "P5", Name = "Apples", Unit = ProductUnit.Kg }, Ct));

        // 1. Post Purchase of 10 kg
        var p = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-5",
            Date = Day(2026, 11, 1),
            StoreId = store.Id,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, Quantity = 10m, Price = 25m, Amount = 250m, LocationId = loc.Id, BestBeforeDate = Day(2026, 11, 30) }]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(p.Id, Ct));

        // Verify stock is 10
        await _host.InScopeAsync(async sp =>
        {
            var stock = await sp.GetRequiredService<IBalanceReader<StockMovement, StockBalance>>().Balances(q => q.Where(b => b.Dimensions.ProductId == prod.Id), Ct);
            Assert.Equal(10m, stock.Sum(s => s.Resources.Quantity));
        });

        // 2. Change posted purchase to 15 kg
        var readPurchase = await _host.InScopeAsync(sp => Purchases(sp).GetByIdAAsync(p.Id, Ct));
        readPurchase.Lines.First().Quantity = 15m;
        readPurchase.Lines.First().Amount = 375m;
        readPurchase.Total = 375m;
        await _host.InScopeAsync(sp => Purchases(sp).UpdateAsync(readPurchase, Ct));

        // Verify stock is now 15
        await _host.InScopeAsync(async sp =>
        {
            var stock = await sp.GetRequiredService<IBalanceReader<StockMovement, StockBalance>>().Balances(q => q.Where(b => b.Dimensions.ProductId == prod.Id), Ct);
            Assert.Equal(15m, stock.Sum(s => s.Resources.Quantity));
        });

        // 3. Unpost purchase -> stock becomes 0
        await _host.InScopeAsync(sp => Purchases(sp).UnsubmitAsync(p.Id, Ct));
        await _host.InScopeAsync(async sp =>
        {
            var stock = await sp.GetRequiredService<IBalanceReader<StockMovement, StockBalance>>().Balances(q => q.Where(b => b.Dimensions.ProductId == prod.Id), Ct);
            Assert.Equal(0m, stock.Sum(s => s.Resources.Quantity));
        });

        // 4. Re-submit with 10 kg
        readPurchase = await _host.InScopeAsync(sp => Purchases(sp).GetByIdAAsync(p.Id, Ct));
        readPurchase.Lines.First().Quantity = 10m;
        readPurchase.Lines.First().Amount = 250m;
        readPurchase.Total = 250m;
        await _host.InScopeAsync(sp => Purchases(sp).UpdateAsync(readPurchase, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(p.Id, Ct));

        // 5. Consume 4 kg
        var c = await _host.InScopeAsync(sp => Consumes(sp).InsertAsync(new ConsumeDto
        {
            Code = "CON-5",
            Date = Day(2026, 11, 2),
            Reason = ConsumeReason.Eaten,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, LocationId = loc.Id, Quantity = 4m }]
        }, Ct));
        await _host.InScopeAsync(sp => Consumes(sp).SubmitAsync(c.Id, Ct));

        // Stock is now 6 kg
        await _host.InScopeAsync(async sp =>
        {
            var stock = await sp.GetRequiredService<IBalanceReader<StockMovement, StockBalance>>().Balances(q => q.Where(b => b.Dimensions.ProductId == prod.Id), Ct);
            Assert.Equal(6m, stock.Sum(s => s.Resources.Quantity));
        });

        // 6. Attempt to unpost purchase whose stock was already consumed -> Refused!
        await _host.InScopeAsync(async sp =>
        {
            await Assert.ThrowsAsync<InvalidStateException>(() => Purchases(sp).UnsubmitAsync(p.Id, Ct));
        });

        // Stock remains 6 kg
        await _host.InScopeAsync(async sp =>
        {
            var stock = await sp.GetRequiredService<IBalanceReader<StockMovement, StockBalance>>().Balances(q => q.Where(b => b.Dimensions.ProductId == prod.Id), Ct);
            Assert.Equal(6m, stock.Sum(s => s.Resources.Quantity));
        });
    }

    // 6. Shopping needs for a product equals its minimum stock minus the quantity on hand, when that is positive.
    [Fact]
    public async Task Check6_ShoppingNeedsEqualsMinimumMinusOnHandWhenPositive()
    {
        var loc = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationDto { Code = "L6", Name = "Pantry" }, Ct));
        var store = await _host.InScopeAsync(sp => Stores(sp).InsertAsync(new StoreDto { Code = "S6", Name = "Store 6" }, Ct));
        var prod = await _host.InScopeAsync(sp => Products(sp).InsertAsync(new ProductDto
        {
            Code = "P6",
            Name = "Coffee",
            Unit = ProductUnit.Kg,
            MinimumStock = 10m,
            DefaultLocationId = loc.Id
        }, Ct));

        // Initially: 0 on hand. Shortfall should be 10 - 0 = 10.
        await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportShoppingNeedsDto, ShoppingNeedsReportResult>>();
            var result = await reportService.RunAsync(new ReportShoppingNeedsDto(), Ct);

            var line = Assert.Single(result.Data.Lines, l => l.ProductId == prod.Id);
            Assert.Equal(10m, line.ShortfallQuantity);
        });

        // Purchase 4 kg. On hand is now 4. Shortfall should be 10 - 4 = 6.
        var p = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-6A",
            Date = Day(2026, 11, 1),
            StoreId = store.Id,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, Quantity = 4m, Price = 400m, Amount = 1600m, LocationId = loc.Id, BestBeforeDate = Day(2027, 11, 1) }]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(p.Id, Ct));

        await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportShoppingNeedsDto, ShoppingNeedsReportResult>>();
            var result = await reportService.RunAsync(new ReportShoppingNeedsDto(), Ct);

            var line = Assert.Single(result.Data.Lines, l => l.ProductId == prod.Id);
            Assert.Equal(6m, line.ShortfallQuantity);
        });

        // Purchase 8 kg. On hand is now 12 (exceeds min 10). Shortfall should be 0 (no shortfall).
        var p2 = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-6B",
            Date = Day(2026, 11, 2),
            StoreId = store.Id,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, Quantity = 8m, Price = 400m, Amount = 3200m, LocationId = loc.Id, BestBeforeDate = Day(2027, 11, 1) }]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(p2.Id, Ct));

        await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportShoppingNeedsDto, ShoppingNeedsReportResult>>();
            var result = await reportService.RunAsync(new ReportShoppingNeedsDto(), Ct);

            var line = result.Data.Lines.FirstOrDefault(l => l.ProductId == prod.Id);
            Assert.True(line == null || line.ShortfallQuantity == 0m);
        });
    }

    // 7. The dashboard's figures equal the reports: the below-minimum count as the flagged rows of Stock overview, the
    //    expiring count as the rows of Expiring, this month's spending as Spending for the month.
    [Fact]
    public async Task Check7_DashboardFiguresEqualReports()
    {
        // 1. Seed the host with demo data
        await _host.InScopeAsync(async sp =>
        {
            using var stream = sp.GetRequiredService<IImportPackageRegistry>().Open("homepantry-demo")!;
            var report = await sp.GetRequiredService<IDataImportService>().ImportAsync(stream, new ImportOptions(), Ct);
            Assert.False(report.HasErrors);
        });

        // 2. Run reports and dashboard in the same scope to compare
        await _host.InScopeAsync(async sp =>
        {
            var stockOverviewService = sp.GetRequiredService<IReportService<ReportStockOverviewDto, StockOverviewReportResult>>();
            var expiringService = sp.GetRequiredService<IReportService<ReportExpiringDto, ExpiringReportResult>>();
            var spendingService = sp.GetRequiredService<IReportService<ReportSpendingDto, SpendingReportResult>>();

            var stockOverview = await stockOverviewService.RunAsync(new ReportStockOverviewDto(), Ct);
            var expiring = await expiringService.RunAsync(new ReportExpiringDto(), Ct);

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

            var spending = await spendingService.RunAsync(new ReportSpendingDto
            {
                DateRange = new DateRangeDto { From = startOfMonth, To = endOfMonth }
            }, Ct);

            // Dashboard
            var dashboardBehavior = ActivatorUtilities.CreateInstance<PantryDashboardBehavior>(sp);
            var dashboard = await dashboardBehavior.GetDataAsync(new PantryDashboardDto(), null!, Ct);

            // Compare Below-minimum
            var stockOverviewBelowMinCount = stockOverview.Data.Lines.Count(l => l.BelowMinimum);
            Assert.Equal(stockOverviewBelowMinCount, dashboard.BelowMinimum.Value);

            // Compare Expiring count
            var expiringRowsCount = expiring.Data.Lines.Count;
            Assert.Equal(expiringRowsCount, dashboard.ExpiringBatches.Value);

            // Compare Monthly Spending
            var spendingTotal = spending.Data.Lines.Sum(l => l.PurchasedAmount);
            Assert.Equal(spendingTotal, dashboard.MonthlySpending.Value);
        });
    }

    // 8. StockOverview filters by a Location folder (including descendant locations) and localizes Unit captions.
    [Fact]
    public async Task Check8_StockOverviewFiltersByLocationFolderAndLocalizesUnit()
    {
        var store = await _host.InScopeAsync(sp => Stores(sp).InsertAsync(new StoreDto { Code = "S8", Name = "Store 8" }, Ct));
        var kitchenFolder = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationFolderDto { Code = "KIT", Name = "Kitchen" }, Ct));
        var fridge = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationDto { Code = "FRG", Name = "Fridge", ParentId = kitchenFolder.Id, IsFreezer = false }, Ct));
        var cellar = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationDto { Code = "CEL", Name = "Cellar" }, Ct));

        var butter = await _host.InScopeAsync(sp => Products(sp).InsertAsync(new ProductDto
        {
            Code = "BUTTER",
            Name = "Butter",
            Unit = ProductUnit.Pack,
            DefaultLocationId = fridge.Id
        }, Ct));

        var p = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-8",
            Date = Day(2026, 11, 1),
            StoreId = store.Id,
            Lines =
            [
                new() { Id = Guid.NewGuid(), ProductId = butter.Id, Quantity = 3m, Price = 50m, Amount = 150m, LocationId = fridge.Id, BestBeforeDate = Day(2026, 12, 1) },
                new() { Id = Guid.NewGuid(), ProductId = butter.Id, Quantity = 2m, Price = 50m, Amount = 100m, LocationId = cellar.Id, BestBeforeDate = Day(2026, 12, 1) }
            ]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(p.Id, Ct));

        await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportStockOverviewDto, StockOverviewReportResult>>();

            // Filter by the parent folder "Kitchen" -> should include Fridge (3 packs), excluding Cellar (2 packs)
            var result = await reportService.RunAsync(new ReportStockOverviewDto { LocationId = kitchenFolder.Id }, Ct);
            var line = Assert.Single(result.Data.Lines, l => l.ProductId == butter.Id);
            Assert.Equal(3m, line.Quantity);
            Assert.Equal(150m, line.Value);
            Assert.Equal("pack", line.Unit);
        });
    }

    // 9. Creating a Purchase from a ShoppingList populates defaults, last price, and links the documents; CreateShoppingListFromNeeds persists the ShoppingList.
    [Fact]
    public async Task Check9_ShoppingListToPurchasePrepopulatesDefaultsAndCreateShoppingListFromNeedsPersists()
    {
        var store = await _host.InScopeAsync(sp => Stores(sp).InsertAsync(new StoreDto { Code = "S9", Name = "Store 9" }, Ct));
        var loc = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationDto { Code = "L9", Name = "Pantry 9" }, Ct));
        var prod = await _host.InScopeAsync(sp => Products(sp).InsertAsync(new ProductDto
        {
            Code = "FLOUR",
            Name = "Flour",
            Unit = ProductUnit.Kg,
            MinimumStock = 5m,
            DefaultLocationId = loc.Id,
            DefaultShelfLifeDays = 30
        }, Ct));

        // Establish a last price of 25 via an initial purchase of 1 kg (leaving shortfall of 4 kg)
        var p1 = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-9A",
            Date = Day(2026, 11, 1),
            StoreId = store.Id,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, Quantity = 1m, Price = 25m, Amount = 25m, LocationId = loc.Id, BestBeforeDate = Day(2026, 12, 1) }]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(p1.Id, Ct));

        // 1. Call CreateShoppingListFromNeedsHandler and verify the created shopping list is persisted in the DB
        Guid createdListId = Guid.Empty;
        await _host.InScopeAsync(async sp =>
        {
            var handler = sp.GetRequiredKeyedService<Kandra.Application.Abstractions.Behaviors.IHandlerBehavior<CreateShoppingListFromNeedsRequestDto, CreateShoppingListFromNeedsResponseDto>>("CreateShoppingListFromNeeds");
            var res = await handler.HandleAsync(new CreateShoppingListFromNeedsRequestDto { StoreId = store.Id }, Ct);
            createdListId = res.ShoppingListId;
            Assert.NotEqual(Guid.Empty, createdListId);
        });

        var savedList = await _host.InScopeAsync(sp => ShoppingLists(sp).GetByIdAAsync(createdListId, Ct));
        Assert.Equal(store.Id, savedList.StoreId);
        var slLine = Assert.Single(savedList.Lines, l => l.ProductId == prod.Id);
        Assert.Equal(4m, slLine.Quantity);

        // 2. Create a new Purchase from the ShoppingList using NewAsync(sourceDocumentId: createdListId)
        var purchaseDate = Day(2026, 11, 5);
        var newPurchase = await _host.InScopeAsync(sp => Purchases(sp).NewAsync(purchaseDate, createdListId, Ct));
        Assert.Equal(store.Id, newPurchase.StoreId);
        Assert.Equal(createdListId, newPurchase.ShoppingListId);

        var pLine = Assert.Single(newPurchase.Lines, l => l.ProductId == prod.Id);
        Assert.Equal(4m, pLine.Quantity);
        Assert.Equal(loc.Id, pLine.LocationId);
        Assert.Equal(purchaseDate.AddDays(30), pLine.BestBeforeDate);
        Assert.Equal(25m, pLine.Price);
        Assert.Equal(100m, pLine.Amount);
        Assert.Equal(100m, newPurchase.Total);
    }

    // 10. Editing a posted Purchase to a quantity below what was already consumed is refused, and InventoryCount reads stock as of its document date.
    [Fact]
    public async Task Check10_EditingPostedPurchaseBelowConsumedIsRefused_AndInventoryCountUsesDocumentDate()
    {
        var store = await _host.InScopeAsync(sp => Stores(sp).InsertAsync(new StoreDto { Code = "S10", Name = "Store 10" }, Ct));
        var loc = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationDto { Code = "L10", Name = "Shelf 10" }, Ct));
        var prod = await _host.InScopeAsync(sp => Products(sp).InsertAsync(new ProductDto
        {
            Code = "RICE",
            Name = "Rice",
            Unit = ProductUnit.Kg,
            DefaultLocationId = loc.Id
        }, Ct));

        // Nov 1: Purchase 10 kg
        var p = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-10A",
            Date = Day(2026, 11, 1),
            StoreId = store.Id,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, Quantity = 10m, Price = 40m, Amount = 400m, LocationId = loc.Id, BestBeforeDate = Day(2027, 11, 1) }]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(p.Id, Ct));

        // Nov 2: Consume 6 kg (4 kg left on Nov 2)
        var c = await _host.InScopeAsync(sp => Consumes(sp).InsertAsync(new ConsumeDto
        {
            Code = "CON-10",
            Date = Day(2026, 11, 2),
            Reason = ConsumeReason.UsedUp,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, LocationId = loc.Id, Quantity = 6m }]
        }, Ct));
        await _host.InScopeAsync(sp => Consumes(sp).SubmitAsync(c.Id, Ct));

        // Attempt to edit the posted Purchase on Nov 1 down from 10 kg to 5 kg (which is less than the 6 kg consumed) -> Refused!
        var readPurchase = await _host.InScopeAsync(sp => Purchases(sp).GetByIdAAsync(p.Id, Ct));
        readPurchase.Lines.First().Quantity = 5m;
        readPurchase.Lines.First().Amount = 200m;
        readPurchase.Total = 200m;

        await _host.InScopeAsync(async sp =>
        {
            await Assert.ThrowsAsync<InvalidStateException>(() => Purchases(sp).UpdateAsync(readPurchase, Ct));
        });

        // Nov 10: Later purchase of 20 kg
        var pLater = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-10B",
            Date = Day(2026, 11, 10),
            StoreId = store.Id,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, Quantity = 20m, Price = 40m, Amount = 800m, LocationId = loc.Id, BestBeforeDate = Day(2027, 11, 10) }]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(pLater.Id, Ct));

        // Backdated Inventory Count on Nov 5 (when stock on hand was 4 kg, before the Nov 10 purchase of 20 kg) counting 3 kg (shortage of 1 kg, NOT 21 kg!)
        var inv = await _host.InScopeAsync(sp => InventoryCounts(sp).InsertAsync(new InventoryCountDto
        {
            Code = "INV-10",
            Date = Day(2026, 11, 5),
            LocationId = loc.Id,
            Lines = [new() { Id = Guid.NewGuid(), ProductId = prod.Id, CountedQuantity = 3m }]
        }, Ct));
        await _host.InScopeAsync(sp => InventoryCounts(sp).SubmitAsync(inv.Id, Ct));

        // Total current stock after Nov 10 should now be 3 kg (as of Nov 5) + 20 kg (Nov 10) = 23 kg
        await _host.InScopeAsync(async sp =>
        {
            var stock = await sp.GetRequiredService<IBalanceReader<StockMovement, StockBalance>>().Balances(q => q.Where(b => b.Dimensions.ProductId == prod.Id), Ct);
            Assert.Equal(23m, stock.Sum(s => s.Resources.Quantity));
        });
    }
}
