namespace PersonalAccounting.Domain.Reporting;

public sealed class TransactionJournalRow
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public string DocumentCode { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = "";
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal? Inflow { get; set; }
    public decimal? Outflow { get; set; }
    public string? Comment { get; set; }
}

public sealed class BudgetVsActualRow
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal PercentUsed { get; set; }
    public bool IsOverspent { get; set; }
}

public sealed class SpendingByCategoryRow
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = "";
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal AmountInBase { get; set; }
}

public sealed class AccountStatementRow
{
    public Guid Id { get; set; }
    public DateTime? Date { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public string? DocumentCode { get; set; }
    public string? DocumentType { get; set; }
    public string Description { get; set; } = "";
    public decimal? Inflow { get; set; }
    public decimal? Outflow { get; set; }
    public decimal RunningBalance { get; set; }
}

public sealed class ExchangeRateRow
{
    public Guid Id { get; set; }
    public Guid CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = "";
    public string CurrencyName { get; set; } = "";
    public decimal Rate { get; set; }
    public DateTime ValidFrom { get; set; }
}

public sealed class AccountBalanceSummary
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = "";
    public PersonalAccounting.Enums.AccountType AccountType { get; set; }
    public Guid CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal Balance { get; set; }
    public decimal BalanceInBase { get; set; }
    public bool HasRate { get; set; }
}

public sealed class MonthlyTurnoverSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal IncomeInBase { get; set; }
    public decimal ExpenseInBase { get; set; }
}

public sealed class CategoryExpenseSummary
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal AmountInBase { get; set; }
}
