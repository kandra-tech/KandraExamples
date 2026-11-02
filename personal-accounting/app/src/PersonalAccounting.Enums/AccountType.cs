using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace PersonalAccounting.Enums;

[Description("Classification of personal financial accounts: cash, bank account, card, or savings.")]
public enum AccountType
{
    [EnumCaption("Enum_AccountType_Cash")]
    Cash = 0,

    [EnumCaption("Enum_AccountType_Bank")]
    Bank = 1,

    [EnumCaption("Enum_AccountType_Card")]
    Card = 2,

    [EnumCaption("Enum_AccountType_Savings")]
    Savings = 3,
}
