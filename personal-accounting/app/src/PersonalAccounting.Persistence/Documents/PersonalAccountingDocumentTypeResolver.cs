using Kandra.Domain.Commons;
using PersonalAccounting.Domain.Entities.Documents;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Persistence.Documents;

public sealed class PersonalAccountingDocumentTypeResolver : IDocumentTypeResolver
{
    private static readonly Guid OpeningBalancesTypeId = new(PersonalAccountingTypeIds.OpeningBalances);
    private static readonly Guid SetExchangeRatesTypeId = new(PersonalAccountingTypeIds.SetExchangeRates);
    private static readonly Guid IncomeTypeId = new(PersonalAccountingTypeIds.Income);
    private static readonly Guid ExpenseTypeId = new(PersonalAccountingTypeIds.Expense);
    private static readonly Guid TransferTypeId = new(PersonalAccountingTypeIds.Transfer);
    private static readonly Guid BudgetTypeId = new(PersonalAccountingTypeIds.Budget);

    public Type? Resolve(Guid documentTypeId)
    {
        if (documentTypeId == OpeningBalancesTypeId) return typeof(OpeningBalances);
        if (documentTypeId == SetExchangeRatesTypeId) return typeof(SetExchangeRates);
        if (documentTypeId == IncomeTypeId) return typeof(Income);
        if (documentTypeId == ExpenseTypeId) return typeof(Expense);
        if (documentTypeId == TransferTypeId) return typeof(Transfer);
        if (documentTypeId == BudgetTypeId) return typeof(Budget);
        return null;
    }
}
