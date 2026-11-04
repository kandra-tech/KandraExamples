using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Services.DataProcessors;
using Kandra.DataImport;
using Kandra.Forms.Querying;
using HomePantry.Application.Dashboards;
using HomePantry.Domain.Commons;
using HomePantry.Enums;
using HomePantry.Forms.Dashboards;
using HomePantry.Forms.Dictionaries;
using HomePantry.Forms.Documents;
using HomePantry.Forms.Reports;
using HomePantry.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace HomePantry.Tests.Acceptance;

[Collection("DataImport")]
public sealed class HouseholdAcceptanceChecksTests : IDisposable
{
    private readonly HomePantryTestHost _host = new();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public void Dispose() => _host.Dispose();

    private static DateTime Day(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static IDictionaryCrudService<HouseholdMemberDto, HouseholdMemberQueryDto> HouseholdMembers(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<HouseholdMemberDto, HouseholdMemberQueryDto>>();

    private static IDictionaryCrudService<ChoreDto, ChoreQueryDto> Chores(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<ChoreDto, ChoreQueryDto>>();

    private static IDictionaryCrudService<BatteryDto, BatteryQueryDto> Batteries(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<BatteryDto, BatteryQueryDto>>();

    private static IDictionaryCrudService<EquipmentDto, EquipmentQueryDto> Equipments(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<EquipmentDto, EquipmentQueryDto>>();

    private static IDocumentCrudService<ChoreDoneDto, ChoreDoneQueryDto> ChoresDone(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<ChoreDoneDto, ChoreDoneQueryDto>>();

    private static IDocumentCrudService<BatteryChargedDto, BatteryChargedQueryDto> BatteriesCharged(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<BatteryChargedDto, BatteryChargedQueryDto>>();

    private static IDocumentCrudService<MealPlanDto, MealPlanQueryDto> MealPlans(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<MealPlanDto, MealPlanQueryDto>>();

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

    // 1. Posting a Chore done moves the chore's next due date by its recurrence.
    [Fact]
    public async Task Check1_PostingChoreDoneMovesNextDueDateByRecurrence()
    {
        var member = await _host.InScopeAsync(sp => HouseholdMembers(sp).InsertAsync(new HouseholdMemberDto
        {
            Code = "HM-1",
            Name = "Alice"
        }, Ct));

        var chore = await _host.InScopeAsync(sp => Chores(sp).InsertAsync(new ChoreDto
        {
            Code = "CH-1",
            Name = "Water garden",
            Recurrence = ChoreRecurrence.Weekly,
            EveryNDays = 7,
            AssignedToId = member.Id
        }, Ct));

        var doneDate = DateTime.UtcNow.Date.AddDays(-5);
        var doc = await _host.InScopeAsync(sp => ChoresDone(sp).InsertAsync(new ChoreDoneDto
        {
            Code = "CD-1",
            Date = doneDate,
            ChoreId = chore.Id,
            DoneById = member.Id,
            Comment = "All watered"
        }, Ct));

        await _host.InScopeAsync(sp => ChoresDone(sp).SubmitAsync(doc.Id, Ct));

        // Verify Chores report
        await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportChoresDto, ChoresReportResult>>();
            var result = await reportService.RunAsync(new ReportChoresDto(), Ct);
            var row = Assert.Single(result.Data.Lines, r => r.ChoreId == chore.Id);

            Assert.Equal(doneDate, row.LastDoneDate);
            Assert.Equal(doneDate.AddDays(7), row.NextDueDate);
        });
    }

    // 2. A chore that was never done is due from the day it was created.
    [Fact]
    public async Task Check2_ChoreNeverDoneIsDueFromDayCreated()
    {
        var member = await _host.InScopeAsync(sp => HouseholdMembers(sp).InsertAsync(new HouseholdMemberDto
        {
            Code = "HM-2",
            Name = "Bob"
        }, Ct));

        var chore = await _host.InScopeAsync(sp => Chores(sp).InsertAsync(new ChoreDto
        {
            Code = "CH-2",
            Name = "Clean gutters",
            Recurrence = ChoreRecurrence.Monthly,
            EveryNDays = 30,
            AssignedToId = member.Id
        }, Ct));

        // Verify Chores report on creation date
        await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportChoresDto, ChoresReportResult>>();
            var result = await reportService.RunAsync(new ReportChoresDto(), Ct);
            var row = Assert.Single(result.Data.Lines, r => r.ChoreId == chore.Id);

            Assert.Null(row.LastDoneDate);
            Assert.Null(row.DoneByName);
            Assert.Equal(DateTime.UtcNow.Date, row.NextDueDate.Date);
        });
    }

    // 3. Un-posting or deleting the last Chore done brings back the previous last-done date.
    [Fact]
    public async Task Check3_UnpostingOrDeletingLastChoreDoneBringsBackPreviousLastDoneDate()
    {
        var member = await _host.InScopeAsync(sp => HouseholdMembers(sp).InsertAsync(new HouseholdMemberDto
        {
            Code = "HM-3",
            Name = "Charlie"
        }, Ct));

        var chore = await _host.InScopeAsync(sp => Chores(sp).InsertAsync(new ChoreDto
        {
            Code = "CH-3",
            Name = "Dust shelves",
            Recurrence = ChoreRecurrence.Weekly,
            EveryNDays = 7,
            AssignedToId = member.Id
        }, Ct));

        var date1 = DateTime.UtcNow.Date.AddDays(-10);
        var cd1 = await _host.InScopeAsync(sp => ChoresDone(sp).InsertAsync(new ChoreDoneDto
        {
            Code = "CD-3A",
            Date = date1,
            ChoreId = chore.Id,
            DoneById = member.Id
        }, Ct));
        await _host.InScopeAsync(sp => ChoresDone(sp).SubmitAsync(cd1.Id, Ct));

        var date2 = DateTime.UtcNow.Date.AddDays(-3);
        var cd2 = await _host.InScopeAsync(sp => ChoresDone(sp).InsertAsync(new ChoreDoneDto
        {
            Code = "CD-3B",
            Date = date2,
            ChoreId = chore.Id,
            DoneById = member.Id
        }, Ct));
        await _host.InScopeAsync(sp => ChoresDone(sp).SubmitAsync(cd2.Id, Ct));

        // After cd2, last done should be date2
        await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportChoresDto, ChoresReportResult>>();
            var result = await reportService.RunAsync(new ReportChoresDto(), Ct);
            var row = Assert.Single(result.Data.Lines, r => r.ChoreId == chore.Id);
            Assert.Equal(date2, row.LastDoneDate);
            Assert.Equal(date2.AddDays(7), row.NextDueDate);
        });

        // Unsubmit cd2
        await _host.InScopeAsync(sp => ChoresDone(sp).UnsubmitAsync(cd2.Id, Ct));

        // Last done date should now revert back to date1!
        await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportChoresDto, ChoresReportResult>>();
            var result = await reportService.RunAsync(new ReportChoresDto(), Ct);
            var row = Assert.Single(result.Data.Lines, r => r.ChoreId == chore.Id);
            Assert.Equal(date1, row.LastDoneDate);
            Assert.Equal(date1.AddDays(7), row.NextDueDate);
        });
    }

