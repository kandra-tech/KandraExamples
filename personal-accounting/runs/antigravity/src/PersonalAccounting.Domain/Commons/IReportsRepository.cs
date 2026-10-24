using PersonalAccounting.Domain.Reporting;

namespace PersonalAccounting.Domain.Commons;

public interface IReportsRepository
{
    Task<IReadOnlyList<TransactionJournalRow>> GetTransactionJournalAsync(
        DateTime? fromDate,
        DateTime? toDate,
        IReadOnlyList<Guid>? accountIds,
        IReadOnlyList<Guid>? categoryIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BudgetVsActualRow>> GetBudgetVsActualAsync(
        DateTime month,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SpendingByCategoryRow>> GetSpendingByCategoryAsync(
        DateTime? fromDate,
        DateTime? toDate,
        IReadOnlyList<Guid>? categoryIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountStatementRow>> GetAccountStatementAsync(
        Guid accountId,
        DateTime? fromDate,
        DateTime? toDate,
        string openingLabel,
        string closingLabel,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExchangeRateRow>> GetExchangeRatesAsync(
        DateTime dateAsOf,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountBalanceSummary>> GetAccountBalancesSummaryAsync(
        DateTime asOf,
        string baseCurrencyCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MonthlyTurnoverSummary>> GetMonthlyTurnoversAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryExpenseSummary>> GetCategoryExpensesAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken);

    Task<(decimal planned, decimal actual)> GetBudgetSpentAsync(
        DateTime month,
        CancellationToken cancellationToken);
}
