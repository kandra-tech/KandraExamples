using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace HomeUtilities.Enums;

[Description("The kind of utility provider (electricity, gas, water, heating, internet, housing management, or other).")]
public enum ProviderKind
{
    [EnumCaption("Enum_ProviderKind_Electricity")]
    Electricity = 0,

    [EnumCaption("Enum_ProviderKind_Gas")]
    Gas = 1,

    [EnumCaption("Enum_ProviderKind_Water")]
    Water = 2,

    [EnumCaption("Enum_ProviderKind_Heating")]
    Heating = 3,

    [EnumCaption("Enum_ProviderKind_Internet")]
    Internet = 4,

    [EnumCaption("Enum_ProviderKind_HousingManagement")]
    HousingManagement = 5,

    [EnumCaption("Enum_ProviderKind_Other")]
    Other = 6,
}
