using HomePantry.Domain.Entities.ReportingModels;

namespace HomePantry.Domain.Commons;

/// <summary>
/// The report queries. A method that works with calendar days takes the user's time zone: <c>asOf</c> is a UTC instant,
/// and its day is the local one (<see cref="LocalCalendar"/>). Dates it returns are local midnight, as the client shows them.
/// </summary>
public interface IReportsRepository
{
    Task<IReadOnlyList<StockOverviewLine>> GetStockOverviewAsync(Guid? locationId, Guid? productGroupId, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExpiringLine>> GetExpiringAsync(int lookAheadDays, DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<IReadOnlyList<ShoppingNeedsLine>> GetShoppingNeedsAsync(Guid? recipeId, decimal? servings, DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<IReadOnlyList<ShoppingNeedsLine>> GetShoppingNeedsAsync(Guid? recipeId, decimal? servings, DateTime asOf, DateTime? mealPlanWeek, Guid? mealPlanId, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<IReadOnlyList<CanICookItLine>> GetCanICookItAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SpendingLine>> GetSpendingAsync(DateTime? from, DateTime? to, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChoresReportLine>> GetChoresAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<IReadOnlyList<BatteriesReportLine>> GetBatteriesAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<IReadOnlyList<EquipmentReportLine>> GetEquipmentWarrantiesAsync(int withinDays, DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<IReadOnlyList<WeekPlanReportLine>> GetWeekPlanAsync(DateTime weekStart, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<IReadOnlyList<CalendarReportLine>> GetCalendarAsync(DateTime from, DateTime to, TimeZoneInfo? zone, CancellationToken cancellationToken);

    // Dashboard queries
    Task<decimal> GetTotalStockValueAsync(CancellationToken cancellationToken);
    Task<int> GetBelowMinimumCountAsync(CancellationToken cancellationToken);
    Task<int> GetExpiringBatchesCountAsync(int lookAheadDays, DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken);
    /// <summary>The purchased amount in each period, from <c>StartUtc</c> (inclusive) to <c>EndUtc</c> (exclusive).</summary>
    Task<IReadOnlyList<decimal>> GetSpendingPerPeriodAsync(IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> periods, CancellationToken cancellationToken);
    Task<IReadOnlyList<(string GroupName, decimal Value)>> GetStockValueByGroupAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ExpiringLine>> GetNextExpiringBatchesAsync(DateTime asOf, int count, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<int> GetOverdueChoresCountAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<int> GetBatteriesDueCountAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken);
    Task<IReadOnlyList<TodayScheduleItem>> GetTodayScheduleAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken cancellationToken);
}
