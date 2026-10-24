using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Services;
using Kandra.Domain.Exceptions;
using Kandra.Forms.Querying;
using Microsoft.Extensions.DependencyInjection;
using PersonalAccounting.Forms.Dictionaries;
using PersonalAccounting.Forms.Documents;
using PersonalAccounting.Forms.Reports;
using PersonalAccounting.Tests.Support;

namespace PersonalAccounting.Tests.Acceptance;

/// <summary>
/// Rules added after the agent's run (see RESULTS.md): a budget's month is the user's local calendar month, one budget
/// per month, and an income or expense takes a category of its own kind. Runs as a user in Kyiv (UTC+2/+3), where a local
/// month starts on the previous day in UTC.
/// </summary>
public sealed class MonthAndCategoryRulesTests : IDisposable
{
    private static readonly TimeZoneInfo Kyiv = FindKyiv();
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly PersonalAccountingTestHost _host =
        new(services => services.AddScoped<ICurrentTimeZone>(_ => new FixedTimeZone(Kyiv)));

    public void Dispose() => _host.Dispose();

    /// <summary>Local midnight of a Kyiv calendar date, as the UTC instant the client sends.</summary>
    private static DateTime KyivMidnight(int year, int month, int day) =>
        TimeZoneInfo.ConvertTimeToUtc(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified), Kyiv);

    [Fact]
    public async Task BudgetAndReport_UseTheLocalCalendarMonth()
    {
        var (account, groceries, _) = await SetupAsync();

        // 1 November in Kyiv is 31 October 22:00 UTC.
        var november = KyivMidnight(2026, 11, 1);
        var budget = await PostBudgetAsync("B-NOV", november, groceries, 1000m);
        Assert.NotEqual(Guid.Empty, budget);
        await PostExpenseAsync("E-NOV", november, account, groceries, 300m);

        // Any day of the month picks the month; both the plan and the expense belong to November, none to October.
        var inNovember = await RunBudgetVsActualAsync(KyivMidnight(2026, 11, 20));
        var row = Assert.Single(inNovember, l => l.Id == groceries);
        Assert.Equal(1000m, row.PlannedAmount);
        Assert.Equal(300m, row.ActualAmount);

        var inOctober = await RunBudgetVsActualAsync(KyivMidnight(2026, 10, 20));
        Assert.DoesNotContain(inOctober, l => l.Id == groceries);
    }

    private async Task<ICollection<BudgetVsActualReportLineDto>> RunBudgetVsActualAsync(DateTime month)
    {
        var report = await _host.InScopeAsync(sp => sp.GetRequiredService<IReportService<ReportBudgetVsActualDto, BudgetVsActualReportResult>>()
            .RunAsync(new ReportBudgetVsActualDto { Month = month }, Ct));
        return report.Data!.Lines;
    }

    [Fact]
    public async Task SecondBudgetForTheSameMonth_IsRefused_UntilTheFirstIsUnposted()
    {
        var (_, groceries, _) = await SetupAsync();

        var first = await PostBudgetAsync("B-1", KyivMidnight(2026, 11, 1), groceries, 1000m);

        var second = await InsertBudgetAsync("B-2", KyivMidnight(2026, 11, 15), groceries, 500m);
        await Assert.ThrowsAsync<InvalidStateException>(() =>
            _host.InScopeAsync(sp => Budgets(sp).SubmitAsync(second, Ct)));

        await _host.InScopeAsync(sp => Budgets(sp).UnsubmitAsync(first, Ct));
        await _host.InScopeAsync(sp => Budgets(sp).SubmitAsync(second, Ct));
    }

    [Fact]
    public async Task IncomeAndExpense_TakeOnlyACategoryOfTheirOwnKind()
    {
        var (account, groceries, salary) = await SetupAsync();

        var expense = await _host.InScopeAsync(sp => Expenses(sp).InsertAsync(new ExpenseDto
        {
            Code = "E-WRONG", Date = KyivMidnight(2026, 11, 2), AccountId = account, CategoryId = salary, Amount = 100m
        }, Ct));
        await Assert.ThrowsAsync<InvalidStateException>(() =>
            _host.InScopeAsync(sp => Expenses(sp).SubmitAsync(expense.Id, Ct)));

        var income = await _host.InScopeAsync(sp => Incomes(sp).InsertAsync(new IncomeDto
        {
            Code = "I-WRONG", Date = KyivMidnight(2026, 11, 2), AccountId = account, CategoryId = groceries, Amount = 100m
        }, Ct));
        await Assert.ThrowsAsync<InvalidStateException>(() =>
            _host.InScopeAsync(sp => Incomes(sp).SubmitAsync(income.Id, Ct)));
    }

    private async Task<(Guid Account, Guid Groceries, Guid Salary)> SetupAsync()
    {
        await _host.InScopeAsync(sp => sp.GetRequiredService<IConstantsManageService>()
            .SetAsync<string>("BaseCurrency", "UAH", new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), Ct));

        var currency = await _host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<CurrencyDto, CurrencyQueryDto>>()
            .InsertAsync(new CurrencyDto { Code = "UAH", Name = "Hryvnia" }, Ct));
        var account = await _host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<AccountDto, AccountQueryDto>>()
            .InsertAsync(new AccountDto { Code = "CARD", Name = "Card", Type = Enums.AccountType.Card, CurrencyId = currency.Id }, Ct));
        var groceries = await _host.InScopeAsync(sp => Categories(sp)
            .InsertAsync(new CategoryDto { Code = "GROC", Name = "Groceries", Kind = Enums.CategoryKind.Expense }, Ct));
        var salary = await _host.InScopeAsync(sp => Categories(sp)
            .InsertAsync(new CategoryDto { Code = "SAL", Name = "Salary", Kind = Enums.CategoryKind.Income }, Ct));
        return (account.Id, groceries.Id, salary.Id);
    }

    private async Task<Guid> InsertBudgetAsync(string code, DateTime date, Guid category, decimal planned)
    {
        var budget = await _host.InScopeAsync(sp => Budgets(sp).InsertAsync(new BudgetDto
        {
            Code = code, Date = date, Lines = [new BudgetLineDto { CategoryId = category, PlannedAmount = planned }]
        }, Ct));
        return budget.Id;
    }

    private async Task<Guid> PostBudgetAsync(string code, DateTime date, Guid category, decimal planned)
    {
        var id = await InsertBudgetAsync(code, date, category, planned);
        await _host.InScopeAsync(sp => Budgets(sp).SubmitAsync(id, Ct));
        return id;
    }

    private async Task PostExpenseAsync(string code, DateTime date, Guid account, Guid category, decimal amount)
    {
        var expense = await _host.InScopeAsync(sp => Expenses(sp).InsertAsync(new ExpenseDto
        {
            Code = code, Date = date, AccountId = account, CategoryId = category, Amount = amount
        }, Ct));
        await _host.InScopeAsync(sp => Expenses(sp).SubmitAsync(expense.Id, Ct));
    }

    private static IDocumentCrudService<BudgetDto, BudgetQueryDto> Budgets(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<BudgetDto, BudgetQueryDto>>();

    private static IDocumentCrudService<ExpenseDto, ExpenseQueryDto> Expenses(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<ExpenseDto, ExpenseQueryDto>>();

    private static IDocumentCrudService<IncomeDto, IncomeQueryDto> Incomes(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<IncomeDto, IncomeQueryDto>>();

    private static IHierarchicalDictionaryCrudService<CategoryNodeDto, CategoryQueryDto> Categories(IServiceProvider sp) =>
        sp.GetRequiredService<IHierarchicalDictionaryCrudService<CategoryNodeDto, CategoryQueryDto>>();

    private static TimeZoneInfo FindKyiv() =>
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Kyiv", out var zone) ? zone : TimeZoneInfo.FindSystemTimeZoneById("Europe/Kiev");

    private sealed class FixedTimeZone(TimeZoneInfo zone) : ICurrentTimeZone
    {
        public TimeZoneInfo? TimeZone => zone;
    }
}
