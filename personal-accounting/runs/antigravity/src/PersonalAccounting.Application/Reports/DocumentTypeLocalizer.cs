using Microsoft.Extensions.Localization;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Application.Reports;

internal static class DocumentTypeLocalizer
{
    private static readonly Guid IncomeId = new(PersonalAccountingTypeIds.Income);
    private static readonly Guid ExpenseId = new(PersonalAccountingTypeIds.Expense);
    private static readonly Guid TransferId = new(PersonalAccountingTypeIds.Transfer);
    private static readonly Guid OpeningBalancesId = new(PersonalAccountingTypeIds.OpeningBalances);
    private static readonly Guid SetExchangeRatesId = new(PersonalAccountingTypeIds.SetExchangeRates);
    private static readonly Guid BudgetId = new(PersonalAccountingTypeIds.Budget);

    public static string ResolveName(Guid? typeId, IStringLocalizer localizer)
    {
        if (!typeId.HasValue) return "";
        var id = typeId.Value;
        if (id == IncomeId) return localizer["Income"].Value ?? "Income";
        if (id == ExpenseId) return localizer["Expense"].Value ?? "Expense";
        if (id == TransferId) return localizer["Transfer"].Value ?? "Transfer";
        if (id == OpeningBalancesId) return localizer["OpeningBalances"].Value ?? "Opening balances";
        if (id == SetExchangeRatesId) return localizer["SetExchangeRates"].Value ?? "Set exchange rates";
        if (id == BudgetId) return localizer["Budget"].Value ?? "Budget";
        return "";
    }
}
