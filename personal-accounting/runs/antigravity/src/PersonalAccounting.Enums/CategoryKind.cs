using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace PersonalAccounting.Enums;

[Description("Classification of income/expense categories: whether the category represents income or expense.")]
public enum CategoryKind
{
    [EnumCaption("Enum_CategoryKind_Income")]
    Income = 0,

    [EnumCaption("Enum_CategoryKind_Expense")]
    Expense = 1,
}
