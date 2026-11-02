using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace FamilyGarage.Enums;

[Description("The role or kind of service/goods provider.")]
public enum ProviderKind
{
    [EnumCaption("Enum_ProviderKind_ServiceStation")]
    ServiceStation = 0,

    [EnumCaption("Enum_ProviderKind_Insurer")]
    Insurer = 1,

    [EnumCaption("Enum_ProviderKind_FuelStation")]
    FuelStation = 2,

    [EnumCaption("Enum_ProviderKind_Dealer")]
    Dealer = 3,

    [EnumCaption("Enum_ProviderKind_Bank")]
    Bank = 4,

    [EnumCaption("Enum_ProviderKind_LeasingCompany")]
    LeasingCompany = 5,

    [EnumCaption("Enum_ProviderKind_Other")]
    Other = 6,
}
