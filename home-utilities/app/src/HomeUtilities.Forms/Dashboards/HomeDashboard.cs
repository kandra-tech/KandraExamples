using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Dashboards;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Forms.Dashboards;
using HomeUtilities.Forms.Dictionaries;
using HomeUtilities.Forms.Reports;

namespace HomeUtilities.Forms.Dashboards;

[KandraDashboardForm(Name = "HomeDashboard", ResultDtoType = typeof(HomeDashboardResultDto), ValidatorType = typeof(HomeDashboardDtoValidator))]
[AutoRefresh(300, Intervals = [60, 300, 600, 1800])]
[Description("Home dashboard providing KPI tiles, cost breakdowns, consumption trends, and provider debt tables.")]
public sealed class HomeDashboardDto : IDashboardFiltersDto
{
    [Caption("HomeDashboard_Service")]
    [Search(typeof(ServiceNodeDto))]
    [Description("The utility service to show consumption trends for (default Electricity).")]
    public Guid ServiceId { get; set; }
}

public sealed class HomeDashboardResultDto : DashboardResult<HomeDashboardDto>
{
    [KpiTile, Widget(Order = 10, Width = 4, Height = WidgetHeight.Small)]
    [Caption("HomeDashboard_TotalDebt")]
    [Hint("HomeDashboard_TotalDebt_Hint")]
    [Format("N2")]
    [DrillDown(typeof(ReportProviderStatementDto))]
    public KpiData TotalDebtKpi { get; set; } = new();

    [KpiTile, Widget(Order = 20, Width = 4, Height = WidgetHeight.Small)]
    [Caption("HomeDashboard_LastMonthInvoices")]
    [Hint("HomeDashboard_LastMonthInvoices_Hint")]
    [Format("N2")]
    [DrillDown(typeof(ReportCostsDto))]
    public KpiData LastMonthInvoicesKpi { get; set; } = new();

    [KpiTile, Widget(Order = 30, Width = 4, Height = WidgetHeight.Small)]
    [Caption("HomeDashboard_MissingReadings")]
    [Hint("HomeDashboard_MissingReadings_Hint")]
    [Format("N0")]
    [DrillDown(typeof(ReportMeterReadingsHistoryDto))]
    public KpiData MissingReadingsKpi { get; set; } = new();

    [BarChart(Stacked = true), Widget(Order = 40, Width = 6, Height = WidgetHeight.Medium)]
    [Caption("HomeDashboard_CostsByServiceGroup")]
    [Hint("HomeDashboard_CostsByServiceGroup_Hint")]
    [Format("N0")]
    [DrillDown(typeof(ReportCostsDto))]
    public CategoryChartData CostsByServiceGroup { get; set; } = new();

    [LineChart, Widget(Order = 50, Width = 6, Height = WidgetHeight.Medium)]
    [Caption("HomeDashboard_ServiceConsumption")]
    [Hint("HomeDashboard_ServiceConsumption_Hint")]
    [Format("N3")]
    [DrillDown(typeof(ReportConsumptionDto))]
    public CategoryChartData ServiceConsumption { get; set; } = new();

    [TableWidget, Widget(Order = 60, Width = 6, Height = WidgetHeight.Large)]
    [Caption("HomeDashboard_ProviderDebts")]
    [Hint("HomeDashboard_ProviderDebts_Hint")]
    [DrillDown(typeof(ReportProviderStatementDto))]
    public TableData<ProviderDebtRow> ProviderDebts { get; set; } = new();

    [TableWidget, Widget(Order = 70, Width = 6, Height = WidgetHeight.Large)]
    [Caption("HomeDashboard_MeterAlerts")]
    [Hint("HomeDashboard_MeterAlerts_Hint")]
    [DrillDown(typeof(ReportMeterReadingsHistoryDto))]
    public TableData<MeterAlertRow> MeterAlerts { get; set; } = new();
}

public sealed class ProviderDebtRow
{
    public Guid ProviderId { get; set; }

    [Caption("HomeDashboard_Provider")]
    public string ProviderName { get; set; } = string.Empty;

    [Caption("HomeDashboard_Debt")]
    [Format("N2")]
    public decimal Debt { get; set; }

    [Caption("HomeDashboard_Prepayment")]
    [Format("N2")]
    public decimal Prepayment { get; set; }
}

public sealed class MeterAlertRow
{
    public Guid MeterId { get; set; }

    [Caption("HomeDashboard_Property")]
    public string PropertyName { get; set; } = string.Empty;

    [Caption("HomeDashboard_Meter")]
    public string MeterName { get; set; } = string.Empty;

    [Caption("HomeDashboard_Issue")]
    public string Issue { get; set; } = string.Empty;

    [Caption("HomeDashboard_DueDate")]
    [DateEditor(DateEditorKind.Date)]
    public DateTime? DueDate { get; set; }
}

public class HomeDashboardDtoValidator : AbstractValidator<HomeDashboardDto>
{
    public HomeDashboardDtoValidator()
    {
    }
}