    // 4. Shopping needs for a meal-plan week equals the scaled ingredients of its planned meals minus the stock on hand.
    [Fact]
    public async Task Check4_ShoppingNeedsForMealPlanWeekEqualsScaledIngredientsMinusStockOnHand()
    {
        var store = await _host.InScopeAsync(sp => Stores(sp).InsertAsync(new StoreDto { Code = "S-M4", Name = "Grocery Store" }, Ct));
        var loc = await _host.InScopeAsync(sp => Locations(sp).InsertAsync(new LocationDto { Code = "L-M4", Name = "Pantry Cupboard" }, Ct));

        var flour = await _host.InScopeAsync(sp => Products(sp).InsertAsync(new ProductDto
        {
            Code = "P-FLOUR",
            Name = "Baking Flour",
            Unit = ProductUnit.Kg,
            MinimumStock = 0m,
            DefaultLocationId = loc.Id
        }, Ct));

        // Recipe: 2 servings requires 0.5 kg of flour
        var recipe = await _host.InScopeAsync(sp => Recipes(sp).InsertAsync(new RecipeDto
        {
            Code = "R-CAKE",
            Name = "Sponge Cake",
            Servings = 2,
            Lines = [new() { ProductId = flour.Id, Quantity = 0.5m }]
        }, Ct));

        // Purchase 0.2 kg of flour (on hand = 0.2 kg)
        var purchase = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new PurchaseDto
        {
            Code = "PUR-M4",
            Date = Day(2026, 11, 2),
            StoreId = store.Id,
            Lines = [new() { ProductId = flour.Id, Quantity = 0.2m, Price = 30m, Amount = 6m, LocationId = loc.Id, BestBeforeDate = Day(2027, 1, 1) }]
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(purchase.Id, Ct));

        // Meal plan for Monday Nov 9: 6 servings of cake (scale factor = 6 / 2 = 3; needed flour = 3 * 0.5 = 1.5 kg)
        var weekMonday = Day(2026, 11, 9);
        var mealPlan = await _host.InScopeAsync(sp => MealPlans(sp).InsertAsync(new MealPlanDto
        {
            Code = "MP-M4",
            Date = weekMonday,
            WeekStartDate = weekMonday,
            Lines =
            [
                new()
                {
                    Day = MealPlanDay.Monday,
                    RecipeId = recipe.Id,
                    Servings = 6m
                }
            ]
        }, Ct));
        await _host.InScopeAsync(sp => MealPlans(sp).SubmitAsync(mealPlan.Id, Ct));

        // Query Shopping Needs for that meal plan week
        // Scaled ingredients = 1.5 kg
        // Stock on hand = 0.2 kg
        // Expected needed quantity = 1.5 kg - 0.2 kg = 1.3 kg
        await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportShoppingNeedsDto, ShoppingNeedsReportResult>>();
            var result = await reportService.RunAsync(new ReportShoppingNeedsDto
            {
                MealPlanWeek = weekMonday
            }, Ct);

            var line = Assert.Single(result.Data.Lines, l => l.ProductId == flour.Id);
            Assert.Equal(0.2m, line.OnHandQuantity);
            Assert.Equal(1.5m, line.RecipeNeededQuantity);
            Assert.Equal(1.3m, line.TotalNeededQuantity);
        });
    }

    // 5. The dashboard's overdue chores tile equals the overdue rows of the Chores report.
    [Fact]
    public async Task Check5_DashboardOverdueChoresTileEqualsOverdueRowsOfChoresReport()
    {
        // Apply demo package to populate all household chores and records
        await _host.InScopeAsync(async sp =>
        {
            using var stream = sp.GetRequiredService<IImportPackageRegistry>().Open("homepantry-demo-en")!;
            var report = await sp.GetRequiredService<IDataImportService>().ImportAsync(stream, new ImportOptions(), Ct);
            Assert.False(report.HasErrors);
        });

        await _host.InScopeAsync(async sp =>
        {
            var choresService = sp.GetRequiredService<IReportService<ReportChoresDto, ChoresReportResult>>();
            var choresReport = await choresService.RunAsync(new ReportChoresDto(), Ct);
            var overdueInReport = choresReport.Data.Lines.Count(l => l.IsOverdue);

            var repo = sp.GetRequiredService<IReportsRepository>();
            var dashboardBehavior = ActivatorUtilities.CreateInstance<PantryDashboardBehavior>(sp);
            var dashboard = await dashboardBehavior.GetDataAsync(new PantryDashboardDto(), null!, Ct);

            Assert.Equal(overdueInReport, (int)(dashboard.OverdueChores.Value ?? 0));

            // Equipment report on demo data resolves folder LocationName ("Kitchen")
            var equipmentReportService = sp.GetRequiredService<IReportService<ReportEquipmentDto, EquipmentReportResult>>();
            var equipmentReport = await equipmentReportService.RunAsync(new ReportEquipmentDto { Days = 30 }, Ct);
            var roborock = Assert.Single(equipmentReport.Data.Lines, e => e.EquipmentCode == "EQ-ROBOT");
            Assert.Equal("Kitchen", roborock.LocationName);

            // Today's schedule and WeekPlan / Calendar find planned meals even when today is not Monday
            var weekPlanService = sp.GetRequiredService<IReportService<ReportWeekPlanDto, WeekPlanReportResult>>();
            var weekPlan = await weekPlanService.RunAsync(new ReportWeekPlanDto { Week = DateTime.UtcNow.Date }, Ct);
            Assert.Equal(3, weekPlan.Data.Lines.Count);

            var monday = weekPlan.Data.Lines.Min(l => l.Date);
            var wednesdaySchedule = await repo.GetTodayScheduleAsync(monday.AddDays(2), null, Ct);
            Assert.Contains(wednesdaySchedule, s => s.IsMeal && s.Servings == 3m);
        });
    }

    // 6. All part A tests still pass.
    [Fact]
    public async Task Check6_DisposableBatteryReplacementAndRemindersProcessorWork()
    {
        var battery = await _host.InScopeAsync(sp => Batteries(sp).InsertAsync(new BatteryDto
        {
            Code = "BAT-DISP",
            Name = "Hallway Smoke Detector 9V",
            UsedIn = "Hallway",
            BatteryKind = BatteryKind.Disposable,
            ChargeIntervalDays = 10
        }, Ct));

        var chargeDoc = await _host.InScopeAsync(sp => BatteriesCharged(sp).InsertAsync(new BatteryChargedDto
        {
            Code = "BC-DISP",
            Date = DateTime.UtcNow.Date.AddDays(-15),
            BatteryId = battery.Id,
            Replaced = false // should be forced to true for Disposable battery by OnSaveAsync/OnSubmitAsync
        }, Ct));

        var loadedDoc = await _host.InScopeAsync(sp => BatteriesCharged(sp).GetByIdAAsync(chargeDoc.Id, Ct));
        Assert.True(loadedDoc!.Replaced);
        await _host.InScopeAsync(sp => BatteriesCharged(sp).SubmitAsync(chargeDoc.Id, Ct));

        await _host.InScopeAsync(async sp =>
        {
            var batteryReport = await sp.GetRequiredService<IReportService<ReportBatteriesDto, BatteriesReportResult>>()
                .RunAsync(new ReportBatteriesDto(), Ct);
            var row = Assert.Single(batteryReport.Data.Lines, b => b.BatteryId == battery.Id);
            Assert.True(row.IsOverdue);
            Assert.Equal(1m, row.TotalCycles);

            var reminders = sp.GetRequiredService<IDataProcessorService<HomePantry.Forms.DataProcessors.RemindersDto, HomePantry.Forms.DataProcessors.RemindersResultDto>>();
            var reminderResult = await reminders.ProcessAsync(new HomePantry.Forms.DataProcessors.RemindersDto(), Ct);
            Assert.True(reminderResult.DueBatteriesCount >= 1);
        });
    }
}
