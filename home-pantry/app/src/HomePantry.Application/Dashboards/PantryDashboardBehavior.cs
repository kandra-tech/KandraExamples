using System.Globalization;
using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Constants;
using Kandra.Application.Abstractions.Dashboards;
using Kandra.Application.Abstractions.Services;
using Kandra.Attributes.Application;
using Kandra.Forms.Dashboards;
using Kandra.Persistence.Abstractions;
using HomePantry.Application.Constants;
using HomePantry.Domain.Commons;
using HomePantry.Forms.Dashboards;
using Microsoft.Extensions.Localization;

namespace HomePantry.Application.Dashboards;

[KandraDashboardBehavior(typeof(PantryDashboardDto))]
public class PantryDashboardBehavior(
    IReportsRepository repository,
    IConstantsService constants,
    IStringLocalizer localizer)
    : IDashboardBehavior<PantryDashboardDto, PantryDashboardResultDto>
{
    public string ObjectName => "PantryDashboard";

    public Task OnNewAsync(PantryDashboardDto filters, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<PantryDashboardResultDto> GetDataAsync(PantryDashboardDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var lookAheadConst = await constants.GetAsync<ExpiringLookAheadDaysConstant>(now, cancellationToken);
        var lookAhead = lookAheadConst?.Value ?? 5;
        if (lookAhead <= 0) lookAhead = 5;

        var stockValue = await repository.GetTotalStockValueAsync(cancellationToken);
        var belowMinCount = await repository.GetBelowMinimumCountAsync(cancellationToken);
        var expiringCount = await repository.GetExpiringBatchesCountAsync(lookAhead, now, cancellationToken);
        var (thisMonthSpending, lastMonthSpending) = await repository.GetMonthlySpendingComparisonAsync(now, cancellationToken);

        var spendingHistory = await repository.GetSpendingHistoryAsync(now, 6, cancellationToken);
        var spendingChart = new CategoryChartData
        {
            Labels = spendingHistory.Select(x => DateTime.ParseExact(x.MonthLabel, "yyyy-MM", CultureInfo.InvariantCulture).ToString("MMM yyyy")).ToList()
        }.AddSeries(localizer["Spending"], spendingHistory.Select(x => x.Amount).ToList());

        var stockByGroup = await repository.GetStockValueByGroupAsync(cancellationToken);
        var groupChart = new CategoryChartData
        {
            Labels = stockByGroup.Select(x => string.IsNullOrEmpty(x.GroupName) ? (localizer["Other"].Value ?? "Other") : x.GroupName).ToList()
        }.AddSeries(localizer["Value"], stockByGroup.Select(x => x.Value).ToList());

        var nextExpiring = await repository.GetNextExpiringBatchesAsync(now, 10, cancellationToken);
        var tableData = new TableData<ExpiringBatchRow>
        {
            Rows = nextExpiring.Select(e => new ExpiringBatchRow
            {
                Product = e.ProductName,
                Location = e.LocationName,
                BestBeforeDate = e.BestBeforeDate,
                DaysLeft = e.DaysLeft
            }).ToList()
        };

        var overdueChoresCount = await repository.GetOverdueChoresCountAsync(now, cancellationToken);
        var batteriesDueCount = await repository.GetBatteriesDueCountAsync(now, cancellationToken);
        var todaySchedule = await repository.GetTodayScheduleAsync(now, cancellationToken);
        var scheduleTable = new TableData<TodayScheduleRow>
        {
            Rows = todaySchedule.Select(s => new TodayScheduleRow
            {
                Kind = s.IsMeal ? localizer["MealPlan"].Value : localizer["Chore"].Value,
                Item = s.ItemName,
                Details = s.IsMeal ? $"{s.Servings:N0}" : (s.DoneByName ?? string.Empty),
                Status = s.IsMeal ? localizer["PlannedMeals"].Value : (s.IsOverdue ? localizer["IsOverdue"].Value : localizer["NextDueDate"].Value)
            }).ToList()
        };

        return new PantryDashboardResultDto
        {
            Filters = filters,
            StockValue = new KpiData
            {
                Value = stockValue,
                Trend = KpiTrend.Neutral
            },
            BelowMinimum = new KpiData
            {
                Value = belowMinCount,
                Trend = KpiTrend.LowerIsBetter
            },
            ExpiringBatches = new KpiData
            {
                Value = expiringCount,
                Trend = KpiTrend.LowerIsBetter
            },
            OverdueChores = new KpiData
            {
                Value = overdueChoresCount,
                Trend = KpiTrend.LowerIsBetter
            },
            BatteriesDue = new KpiData
            {
                Value = batteriesDueCount,
                Trend = KpiTrend.LowerIsBetter
            },
            MonthlySpending = new KpiData
            {
                Value = thisMonthSpending,
                Previous = lastMonthSpending,
                Trend = KpiTrend.Neutral
            },
            SpendingHistory = spendingChart,
            StockByGroup = groupChart,
            NextExpiringBatches = tableData,
            TodaySchedule = scheduleTable
        };
    }
}
