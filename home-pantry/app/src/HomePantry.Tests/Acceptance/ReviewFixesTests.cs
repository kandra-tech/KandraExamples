using HomePantry.Application.Dashboards;
using HomePantry.Enums;
using HomePantry.Forms.Dashboards;
using HomePantry.Forms.Dictionaries;
using HomePantry.Forms.Documents;
using HomePantry.Forms.Handlers;
using HomePantry.Forms.Reports;
using HomePantry.Tests.Support;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Abstractions.Services.Blobs;
using Kandra.Forms.Querying;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace HomePantry.Tests.Acceptance;

/// <summary>
/// The rules the review of the whole application found broken. Most are about calendar days: every date the client
/// sends is a UTC instant, so a date picked in Kyiv is the previous UTC day, and these tests run there.
/// </summary>
[Collection("DataImport")]
public sealed class ReviewFixesTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly TimeZoneInfo Kyiv = HomePantryTestHost.EastOfUtc;

    private static DateTime Local(int year, int month, int day) => HomePantryTestHost.LocalMidnight(Kyiv, year, month, day);

    private static DateTime LocalDate(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, Kyiv).Date;

    private static DateTime LocalToday => LocalDate(DateTime.UtcNow);

    private static async Task<(Guid Store, Guid Location, Guid Product)> SeedStockAsync(HomePantryTestHost host, int shelfLifeDays = 0)
    {
        var store = await host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<StoreDto, StoreQueryDto>>()
            .InsertAsync(new StoreDto { Code = "S1", Name = "Market" }, Ct));
        var location = await host.InScopeAsync(sp => sp.GetRequiredService<IHierarchicalDictionaryCrudService<LocationNodeDto, LocationQueryDto>>()
            .InsertAsync(new LocationDto { Code = "L1", Name = "Fridge" }, Ct));
        var product = await host.InScopeAsync(sp => sp.GetRequiredService<IHierarchicalDictionaryCrudService<ProductNodeDto, ProductQueryDto>>()
            .InsertAsync(new ProductDto
            {
                Code = "P1", Name = "Milk", Unit = ProductUnit.L, DefaultLocationId = location.Id, DefaultShelfLifeDays = shelfLifeDays,
            }, Ct));
        return (store.Id, location.Id, product.Id);
    }

    private static async Task PostPurchaseAsync(HomePantryTestHost host, (Guid Store, Guid Location, Guid Product) seed, DateTime date, DateTime bestBefore, decimal amount = 40m)
    {
        var crud = (IServiceProvider sp) => sp.GetRequiredService<IDocumentCrudService<PurchaseDto, PurchaseQueryDto>>();
        var purchase = await host.InScopeAsync(sp => crud(sp).InsertAsync(new PurchaseDto
        {
            Code = $"PUR-{Guid.NewGuid():N}"[..12],
            Date = date,
            StoreId = seed.Store,
            Lines = [new PurchaseLineDto { Id = Guid.NewGuid(), ProductId = seed.Product, Quantity = 2m, Price = amount / 2, LocationId = seed.Location, BestBeforeDate = bestBefore }],
        }, Ct));
        await host.InScopeAsync(sp => crud(sp).SubmitAsync(purchase.Id, Ct));
    }

    [Fact]
    public async Task MealPlan_WeekPickedInKyiv_PlansThatWeek_NotThePreviousOne()
    {
        using var host = new HomePantryTestHost(timeZone: Kyiv);
        var (_, _, product) = await SeedStockAsync(host);
        var recipe = await host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<RecipeDto, RecipeQueryDto>>()
            .InsertAsync(new RecipeDto { Code = "R1", Name = "Porridge", Servings = 2, Lines = [new() { ProductId = product, Quantity = 0.5m }] }, Ct));

        var monday = Local(2026, 11, 9);
        var crud = (IServiceProvider sp) => sp.GetRequiredService<IDocumentCrudService<MealPlanDto, MealPlanQueryDto>>();
        var plan = await host.InScopeAsync(sp => crud(sp).InsertAsync(new MealPlanDto
        {
            Code = "MP1", Date = monday, WeekStartDate = monday,
            Lines = [new() { Day = MealPlanDay.Wednesday, RecipeId = recipe.Id, Servings = 2m }],
        }, Ct));
        await host.InScopeAsync(sp => crud(sp).SubmitAsync(plan.Id, Ct));

        var saved = await host.InScopeAsync(sp => crud(sp).GetByIdAAsync(plan.Id, Ct));
        Assert.Equal(new DateTime(2026, 11, 9), LocalDate(saved!.WeekStartDate));

        var weekPlan = await host.InScopeAsync(sp => sp.GetRequiredService<IReportService<ReportWeekPlanDto, WeekPlanReportResult>>()
            .RunAsync(new ReportWeekPlanDto { Week = Local(2026, 11, 11) }, Ct));
        var meal = Assert.Single(weekPlan.Data.Lines);
        Assert.Equal(new DateTime(2026, 11, 11), LocalDate(meal.Date));

        var needs = await host.InScopeAsync(sp => sp.GetRequiredService<IReportService<ReportShoppingNeedsDto, ShoppingNeedsReportResult>>()
            .RunAsync(new ReportShoppingNeedsDto { MealPlanWeek = monday }, Ct));
        Assert.Equal(0.5m, Assert.Single(needs.Data.Lines).TotalNeededQuantity);
    }

    [Fact]
    public async Task BestBefore_PickedInKyiv_CountsCalendarDays_AndTheDefaultIsThatDay()
    {
        using var host = new HomePantryTestHost(timeZone: Kyiv);
        var seed = await SeedStockAsync(host, shelfLifeDays: 3);
        var today = LocalToday;

        // The default the form fills for a purchase dated today (a date picked in Kyiv: local midnight).
        var lookup = await host.InScopeAsync(sp => sp.GetRequiredKeyedService<IHandlerBehavior<ProductDefaultLookupRequestDto, ProductDefaultLookupResponseDto>>("ProductDefaultLookup")
            .HandleAsync(new ProductDefaultLookupRequestDto { ProductId = seed.Product, DocumentDate = Local(today.Year, today.Month, today.Day) }, Ct));
        Assert.Equal(today.AddDays(3), LocalDate(lookup.BestBeforeDate));

        await PostPurchaseAsync(host, seed, DateTime.UtcNow, lookup.BestBeforeDate);

        var expiring = await host.InScopeAsync(sp => sp.GetRequiredService<IReportService<ReportExpiringDto, ExpiringReportResult>>()
            .RunAsync(new ReportExpiringDto(), Ct));
        var batch = Assert.Single(expiring.Data.Lines);
        Assert.Equal(3, batch.DaysLeft);
        Assert.Equal(today.AddDays(3), LocalDate(batch.BestBeforeDate));
    }

    [Fact]
    public async Task Spending_ForAPeriodPickedInKyiv_IncludesItsLastDay()
    {
        using var host = new HomePantryTestHost(timeZone: Kyiv);
        var seed = await SeedStockAsync(host);
        await PostPurchaseAsync(host, seed, Local(2026, 10, 5).AddHours(12), Local(2027, 1, 1), amount: 40m);

        var spending = await host.InScopeAsync(sp => sp.GetRequiredService<IReportService<ReportSpendingDto, SpendingReportResult>>()
            .RunAsync(new ReportSpendingDto { DateRange = new DateRangeDto { From = Local(2026, 10, 5), To = Local(2026, 10, 5) } }, Ct));
        Assert.Equal(40m, spending.Data.Lines.Sum(l => l.PurchasedAmount));
    }

    [Fact]
    public async Task Dashboard_ThisMonthsSpending_IsTheLocalMonth_AsTheSpendingReport()
    {
        using var host = new HomePantryTestHost(timeZone: Kyiv);
        var seed = await SeedStockAsync(host);
        var today = LocalToday;
        var firstOfMonth = Local(today.Year, today.Month, 1);
        // Bought just after local midnight of the 1st: still the previous month in UTC.
        await PostPurchaseAsync(host, seed, firstOfMonth.AddMinutes(30), Local(2099, 1, 1), amount: 40m);

        var dashboard = await host.InScopeAsync(sp => ActivatorUtilities.CreateInstance<PantryDashboardBehavior>(sp)
            .GetDataAsync(new PantryDashboardDto(), null!, Ct));
        Assert.Equal(40m, dashboard.MonthlySpending.Value);
        Assert.Equal(0m, dashboard.MonthlySpending.Previous ?? 0m);
        Assert.Equal(40m, dashboard.SpendingHistory.Series.Single().Values.Last());

        var spending = await host.InScopeAsync(sp => sp.GetRequiredService<IReportService<ReportSpendingDto, SpendingReportResult>>()
            .RunAsync(new ReportSpendingDto { DateRange = new DateRangeDto { From = firstOfMonth, To = Local(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)) } }, Ct));
        Assert.Equal(dashboard.MonthlySpending.Value, spending.Data.Lines.Sum(l => l.PurchasedAmount));
    }

    [Fact]
    public async Task ChoreDone_JustAfterLocalMidnight_DueAWeekFromThatLocalDay()
    {
        using var host = new HomePantryTestHost(timeZone: Kyiv);
        var member = await host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<HouseholdMemberDto, HouseholdMemberQueryDto>>()
            .InsertAsync(new HouseholdMemberDto { Code = "HM1", Name = "Olena" }, Ct));
        var chore = await host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<ChoreDto, ChoreQueryDto>>()
            .InsertAsync(new ChoreDto { Code = "CH1", Name = "Water plants", Recurrence = ChoreRecurrence.Weekly, AssignedToId = member.Id }, Ct));

        var crud = (IServiceProvider sp) => sp.GetRequiredService<IDocumentCrudService<ChoreDoneDto, ChoreDoneQueryDto>>();
        var done = await host.InScopeAsync(sp => crud(sp).InsertAsync(new ChoreDoneDto
        {
            Code = "CD1", Date = Local(2026, 11, 2).AddMinutes(30), ChoreId = chore.Id, DoneById = member.Id,
        }, Ct));
        await host.InScopeAsync(sp => crud(sp).SubmitAsync(done.Id, Ct));

        var report = await host.InScopeAsync(sp => sp.GetRequiredService<IReportService<ReportChoresDto, ChoresReportResult>>()
            .RunAsync(new ReportChoresDto(), Ct));
        var row = Assert.Single(report.Data.Lines);
        Assert.Equal(new DateTime(2026, 11, 9), LocalDate(row.NextDueDate));
    }

    [Fact]
    public async Task Calendar_DailyChore_IsDueOnEveryDayOfThePeriod()
    {
        using var host = new HomePantryTestHost();
        var member = await host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<HouseholdMemberDto, HouseholdMemberQueryDto>>()
            .InsertAsync(new HouseholdMemberDto { Code = "HM1", Name = "Olena" }, Ct));
        var chore = await host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<ChoreDto, ChoreQueryDto>>()
            .InsertAsync(new ChoreDto { Code = "CH1", Name = "Feed the cat", Recurrence = ChoreRecurrence.Daily, AssignedToId = member.Id }, Ct));
        var today = DateTime.UtcNow.Date;
        var crud = (IServiceProvider sp) => sp.GetRequiredService<IDocumentCrudService<ChoreDoneDto, ChoreDoneQueryDto>>();
        var done = await host.InScopeAsync(sp => crud(sp).InsertAsync(new ChoreDoneDto
        {
            Code = "CD1", Date = today.AddHours(8), ChoreId = chore.Id, DoneById = member.Id,
        }, Ct));
        await host.InScopeAsync(sp => crud(sp).SubmitAsync(done.Id, Ct));

        var calendar = await host.InScopeAsync(sp => sp.GetRequiredService<IReportService<ReportCalendarDto, CalendarReportResult>>()
            .RunAsync(new ReportCalendarDto { From = today, To = today.AddDays(3) }, Ct));
        Assert.Equal(4, calendar.Data.Lines.Count);
        Assert.Equal(["", "Feed the cat", "Feed the cat", "Feed the cat"], calendar.Data.Lines.OrderBy(l => l.Date).Select(l => l.ChoresDue));
    }

    [Fact]
    public async Task Equipment_WithAManualThatIsNotStored_GetsNoMadeUpFile()
    {
        using var host = new HomePantryTestHost();
        var missing = Guid.NewGuid();
        var (_, location, _) = await SeedStockAsync(host);
        var crud = (IServiceProvider sp) => sp.GetRequiredService<IDictionaryCrudService<EquipmentDto, EquipmentQueryDto>>();
        var created = await host.InScopeAsync(sp => crud(sp).InsertAsync(new EquipmentDto
        {
            Code = "EQ1", Name = "Kettle", LocationId = location, ManualFileId = missing,
        }, Ct));
        var saved = await host.InScopeAsync(sp => crud(sp).GetByIdAAsync(created.Id, Ct));

        var stored = await host.InScopeAsync(sp => sp.GetRequiredService<IBlobService>().GetMetadataAsync(saved.ManualFileId ?? missing, Ct));
        Assert.Null(stored);
    }

    [Fact]
    public async Task Transfer_ToTheSameLocation_IsRefused()
    {
        using var host = new HomePantryTestHost();
        var localizer = host.Services.GetRequiredService<IStringLocalizer>();
        var location = Guid.NewGuid();
        var result = new TransferValidator(localizer).Validate(new TransferDto
        {
            Code = "TRF1", Date = DateTime.UtcNow,
            Lines = [new TransferLineDto { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), FromLocationId = location, ToLocationId = location, Quantity = 1m }],
        });
        Assert.False(result.IsValid);
    }
}
