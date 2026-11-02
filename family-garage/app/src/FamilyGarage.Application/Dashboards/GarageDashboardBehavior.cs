using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Constants;
using Kandra.Application.Abstractions.Services;
using Kandra.Attributes.Application;
using Kandra.Forms.Dashboards;
using Kandra.Localization;
using Kandra.Persistence.Abstractions;
using FamilyGarage.Application.Constants;
using FamilyGarage.Application.Reports;
using FamilyGarage.Domain.Commons;
using FamilyGarage.Domain.Reporting;
using FamilyGarage.Enums;
using FamilyGarage.Forms.Dashboards;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Application.Dashboards;

[KandraDashboardBehavior(typeof(GarageDashboardDto))]
public class GarageDashboardBehavior(
    IReportsRepository repository,
    IConstantsService constants,
    ICurrentTimeZone timeZone,
    IStringLocalizer localizer,
    TimeProvider? time = null)
    : IDashboardBehavior<GarageDashboardDto, GarageDashboardResultDto>
{
    public string ObjectName => "GarageDashboard";

    public Task OnNewAsync(GarageDashboardDto filters, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<GarageDashboardResultDto> GetDataAsync(GarageDashboardDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        // Months and "today" are the user's (a document dated the 1st at local midnight is still the previous month in UTC
        // for a user east of UTC), so every bound is a local date turned into a UTC instant.
        var zone = timeZone.TimeZone;
        var utcNow = (time ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        var today = LocalCalendar.LocalDate(utcNow, zone);
        var thisMonth = LocalCalendar.MonthStart(today);
        var nextMonthStartUtc = LocalCalendar.StartUtc(thisMonth.AddMonths(1), zone);
        var thisMonthStartUtc = LocalCalendar.StartUtc(thisMonth, zone);
        var lastMonthStartUtc = LocalCalendar.StartUtc(thisMonth.AddMonths(-1), zone);

        var currency = (await constants.GetAsync<CurrencyCodeConstant>(utcNow, cancellationToken)).Value;
        var lookAheadDays = (await constants.GetAsync<UpcomingDaysAheadConstant>(utcNow, cancellationToken)).Value;
        var lookAheadKm = (await constants.GetAsync<UpcomingKmAheadConstant>(utcNow, cancellationToken)).Value;

        var thisMonthCosts = await repository.GetCostsForPeriodAsync(thisMonthStartUtc, nextMonthStartUtc, cancellationToken);
        var lastMonthCosts = await repository.GetCostsForPeriodAsync(lastMonthStartUtc, thisMonthStartUtc, cancellationToken);

        var totalDebt = await repository.GetTotalDebtAsync(cancellationToken);

        // The same events as the Upcoming report with its default look-ahead, so the overdue count and the "next five"
        // match what the report shows when the tile or the table is clicked.
        var upcoming = await repository.GetUpcomingItemsAsync(lookAheadDays, lookAheadKm, null, today, zone, cancellationToken);
        var overdueCount = upcoming.Count(u => u.IsOverdue);

        var tomorrowUtc = LocalCalendar.StartUtc(today.AddDays(1), zone);
        var threeMonthsAgoUtc = LocalCalendar.StartUtc(today.AddMonths(-3), zone);
        var avgFuel = await repository.GetAverageFuelConsumptionAsync(threeMonthsAgoUtc, tomorrowUtc, cancellationToken);

        // Chart: the last twelve local months by category.
        var firstMonth = thisMonth.AddMonths(-11);
        var monthlyCosts = await repository.GetMonthlyCostsByCategoryAsync(
            LocalCalendar.StartUtc(firstMonth, zone), nextMonthStartUtc, zone, cancellationToken);

        var months = Enumerable.Range(0, 12).Select(i => firstMonth.AddMonths(i)).ToList();
        var chart = new CategoryChartData();
        foreach (var m in months)
        {
            chart.AddLabel(m.ToString("MMM yy", System.Globalization.CultureInfo.CurrentCulture), m.ToString("yyyy-MM"));
        }

        foreach (var cat in Enum.GetValues<CostCategory>())
        {
            var values = months.Select(m =>
                monthlyCosts.TryGetValue(m.ToString("yyyy-MM"), out var catDict) && catDict.TryGetValue(cat, out var val) ? val : 0m).ToList();
            chart.AddSeries(cat.GetCaption(localizer), values);
        }

        // Table 1: one row per vehicle.
        var vStatus = await repository.GetVehicleDashboardStatusAsync(today, zone, cancellationToken);
        var vRows = vStatus.Select(v => new VehicleDashboardRowDto
        {
            Vehicle = v.VehicleName,
            Odometer = v.Odometer,
            TotalCostOfOwnership = v.Tco,
            DebtOwed = v.DebtOwed,
            NextDueEvent = v.NextDue is { } next ? UpcomingText.NextDue(next, localizer) : string.Empty
        }).ToList();

        // Table 2: the next five due events.
        var nextDueRows = upcoming.Take(5).Select(u => new UpcomingDashboardRowDto
        {
            Vehicle = u.VehicleName,
            Kind = UpcomingText.Kind(u, localizer),
            Description = UpcomingText.Description(u, localizer),
            DueDate = u.DueDate?.ToString("d", System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty,
            RemainingDays = u.RemainingDays,
            RemainingKm = u.RemainingKm,
            IsOverdue = localizer[u.IsOverdue ? "Common_Yes" : "Common_No"].Value
        }).ToList();

        return new GarageDashboardResultDto
        {
            Filters = filters,
            ThisMonthCosts = new KpiData
            {
                Value = thisMonthCosts,
                Previous = lastMonthCosts,
                Unit = currency,
                Trend = KpiTrend.LowerIsBetter
            },
            TotalDebt = new KpiData
            {
                Value = totalDebt,
                Unit = currency,
                Trend = KpiTrend.Neutral
            },
            OverdueEvents = new KpiData
            {
                Value = overdueCount,
                Trend = KpiTrend.LowerIsBetter
            },
            AvgFuelConsumption = new KpiData
            {
                Value = avgFuel,
                Unit = localizer["Unit_LitresPer100Km"].Value,
                Trend = KpiTrend.LowerIsBetter
            },
            CostsChart = chart,
            VehiclesTable = new TableData<VehicleDashboardRowDto> { Rows = vRows },
            NextDueEventsTable = new TableData<UpcomingDashboardRowDto> { Rows = nextDueRows }
        };
    }
}
