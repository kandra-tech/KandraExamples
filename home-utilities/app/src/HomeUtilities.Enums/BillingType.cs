using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace HomeUtilities.Enums;

[Description("The billing method for a utility service: metered, fixed per month, or per square metre.")]
public enum BillingType
{
    [EnumCaption("Enum_BillingType_Metered")]
    Metered = 0,

    [EnumCaption("Enum_BillingType_FixedPerMonth")]
    FixedPerMonth = 1,

    [EnumCaption("Enum_BillingType_PerSquareMetre")]
    PerSquareMetre = 2,

    [EnumCaption("Enum_BillingType_PerPerson")]
    PerPerson = 3,
}
