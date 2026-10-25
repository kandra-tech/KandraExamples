using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace FamilyGarage.Enums;

[Description("The method of payment for a vehicle purchase: cash, bank loan, or leasing.")]
public enum VehiclePurchasePaymentKind
{
    [EnumCaption("Enum_VehiclePurchasePaymentKind_Cash")]
    Cash = 0,

    [EnumCaption("Enum_VehiclePurchasePaymentKind_BankLoan")]
    BankLoan = 1,

    [EnumCaption("Enum_VehiclePurchasePaymentKind_Leasing")]
    Leasing = 2,
}
