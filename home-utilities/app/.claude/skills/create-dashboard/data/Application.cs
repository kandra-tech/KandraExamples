using System.Globalization;
using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Dashboards;
using Kandra.Attributes.Application;
using Kandra.Forms.Dashboards;
using Kandra.Persistence.Abstractions;
using {{Config}}.Domain.Commons;
using {{Config}}.Forms.Dashboards;
using Microsoft.Extensions.Localization;

namespace {{Config}}.Application.Dashboards;

[KandraDashboardBehavior(typeof({{Name}}DashboardDto))]
public class {{Name}}DashboardBehavior(
    I{{Name}}DashboardRepository repository,   // a repository over the register READERS; never Reg_* tables, never raw SQL
    ICurrentTimeZone timeZone,
    IStringLocalizer localizer,
    TimeProvider? time = null)                 // lets a test fix "now"
    : IDashboardBehavior<{{Name}}DashboardDto, {{Name}}DashboardResultDto>
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    // MUST equal [KandraDashboardForm(Name = ...)]: claims, the nav node and the endpoint all use it.
    public string ObjectName => "{{Name}}Dashboard";

    // The page runs this and then GetDataAsync without waiting for the user: these defaults must pass the validator.
    public Task OnNewAsync({{Name}}DashboardDto filters, CancellationToken cancellationToken)
    {
        filters.Months = 12;
        return Task.CompletedTask;
    }

    public async Task<{{Name}}DashboardResultDto> GetDataAsync({{Name}}DashboardDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        // Calendar buckets in the CALLER's time zone; never group UTC dates.
        var months = DashboardPeriods.Last(filters.Months, DashboardBucketSize.Month, timeZone.TimeZone, now);

        var rows = await repository.Get{{Measure}}(months[0].StartUtc, months[^1].EndUtc.AddTicks(-1), cancellationToken);

        var totals = months.Select(_ => 0m).ToList();
        foreach (var row in rows)
        {
            var index = DashboardPeriods.IndexOf(months, row.Date);
            if (index >= 0) totals[index] += row.Amount;
        }

        return new {{Name}}DashboardResultDto
        {
            Filters = filters,
            {{Measure}}Kpi = new KpiData { Value = totals[^1], Previous = totals.Count > 1 ? totals[^2] : null },
            {{Measure}}ByMonth = new CategoryChartData
            {
                // Labels are already localized/formatted; Keys are for drill-down and may be left out.
                Labels = months.Select(m => m.Start.ToString("MMM yy", CultureInfo.CurrentCulture)).ToList(),
            }
                .AddSeries(localizer["{{Name}}_{{Measure}}"], totals),
        };
    }
}
