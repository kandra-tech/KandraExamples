using HomePantry.Domain.Entities.ReportingModels;

namespace HomePantry.Domain.Commons;

public interface IReportsRepository
{
    Task<IReadOnlyList<StockOverviewLine>> GetStockOverviewAsync(Guid? locationId, Guid? productGroupId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExpiringLine>> GetExpiringAsync(int lookAheadDays, DateTime asOf, CancellationToken cancellationToken);
    Task<IReadOnlyList<ShoppingNeedsLine>> GetShoppingNeedsAsync(Guid? recipeId, decimal? servings, DateTime asOf, CancellationToken cancellationToken);
    Task<IReadOnlyList<CanICookItLine>> GetCanICookItAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SpendingLine>> GetSpendingAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken);

    // Dashboard queries
    Task<decimal> GetTotalStockValueAsync(CancellationToken cancellationToken);
    Task<int> GetBelowMinimumCountAsync(CancellationToken cancellationToken);
    Task<int> GetExpiringBatchesCountAsync(int lookAheadDays, DateTime asOf, CancellationToken cancellationToken);
    Task<(decimal thisMonth, decimal lastMonth)> GetMonthlySpendingComparisonAsync(DateTime asOf, CancellationToken cancellationToken);
    Task<IReadOnlyList<(string MonthKey, string MonthLabel, decimal Amount)>> GetSpendingHistoryAsync(DateTime asOf, int months, CancellationToken cancellationToken);
    Task<IReadOnlyList<(string GroupName, decimal Value)>> GetStockValueByGroupAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ExpiringLine>> GetNextExpiringBatchesAsync(DateTime asOf, int count, CancellationToken cancellationToken);
}
