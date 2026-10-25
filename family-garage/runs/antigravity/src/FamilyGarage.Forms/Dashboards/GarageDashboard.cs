using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Dashboards;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Forms.Dashboards;
using FamilyGarage.Forms.Reports;

namespace FamilyGarage.Forms.Dashboards;

public sealed class VehicleDashboardRowDto
{
    [Caption("Vehicle")]
    public string Vehicle { get; set; } = string.Empty;

    [Caption("Odometer")]
    [Format("N0")]
    public int Odometer { get; set; }

    [Caption("TotalCostOfOwnership")]
    [Format("N2")]
    public decimal TotalCostOfOwnership { get; set; }

    [Caption("DebtOwed")]
    [Format("N2")]
    public decimal DebtOwed { get; set; }

    [Caption("NextDueEvent")]
    public string NextDueEvent { get; set; } = string.Empty;
}

public sealed class UpcomingDashboardRowDto
{
    [Caption("Vehicle")]
    public string Vehicle { get; set; } = string.Empty;

    [Caption("Kind")]
    public string Kind { get; set; } = string.Empty;

    [Caption("Description")]
    public string Description { get; set; } = string.Empty;

    [Caption("DueDate")]
    public string DueDate { get; set; } = string.Empty;

    [Caption("RemainingDays")]
    [Format("N0")]
    public int? RemainingDays { get; set; }

    [Caption("RemainingKm")]
    [Format("N0")]
    public int? RemainingKm { get; set; }

    [Caption("IsOverdue")]
    public string IsOverdue { get; set; } = string.Empty;
}

[KandraDashboardForm(Name = "GarageDashboard", ResultDtoType = typeof(GarageDashboardResultDto), ValidatorType = typeof(GarageDashboardValidator))]
[AutoRefresh(60, Intervals = [60, 300, 600])]
[Description("Garage home dashboard displaying monthly costs, total debt, overdue events, fuel consumption, costs trend, and vehicle status.")]
public sealed class GarageDashboardDto : IDashboardFiltersDto
{
}

public sealed class GarageDashboardResultDto : DashboardResult<GarageDashboardDto>
{
    [Caption("ThisMonthCosts")]
    [Hint("ThisMonthCosts_Hint")]
    [KpiTile]
    [Widget(Order = 1, Width = 3, Height = WidgetHeight.Small)]
    public KpiData ThisMonthCosts { get; set; } = new();

    [Caption("TotalDebt")]
    [Hint("TotalDebt_Hint")]
    [KpiTile]
    [Widget(Order = 2, Width = 3, Height = WidgetHeight.Small)]
    public KpiData TotalDebt { get; set; } = new();

    [Caption("OverdueEvents")]
    [Hint("OverdueEvents_Hint")]
    [KpiTile]
    [Widget(Order = 3, Width = 3, Height = WidgetHeight.Small)]
    public KpiData OverdueEvents { get; set; } = new();

    [Caption("AvgFuelConsumption")]
    [Hint("AvgFuelConsumption_Hint")]
    [KpiTile]
    [Widget(Order = 4, Width = 3, Height = WidgetHeight.Small)]
    public KpiData AvgFuelConsumption { get; set; } = new();

    [Caption("CostsPerMonthByCategory")]
    [Hint("CostsPerMonthByCategory_Hint")]
    [BarChart(Stacked = true)]
    [Widget(Order = 5, Width = 12, Height = WidgetHeight.Medium)]
    public CategoryChartData CostsChart { get; set; } = new();

    [Caption("VehiclesStatus")]
    [Hint("VehiclesStatus_Hint")]
    [TableWidget]
    [Widget(Order = 6, Width = 12, Height = WidgetHeight.Large)]
    public TableData<VehicleDashboardRowDto> VehiclesTable { get; set; } = new();

    [Caption("NextDueEvents")]
    [Hint("NextDueEvents_Hint")]
    [TableWidget]
    [DrillDown(typeof(ReportUpcomingDto))]
    [Widget(Order = 7, Width = 12, Height = WidgetHeight.Medium)]
    public TableData<UpcomingDashboardRowDto> NextDueEventsTable { get; set; } = new();
}

public class GarageDashboardValidator : AbstractValidator<GarageDashboardDto>
{
}
