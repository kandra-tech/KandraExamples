using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Dashboards;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Forms.Dashboards;

namespace {{Config}}.Forms.Dashboards;

// The filters Dto. Name is the permission object ("O|{{Name}}Dashboard|View"), the policy stem and the page's caption key.
// ValidatorType is mandatory. [AutoRefresh] is optional (delete the line for a dashboard that refreshes only on demand).
/// <summary>{{What the dashboard shows, in one sentence: which registers it reads and what is netted.}}</summary>
[KandraDashboardForm(Name = "{{Name}}Dashboard", ResultDtoType = typeof({{Name}}DashboardResultDto), ValidatorType = typeof({{Name}}DashboardDtoValidator))]
[AutoRefresh(60, Intervals = [30, 60, 300, 900])]
public sealed class {{Name}}DashboardDto : IDashboardFiltersDto
{
    // OPTIONAL - a dashboard may have no filters at all (delete the property and the validator rule).
    // Every property needs a default in OnNewAsync that passes the validator: the page runs on open.
    /// <summary>How many calendar months the bar chart covers, ending with the current one.</summary>
    [Caption("{{Name}}_Months")]
    public int Months { get; set; }
}

// One property per widget. The widget attribute must match the property's shape (KANDRAUI033).
public sealed class {{Name}}DashboardResultDto : DashboardResult<{{Name}}DashboardDto>
{
    [KpiTile, Widget(Order = 10, Width = 4, Height = WidgetHeight.Small)]
    [Caption("{{Name}}_{{Measure}}Kpi")]
    [Hint("{{Name}}_{{Measure}}Kpi_Hint")]   // OPTIONAL - a resource key; the text is the tooltip
    [Format("N0")]
    public KpiData {{Measure}}Kpi { get; set; } = new();

    [BarChart, Widget(Order = 20, Width = 8)]
    [Caption("{{Name}}_{{Measure}}ByMonth")]
    [Hint("{{Name}}_{{Measure}}ByMonth_Hint")]
    [Format("N0")]
    public CategoryChartData {{Measure}}ByMonth { get; set; } = new();
}

public class {{Name}}DashboardDtoValidator : AbstractValidator<{{Name}}DashboardDto>
{
    public {{Name}}DashboardDtoValidator()
    {
        RuleFor(x => x.Months).InclusiveBetween(3, 24);
    }
}
