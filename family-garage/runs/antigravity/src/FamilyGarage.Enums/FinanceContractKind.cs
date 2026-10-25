using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace FamilyGarage.Enums;

[Description("The kind of finance contract: bank loan or leasing.")]
public enum FinanceContractKind
{
    [EnumCaption("Enum_FinanceContractKind_BankLoan")]
    BankLoan = 0,

    [EnumCaption("Enum_FinanceContractKind_Leasing")]
    Leasing = 1,
}
