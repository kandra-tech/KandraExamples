using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Dashboards;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Forms.Dashboards;
using HomePantry.Forms.Reports;

namespace HomePantry.Forms.Dashboards;

[KandraDashboardForm(Name = "PantryDashboard", ResultDtoType = typeof(PantryDashboardResultDto), ValidatorType = typeof(PantryDashboardDtoValidator))]
[AutoRefresh(60, Intervals = [30, 60, 300, 600])]
[Description("Home pantry operational dashboard displaying current stock value, below-minimum warnings, upcoming expirations, spending trends, and breakdown by product group.")]
public sealed class PantryDashboardDto : IDashboardFiltersDto
{
}

public sealed class ExpiringBatchRow
{
    [Caption("Product")]
    public string Product { get; set; } = "";

    [Caption("Location")]
    public string Location { get; set; } = "";

    [Caption("BestBeforeDate")]
    [Format("d")]
    public DateTime BestBeforeDate { get; set; }

    [Caption("DaysLeft")]
    [Format("N0")]
    public int DaysLeft { get; set; }
}

public sealed class PantryDashboardResultDto : DashboardResult<PantryDashboardDto>
{
    [KpiTile, Widget(Order = 10, Width = 3, Height = WidgetHeight.Small)]
    [Caption("Dashboard_StockValue")]
    [Format("N2")]
    [DrillDown(typeof(ReportStockOverviewDto))]
    public KpiData StockValue { get; set; } = new();

    [KpiTile, Widget(Order = 20, Width = 3, Height = WidgetHeight.Small)]
    [Caption("Dashboard_BelowMinimum")]
    [Format("N0")]
    [DrillDown(typeof(ReportStockOverviewDto))]
    public KpiData BelowMinimum { get; set; } = new();

    [KpiTile, Widget(Order = 30, Width = 3, Height = WidgetHeight.Small)]
    [Caption("Dashboard_Expiring")]
    [Format("N0")]
    [DrillDown(typeof(ReportExpiringDto))]
    public KpiData ExpiringBatches { get; set; } = new();

    [KpiTile, Widget(Order = 40, Width = 3, Height = WidgetHeight.Small)]
    [Caption("Dashboard_MonthlySpending")]
    [Format("N2")]
    [DrillDown(typeof(ReportSpendingDto))]
    public KpiData MonthlySpending { get; set; } = new();

    [BarChart, Widget(Order = 50, Width = 6, Height = WidgetHeight.Medium)]
    [Caption("Dashboard_SpendingHistory")]
    [Format("N2")]
    [DrillDown(typeof(ReportSpendingDto))]
    public CategoryChartData SpendingHistory { get; set; } = new();

    [PieChart(Donut = true), Widget(Order = 60, Width = 6, Height = WidgetHeight.Medium)]
    [Caption("Dashboard_StockByGroup")]
    [Format("N2")]
    [DrillDown(typeof(ReportStockOverviewDto))]
    public CategoryChartData StockByGroup { get; set; } = new();

    [TableWidget, Widget(Order = 70, Width = 12, Height = WidgetHeight.Medium)]
    [Caption("Dashboard_NextExpiringBatches")]
    [DrillDown(typeof(ReportExpiringDto))]
    public TableData<ExpiringBatchRow> NextExpiringBatches { get; set; } = new();
}

public class PantryDashboardDtoValidator : AbstractValidator<PantryDashboardDto>
{
}
