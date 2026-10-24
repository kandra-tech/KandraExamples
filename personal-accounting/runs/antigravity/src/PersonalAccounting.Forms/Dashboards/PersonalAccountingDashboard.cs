using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Dashboards;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Forms.Dashboards;

namespace PersonalAccounting.Forms.Dashboards;

public sealed class AccountBalanceDashboardRowDto
{
    [Caption("Account")]
    public string AccountName { get; set; } = "";

    [Caption("Type")]
    public string AccountType { get; set; } = "";

    [Caption("Currency")]
    public string CurrencyCode { get; set; } = "";

    [Caption("Balance")]
    [Format("N2")]
    public decimal Balance { get; set; }

    [Caption("BalanceInBase")]
    [Format("N2")]
    public decimal BalanceInBase { get; set; }
}

[KandraDashboardForm(Name = "PersonalAccountingDashboard", ResultDtoType = typeof(PersonalAccountingDashboardResultDto), ValidatorType = typeof(PersonalAccountingDashboardDtoValidator))]
[AutoRefresh(60, Intervals = [60, 300, 600])]
[Description("Personal Accounting overview: balances, monthly income/expense trends, budget progress, and expense distribution.")]
public sealed class PersonalAccountingDashboardDto : IDashboardFiltersDto
{
}

public sealed class PersonalAccountingDashboardResultDto : DashboardResult<PersonalAccountingDashboardDto>
{
    [KpiTile, Widget(Order = 10, Width = 3, Height = WidgetHeight.Small)]
    [Caption("Dashboard_TotalBalance")]
    [Format("N2")]
    public KpiData TotalBalance { get; set; } = new();

    [KpiTile, Widget(Order = 20, Width = 3, Height = WidgetHeight.Small)]
    [Caption("Dashboard_ThisMonthIncome")]
    [Format("N2")]
    public KpiData ThisMonthIncome { get; set; } = new();

    [KpiTile, Widget(Order = 30, Width = 3, Height = WidgetHeight.Small)]
    [Caption("Dashboard_ThisMonthExpense")]
    [Format("N2")]
    public KpiData ThisMonthExpense { get; set; } = new();

    [KpiTile, Widget(Order = 40, Width = 3, Height = WidgetHeight.Small)]
    [Caption("Dashboard_BudgetSpentShare")]
    [Format("N1")]
    public KpiData BudgetSpentShare { get; set; } = new();

    [BarChart, Widget(Order = 50, Width = 7, Height = WidgetHeight.Medium)]
    [Caption("Dashboard_IncomeExpense6Months")]
    [Format("N0")]
    public CategoryChartData IncomeExpenseTrend { get; set; } = new();

    [PieChart(Donut = true), Widget(Order = 60, Width = 5, Height = WidgetHeight.Medium)]
    [Caption("Dashboard_ExpensesByCategory")]
    [Format("N2")]
    public CategoryChartData ExpensesByCategory { get; set; } = new();

    [TableWidget, Widget(Order = 70, Width = 12, Height = WidgetHeight.Medium)]
    [Caption("Dashboard_AccountBalances")]
    public TableData<AccountBalanceDashboardRowDto> AccountBalances { get; set; } = new();
}

public class PersonalAccountingDashboardDtoValidator : AbstractValidator<PersonalAccountingDashboardDto>
{
    public PersonalAccountingDashboardDtoValidator()
    {
    }
}
