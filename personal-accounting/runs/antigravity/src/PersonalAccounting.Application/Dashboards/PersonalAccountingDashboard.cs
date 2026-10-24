using System.Globalization;
using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Dashboards;
using Kandra.Application.Abstractions.Services;
using Kandra.Attributes.Application;
using Kandra.Domain.Exceptions;
using Kandra.Forms.Dashboards;
using Kandra.Persistence.Abstractions;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Application.Constants;
using PersonalAccounting.Domain.Commons;
using PersonalAccounting.Forms.Dashboards;

namespace PersonalAccounting.Application.Dashboards;

[KandraDashboardBehavior(typeof(PersonalAccountingDashboardDto))]
public class PersonalAccountingDashboardBehavior(
    IReportsRepository repository,
    IConstantsService constants,
    ICurrentTimeZone timeZone,
    IStringLocalizer localizer,
    TimeProvider? time = null)
    : IDashboardBehavior<PersonalAccountingDashboardDto, PersonalAccountingDashboardResultDto>
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public string ObjectName => "PersonalAccountingDashboard";

    public Task OnNewAsync(PersonalAccountingDashboardDto filters, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<PersonalAccountingDashboardResultDto> GetDataAsync(PersonalAccountingDashboardDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var baseCurrencyCode = (await constants.GetAsync<BaseCurrencyConstant>(now, cancellationToken)).Value;

        // 1. Account balances summary
        var accountSummaries = await repository.GetAccountBalancesSummaryAsync(now, baseCurrencyCode, cancellationToken);
        var withoutRate = accountSummaries.FirstOrDefault(a => !a.HasRate);
        if (withoutRate is not null)
            throw new InvalidStateException(localizer["Error_MissingExchangeRate", withoutRate.CurrencyCode, now.ToString("yyyy-MM-dd")]);
        var totalBalanceInBase = accountSummaries.Sum(a => a.BalanceInBase);

        // 2. Six months turnovers
        var userTz = timeZone.TimeZone;
        var months = DashboardPeriods.Last(6, DashboardBucketSize.Month, userTz, now);
        var fromUtc = months[0].StartUtc;
        var toUtc = months[^1].EndUtc;

        var turnovers = await repository.GetMonthlyTurnoversAsync(fromUtc, toUtc, cancellationToken);

        // 3. This month and last month income/expense
        var currentMonthStart = months[^1].StartUtc;
        var currentMonthEnd = months[^1].EndUtc;

        var thisMonthTurnover = turnovers.FirstOrDefault(t => t.Year == months[^1].Start.Year && t.Month == months[^1].Start.Month);
        var lastMonthTurnover = months.Count > 1 ? turnovers.FirstOrDefault(t => t.Year == months[^2].Start.Year && t.Month == months[^2].Start.Month) : null;

        var thisMonthIncome = thisMonthTurnover?.IncomeInBase ?? 0m;
        var lastMonthIncome = lastMonthTurnover?.IncomeInBase;
        var thisMonthExpense = thisMonthTurnover?.ExpenseInBase ?? 0m;
        var lastMonthExpense = lastMonthTurnover?.ExpenseInBase;

        // 4. Budget spent share
        var (budgetPlanned, budgetActual) = await repository.GetBudgetSpentAsync(months[^1].Start.ToDateTime(TimeOnly.MinValue), cancellationToken);
        decimal? budgetShare = budgetPlanned > 0m ? decimal.Round(budgetActual / budgetPlanned * 100m, 1, MidpointRounding.AwayFromZero) : null;

        // 5. 6-months Bar chart
        var incomeSeries = new List<decimal>();
        var expenseSeries = new List<decimal>();
        var monthLabels = new List<string>();

        foreach (var m in months)
        {
            monthLabels.Add(m.Start.ToString("MMM yy", CultureInfo.CurrentCulture));
            var t = turnovers.FirstOrDefault(x => x.Year == m.Start.Year && x.Month == m.Start.Month);
            incomeSeries.Add(t?.IncomeInBase ?? 0m);
            expenseSeries.Add(t?.ExpenseInBase ?? 0m);
        }

        var trendChart = new CategoryChartData
        {
            Labels = monthLabels
        }
        .AddSeries(localizer["Dashboard_Income"].Value ?? "Income", incomeSeries)
        .AddSeries(localizer["Dashboard_Expense"].Value ?? "Expense", expenseSeries);

        // 6. Expenses by category (donut - top 5 + Other)
        var categoryExpenses = await repository.GetCategoryExpensesAsync(currentMonthStart, currentMonthEnd, cancellationToken);
        var donutChart = new CategoryChartData();
        var top5 = categoryExpenses.Take(5).ToList();
        var otherAmount = categoryExpenses.Skip(5).Sum(x => x.AmountInBase);

        var donutLabels = top5.Select(x => x.CategoryName).ToList();
        var donutValues = top5.Select(x => x.AmountInBase).ToList();
        if (otherAmount > 0m)
        {
            donutLabels.Add(localizer["Dashboard_Other"].Value ?? "Other");
            donutValues.Add(otherAmount);
        }
        donutChart.Labels = donutLabels;
        donutChart.AddSeries(localizer["Dashboard_Expense"].Value ?? "Expense", donutValues);

        // 7. Table rows
        var tableRows = accountSummaries.Select(a => new AccountBalanceDashboardRowDto
        {
            AccountName = a.AccountName,
            AccountType = localizer[$"Enum_AccountType_{a.AccountType}"].Value,
            CurrencyCode = a.CurrencyCode,
            Balance = a.Balance,
            BalanceInBase = a.BalanceInBase
        }).ToList();

        return new PersonalAccountingDashboardResultDto
        {
            Filters = filters,
            TotalBalance = new KpiData
            {
                Value = totalBalanceInBase,
                Trend = KpiTrend.Neutral
            },
            ThisMonthIncome = new KpiData
            {
                Value = thisMonthIncome,
                Previous = lastMonthIncome,
                Trend = KpiTrend.HigherIsBetter
            },
            ThisMonthExpense = new KpiData
            {
                Value = thisMonthExpense,
                Previous = lastMonthExpense,
                Trend = KpiTrend.LowerIsBetter
            },
            BudgetSpentShare = new KpiData
            {
                Value = budgetShare ?? 0m,
                Unit = "%",
                Trend = KpiTrend.LowerIsBetter
            },
            IncomeExpenseTrend = trendChart,
            ExpensesByCategory = donutChart,
            AccountBalances = new TableData<AccountBalanceDashboardRowDto>
            {
                Rows = tableRows
            }
        };
    }
}
