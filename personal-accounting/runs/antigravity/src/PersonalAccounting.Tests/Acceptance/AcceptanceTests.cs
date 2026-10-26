using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Services;
using Kandra.Forms.Querying;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Microsoft.Extensions.DependencyInjection;
using PersonalAccounting.Domain.Entities.Registers;
using PersonalAccounting.Forms.Dashboards;
using PersonalAccounting.Forms.Dictionaries;
using PersonalAccounting.Forms.Documents;
using PersonalAccounting.Forms.Reports;
using PersonalAccounting.Tests.Support;

namespace PersonalAccounting.Tests.Acceptance;

public sealed class AcceptanceTests : IDisposable
{
    private readonly PersonalAccountingTestHost _host = new();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public void Dispose() => _host.Dispose();

    private static DateTime Day(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private static IDocumentCrudService<TDoc, TQuery> Docs<TDoc, TQuery>(IServiceProvider sp)
        where TDoc : class, new()
        where TQuery : DocumentQueryDto, new() =>
        sp.GetRequiredService<IDocumentCrudService<TDoc, TQuery>>();

    private static IDictionaryCrudService<TDto, TQuery> Dics<TDto, TQuery>(IServiceProvider sp)
        where TDto : class, new()
        where TQuery : DictionaryQueryDto, new() =>
        sp.GetRequiredService<IDictionaryCrudService<TDto, TQuery>>();

    private static IHierarchicalDictionaryCrudService<CategoryNodeDto, CategoryQueryDto> Categories(IServiceProvider sp) =>
        sp.GetRequiredService<IHierarchicalDictionaryCrudService<CategoryNodeDto, CategoryQueryDto>>();

    private static IReportService<TFilters, TResult> Reports<TFilters, TResult>(IServiceProvider sp)
        where TFilters : class, Kandra.Forms.Reports.IReportFiltersDto, new()
        where TResult : class, Kandra.Forms.Reports.IReportResult<TFilters> =>
        sp.GetRequiredService<IReportService<TFilters, TResult>>();

    private async Task SetupBaseCurrencyAsync(string code = "UAH")
    {
        await _host.InScopeAsync(async sp =>
        {
            var constants = sp.GetRequiredService<IConstantsManageService>();
            await constants.SetJsonAsync("BaseCurrency", System.Text.Json.JsonSerializer.Serialize(new { Value = code }), Day(2000, 1, 1), Ct);
        });
    }

    [Fact]
    public async Task Check1_PostingAnExpense_ReducesAccountBalance_AndIncreasesCategoryTurnover()
    {
        await SetupBaseCurrencyAsync();

        var curr = await _host.InScopeAsync(sp => Dics<CurrencyDto, CurrencyQueryDto>(sp)
            .InsertAsync(new CurrencyDto { Code = "UAH", Name = "Hryvnia" }, Ct));

        var acc = await _host.InScopeAsync(sp => Dics<AccountDto, AccountQueryDto>(sp)
            .InsertAsync(new AccountDto { Code = "CASH", Name = "Cash", Type = Enums.AccountType.Cash, CurrencyId = curr.Id }, Ct));

        var cat = await _host.InScopeAsync(sp => Categories(sp)
            .InsertAsync(new CategoryDto { Code = "GROC", Name = "Groceries", Kind = Enums.CategoryKind.Expense }, Ct));

        // Opening balance 5000
        var opBal = await _host.InScopeAsync(sp => Docs<OpeningBalancesDto, OpeningBalancesQueryDto>(sp).InsertAsync(new OpeningBalancesDto
        {
            Code = "OB1",
            Date = Day(2026, 5, 1),
            Lines = [new OpeningBalancesLineDto { AccountId = acc.Id, Amount = 5000m }]
        }, Ct));
        await _host.InScopeAsync(sp => Docs<OpeningBalancesDto, OpeningBalancesQueryDto>(sp).SubmitAsync(opBal.Id, Ct));

        // Expense 1200
        var exp = await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).InsertAsync(new ExpenseDto
        {
            Code = "EXP1",
            Date = Day(2026, 5, 10),
            AccountId = acc.Id,
            CategoryId = cat.Id,
            Amount = 1200m
        }, Ct));
        await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).SubmitAsync(exp.Id, Ct));

        // 1. Check account balance
        var balance = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IBalanceReader<AccountBalanceMovement, AccountBalancesBalance>>();
            var rows = await reader.Balances(q => q.Where(b => b.Dimensions.AccountId == acc.Id), Ct);
            return rows.Sum(r => r.Resources.Amount);
        });
        Assert.Equal(3800m, balance);

        // 2. Check category turnover
        var turnover = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<ITurnoverReader<CategoryTurnoverMovement>>();
            var movements = await reader.Movements(Day(2026, 5, 1), Day(2026, 5, 31), q => q.Where(m => m.Dimensions.CategoryId == cat.Id), Ct);
            return movements.Sum(m => m.Resources.AmountInAccountCurrency);
        });
        Assert.Equal(1200m, turnover);
    }

    [Fact]
    public async Task Check2_TransferBetweenAccountsInDifferentCurrencies_ChangesBothBalancesCorrectly()
    {
        await SetupBaseCurrencyAsync();

        var uah = await _host.InScopeAsync(sp => Dics<CurrencyDto, CurrencyQueryDto>(sp)
            .InsertAsync(new CurrencyDto { Code = "UAH", Name = "Hryvnia" }, Ct));
        var usd = await _host.InScopeAsync(sp => Dics<CurrencyDto, CurrencyQueryDto>(sp)
            .InsertAsync(new CurrencyDto { Code = "USD", Name = "US Dollar" }, Ct));

        var cashAcc = await _host.InScopeAsync(sp => Dics<AccountDto, AccountQueryDto>(sp)
            .InsertAsync(new AccountDto { Code = "CASH", Name = "Cash UAH", Type = Enums.AccountType.Cash, CurrencyId = uah.Id }, Ct));
        var savingsAcc = await _host.InScopeAsync(sp => Dics<AccountDto, AccountQueryDto>(sp)
            .InsertAsync(new AccountDto { Code = "SAVINGS", Name = "Savings USD", Type = Enums.AccountType.Savings, CurrencyId = usd.Id }, Ct));

        // Opening balances: Cash = 10000 UAH, Savings = 100 USD
        var opBal = await _host.InScopeAsync(sp => Docs<OpeningBalancesDto, OpeningBalancesQueryDto>(sp).InsertAsync(new OpeningBalancesDto
        {
            Code = "OB1",
            Date = Day(2026, 5, 1),
            Lines =
            [
                new OpeningBalancesLineDto { AccountId = cashAcc.Id, Amount = 10000m },
                new OpeningBalancesLineDto { AccountId = savingsAcc.Id, Amount = 100m }
            ]
        }, Ct));
        await _host.InScopeAsync(sp => Docs<OpeningBalancesDto, OpeningBalancesQueryDto>(sp).SubmitAsync(opBal.Id, Ct));

        // Transfer 4100 UAH -> 100 USD
        var transfer = await _host.InScopeAsync(sp => Docs<TransferDto, TransferQueryDto>(sp).InsertAsync(new TransferDto
        {
            Code = "TR1",
            Date = Day(2026, 5, 10),
            FromAccountId = cashAcc.Id,
            ToAccountId = savingsAcc.Id,
            AmountOut = 4100m,
            AmountIn = 100m
        }, Ct));
        await _host.InScopeAsync(sp => Docs<TransferDto, TransferQueryDto>(sp).SubmitAsync(transfer.Id, Ct));

        // Assert Cash UAH balance
        var cashBalance = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IBalanceReader<AccountBalanceMovement, AccountBalancesBalance>>();
            var rows = await reader.Balances(q => q.Where(b => b.Dimensions.AccountId == cashAcc.Id), Ct);
            return rows.Sum(r => r.Resources.Amount);
        });
        Assert.Equal(5900m, cashBalance);

        // Assert Savings USD balance
        var savingsBalance = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IBalanceReader<AccountBalanceMovement, AccountBalancesBalance>>();
            var rows = await reader.Balances(q => q.Where(b => b.Dimensions.AccountId == savingsAcc.Id), Ct);
            return rows.Sum(r => r.Resources.Amount);
        });
        Assert.Equal(200m, savingsBalance);
    }

    [Fact]
    public async Task Check3_ChangingPostedDocument_UnpostingIt_OrDeletingIt_UpdatesBalancesAndTurnoverCorrectly()
    {
        await SetupBaseCurrencyAsync();

        var curr = await _host.InScopeAsync(sp => Dics<CurrencyDto, CurrencyQueryDto>(sp)
            .InsertAsync(new CurrencyDto { Code = "UAH", Name = "Hryvnia" }, Ct));
        var acc = await _host.InScopeAsync(sp => Dics<AccountDto, AccountQueryDto>(sp)
            .InsertAsync(new AccountDto { Code = "CARD", Name = "Card", Type = Enums.AccountType.Card, CurrencyId = curr.Id }, Ct));
        var cat = await _host.InScopeAsync(sp => Categories(sp)
            .InsertAsync(new CategoryDto { Code = "GROC", Name = "Groceries", Kind = Enums.CategoryKind.Expense }, Ct));

        // 1. Post expense 500
        var exp = await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).InsertAsync(new ExpenseDto
        {
            Code = "EXP1",
            Date = Day(2026, 5, 10),
            AccountId = acc.Id,
            CategoryId = cat.Id,
            Amount = 500m
        }, Ct));
        await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).SubmitAsync(exp.Id, Ct));

        var bal1 = await GetBalance(acc.Id);
        var turn1 = await GetTurnover(cat.Id, Day(2026, 5, 1), Day(2026, 5, 31));
        Assert.Equal(-500m, bal1);
        Assert.Equal(500m, turn1);

        // 2. Change posted document to 800
        var posted = await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).GetByIdAAsync(exp.Id, Ct));
        posted.Amount = 800m;
        await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).UpdateAsync(posted, Ct));

        var bal2 = await GetBalance(acc.Id);
        var turn2 = await GetTurnover(cat.Id, Day(2026, 5, 1), Day(2026, 5, 31));
        Assert.Equal(-800m, bal2);
        Assert.Equal(800m, turn2);

        // 3. Un-post document
        await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).UnsubmitAsync(exp.Id, Ct));

        var bal3 = await GetBalance(acc.Id);
        var turn3 = await GetTurnover(cat.Id, Day(2026, 5, 1), Day(2026, 5, 31));
        Assert.Equal(0m, bal3);
        Assert.Equal(0m, turn3);

        // The reports must not count the un-posted document's movements either
        var report = await _host.InScopeAsync(sp => Reports<ReportBudgetVsActualDto, BudgetVsActualReportResult>(sp)
            .RunAsync(new ReportBudgetVsActualDto { Month = Day(2026, 5, 1) }, Ct));
        Assert.DoesNotContain(report.Data!.Lines, l => l.Id == cat.Id);

        // 4. Delete document succeeds after unposting
        await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).DeleteAsync(exp.Id, Ct));
    }

    [Fact]
    public async Task Check4_BudgetVsActual_EqualsTheSumOfPostedExpensesPerCategoryForTheMonth()
    {
        await SetupBaseCurrencyAsync();

        var curr = await _host.InScopeAsync(sp => Dics<CurrencyDto, CurrencyQueryDto>(sp)
            .InsertAsync(new CurrencyDto { Code = "UAH", Name = "Hryvnia" }, Ct));
        var acc = await _host.InScopeAsync(sp => Dics<AccountDto, AccountQueryDto>(sp)
            .InsertAsync(new AccountDto { Code = "CARD", Name = "Card", Type = Enums.AccountType.Card, CurrencyId = curr.Id }, Ct));

        var catGroc = await _host.InScopeAsync(sp => Categories(sp)
            .InsertAsync(new CategoryDto { Code = "GROC", Name = "Groceries", Kind = Enums.CategoryKind.Expense }, Ct));
        var catTrans = await _host.InScopeAsync(sp => Categories(sp)
            .InsertAsync(new CategoryDto { Code = "TRANS", Name = "Transport", Kind = Enums.CategoryKind.Expense }, Ct));

        // Budget for May 2026: Groceries 3000, Transport 1000
        var budget = await _host.InScopeAsync(sp => Docs<BudgetDto, BudgetQueryDto>(sp).InsertAsync(new BudgetDto
        {
            Code = "B1",
            Date = Day(2026, 5, 1),
            Lines =
            [
                new BudgetLineDto { CategoryId = catGroc.Id, PlannedAmount = 3000m },
                new BudgetLineDto { CategoryId = catTrans.Id, PlannedAmount = 1000m }
            ]
        }, Ct));
        await _host.InScopeAsync(sp => Docs<BudgetDto, BudgetQueryDto>(sp).SubmitAsync(budget.Id, Ct));

        // Post expenses: Groceries 1200 + 800 = 2000; Transport 1500
        var exp1 = await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).InsertAsync(new ExpenseDto
        {
            Code = "E1", Date = Day(2026, 5, 5), AccountId = acc.Id, CategoryId = catGroc.Id, Amount = 1200m
        }, Ct));
        await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).SubmitAsync(exp1.Id, Ct));

        var exp2 = await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).InsertAsync(new ExpenseDto
        {
            Code = "E2", Date = Day(2026, 5, 15), AccountId = acc.Id, CategoryId = catGroc.Id, Amount = 800m
        }, Ct));
        await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).SubmitAsync(exp2.Id, Ct));

        var exp3 = await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).InsertAsync(new ExpenseDto
        {
            Code = "E3", Date = Day(2026, 5, 20), AccountId = acc.Id, CategoryId = catTrans.Id, Amount = 1500m
        }, Ct));
        await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).SubmitAsync(exp3.Id, Ct));

        // Run Budget vs Actual report
        var report = await _host.InScopeAsync(sp => Reports<ReportBudgetVsActualDto, BudgetVsActualReportResult>(sp)
            .RunAsync(new ReportBudgetVsActualDto { Month = Day(2026, 5, 1) }, Ct));

        Assert.NotNull(report);
        Assert.NotNull(report.Data);
        var grocRow = Assert.Single(report.Data.Lines, l => l.Id == catGroc.Id);
        Assert.Equal(3000m, grocRow.PlannedAmount);
        Assert.Equal(2000m, grocRow.ActualAmount);
        Assert.Equal(1000m, grocRow.RemainingAmount);
        Assert.Equal(66.7m, grocRow.PercentUsed);
        Assert.False(grocRow.IsOverspent);

        var transRow = Assert.Single(report.Data.Lines, l => l.Id == catTrans.Id);
        Assert.Equal(1000m, transRow.PlannedAmount);
        Assert.Equal(1500m, transRow.ActualAmount);
        Assert.Equal(-500m, transRow.RemainingAmount);
        Assert.Equal(150.0m, transRow.PercentUsed);
        Assert.True(transRow.IsOverspent);
    }

    [Fact]
    public async Task Check5_AccountStatementClosingBalance_EqualsAccountBalanceOnThatDate()
    {
        await SetupBaseCurrencyAsync();

        var curr = await _host.InScopeAsync(sp => Dics<CurrencyDto, CurrencyQueryDto>(sp)
            .InsertAsync(new CurrencyDto { Code = "UAH", Name = "Hryvnia" }, Ct));
        var acc = await _host.InScopeAsync(sp => Dics<AccountDto, AccountQueryDto>(sp)
            .InsertAsync(new AccountDto { Code = "CASH", Name = "Cash", Type = Enums.AccountType.Cash, CurrencyId = curr.Id }, Ct));
        var catGroc = await _host.InScopeAsync(sp => Categories(sp)
            .InsertAsync(new CategoryDto { Code = "GROC", Name = "Groceries", Kind = Enums.CategoryKind.Expense }, Ct));
        var catSal = await _host.InScopeAsync(sp => Categories(sp)
            .InsertAsync(new CategoryDto { Code = "SAL", Name = "Salary", Kind = Enums.CategoryKind.Income }, Ct));

        // Opening balance 5000 on 2026-05-01
        var opBal = await _host.InScopeAsync(sp => Docs<OpeningBalancesDto, OpeningBalancesQueryDto>(sp).InsertAsync(new OpeningBalancesDto
        {
            Code = "OB1", Date = Day(2026, 5, 1), Lines = [new OpeningBalancesLineDto { AccountId = acc.Id, Amount = 5000m }]
        }, Ct));
        await _host.InScopeAsync(sp => Docs<OpeningBalancesDto, OpeningBalancesQueryDto>(sp).SubmitAsync(opBal.Id, Ct));

        // Expense 1500 on 2026-05-10
        var exp = await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).InsertAsync(new ExpenseDto
        {
            Code = "E1", Date = Day(2026, 5, 10), AccountId = acc.Id, CategoryId = catGroc.Id, Amount = 1500m
        }, Ct));
        await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).SubmitAsync(exp.Id, Ct));

        // Income 2500 on 2026-05-15
        var inc = await _host.InScopeAsync(sp => Docs<IncomeDto, IncomeQueryDto>(sp).InsertAsync(new IncomeDto
        {
            Code = "I1", Date = Day(2026, 5, 15), AccountId = acc.Id, CategoryId = catSal.Id, Amount = 2500m
        }, Ct));
        await _host.InScopeAsync(sp => Docs<IncomeDto, IncomeQueryDto>(sp).SubmitAsync(inc.Id, Ct));

        // Run Account Statement report for May 2026
        var report = await _host.InScopeAsync(sp => Reports<ReportAccountStatementDto, AccountStatementReportResult>(sp)
            .RunAsync(new ReportAccountStatementDto
            {
                AccountId = acc.Id,
                DateRange = new DateRangeDto { From = Day(2026, 5, 1), To = Day(2026, 5, 31, 23, 59, 59) }
            }, Ct));

        Assert.NotNull(report);
        Assert.NotNull(report.Data);
        var lines = report.Data.Lines.ToList();
        var closingLine = lines.Last();
        var statementClosingBalance = closingLine.RunningBalance;
        Assert.Equal(6000m, statementClosingBalance);

        // Account balance register as of 2026-05-31
        var actualBalance = await GetBalance(acc.Id);

        Assert.Equal(statementClosingBalance, actualBalance);
    }

    [Fact]
    public async Task Check6_DashboardFiguresForThisMonth_EqualReportsForSameMonth()
    {
        await SetupBaseCurrencyAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

        var curr = await _host.InScopeAsync(sp => Dics<CurrencyDto, CurrencyQueryDto>(sp)
            .InsertAsync(new CurrencyDto { Code = "UAH", Name = "Hryvnia" }, Ct));
        var accCash = await _host.InScopeAsync(sp => Dics<AccountDto, AccountQueryDto>(sp)
            .InsertAsync(new AccountDto { Code = "CASH", Name = "Cash", Type = Enums.AccountType.Cash, CurrencyId = curr.Id }, Ct));
        var accBank = await _host.InScopeAsync(sp => Dics<AccountDto, AccountQueryDto>(sp)
            .InsertAsync(new AccountDto { Code = "BANK", Name = "Bank", Type = Enums.AccountType.Bank, CurrencyId = curr.Id }, Ct));

        var catGroc = await _host.InScopeAsync(sp => Categories(sp)
            .InsertAsync(new CategoryDto { Code = "GROC", Name = "Groceries", Kind = Enums.CategoryKind.Expense }, Ct));
        var catRent = await _host.InScopeAsync(sp => Categories(sp)
            .InsertAsync(new CategoryDto { Code = "RENT", Name = "Rent", Kind = Enums.CategoryKind.Expense }, Ct));

        // Opening balance at month start
        var opBal = await _host.InScopeAsync(sp => Docs<OpeningBalancesDto, OpeningBalancesQueryDto>(sp).InsertAsync(new OpeningBalancesDto
        {
            Code = "OB1",
            Date = monthStart,
            Lines =
            [
                new OpeningBalancesLineDto { AccountId = accCash.Id, Amount = 10000m },
                new OpeningBalancesLineDto { AccountId = accBank.Id, Amount = 20000m }
            ]
        }, Ct));
        await _host.InScopeAsync(sp => Docs<OpeningBalancesDto, OpeningBalancesQueryDto>(sp).SubmitAsync(opBal.Id, Ct));

        // Expenses in current month
        var expDate = monthStart.AddHours(2);
        var exp1 = await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).InsertAsync(new ExpenseDto
        {
            Code = "E1", Date = expDate, AccountId = accCash.Id, CategoryId = catGroc.Id, Amount = 3000m
        }, Ct));
        await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).SubmitAsync(exp1.Id, Ct));

        var exp2 = await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).InsertAsync(new ExpenseDto
        {
            Code = "E2", Date = expDate, AccountId = accBank.Id, CategoryId = catRent.Id, Amount = 12000m
        }, Ct));
        await _host.InScopeAsync(sp => Docs<ExpenseDto, ExpenseQueryDto>(sp).SubmitAsync(exp2.Id, Ct));

        // Compute Dashboard
        var dashboardData = await _host.InScopeAsync(async sp =>
        {
            var behavior = sp.GetRequiredService<IDashboardBehavior<PersonalAccountingDashboardDto, PersonalAccountingDashboardResultDto>>();
            var lookup = sp.GetRequiredService<ILookupRepository>();
            return await behavior.GetDataAsync(new PersonalAccountingDashboardDto(), lookup, Ct);
        });

        // Run Spending by category report for current month
        var spendingReport = await _host.InScopeAsync(sp => Reports<ReportSpendingByCategoryDto, SpendingByCategoryReportResult>(sp)
            .RunAsync(new ReportSpendingByCategoryDto
            {
                DateRange = new DateRangeDto { From = monthStart, To = monthEnd }
            }, Ct));

        // Check expense matches
        var totalReportSpending = spendingReport.Data!.Lines.Sum(l => l.AmountInBase);
        Assert.Equal(15000m, totalReportSpending);
        Assert.Equal(totalReportSpending, dashboardData.ThisMonthExpense.Value);

        // Run Account Statement for Cash and Bank
        var cashStatement = await _host.InScopeAsync(sp => Reports<ReportAccountStatementDto, AccountStatementReportResult>(sp)
            .RunAsync(new ReportAccountStatementDto
            {
                AccountId = accCash.Id,
                DateRange = new DateRangeDto { From = monthStart, To = monthEnd }
            }, Ct));
        var bankStatement = await _host.InScopeAsync(sp => Reports<ReportAccountStatementDto, AccountStatementReportResult>(sp)
            .RunAsync(new ReportAccountStatementDto
            {
                AccountId = accBank.Id,
                DateRange = new DateRangeDto { From = monthStart, To = monthEnd }
            }, Ct));

        var dashboardCashRow = Assert.Single(dashboardData.AccountBalances.Rows, a => a.AccountName == "Cash");
        var dashboardBankRow = Assert.Single(dashboardData.AccountBalances.Rows, a => a.AccountName == "Bank");

        Assert.Equal(cashStatement.Data!.Lines.Last().RunningBalance, dashboardCashRow.Balance);
        Assert.Equal(bankStatement.Data!.Lines.Last().RunningBalance, dashboardBankRow.Balance);
    }

    private Task<decimal> GetBalance(Guid accountId) => _host.InScopeAsync(async sp =>
    {
        var reader = sp.GetRequiredService<IBalanceReader<AccountBalanceMovement, AccountBalancesBalance>>();
        var rows = await reader.Balances(q => q.Where(b => b.Dimensions.AccountId == accountId), Ct);
        return rows.Sum(r => r.Resources.Amount);
    });

    private Task<decimal> GetTurnover(Guid categoryId, DateTime from, DateTime to) => _host.InScopeAsync(async sp =>
    {
        var reader = sp.GetRequiredService<ITurnoverReader<CategoryTurnoverMovement>>();
        var rows = await reader.Movements(from, to, q => q.Where(m => m.Dimensions.CategoryId == categoryId), Ct);
        return rows.Sum(r => r.Resources.AmountInAccountCurrency);
    });
}
