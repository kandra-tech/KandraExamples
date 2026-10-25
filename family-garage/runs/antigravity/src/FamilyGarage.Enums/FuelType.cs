using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace FamilyGarage.Enums;

[Description("The fuel or propulsion type of a vehicle.")]
public enum FuelType
{
    [EnumCaption("Enum_FuelType_Petrol")]
    Petrol = 0,

    [EnumCaption("Enum_FuelType_Diesel")]
    Diesel = 1,

    [EnumCaption("Enum_FuelType_LPG")]
    LPG = 2,

    [EnumCaption("Enum_FuelType_Electric")]
    Electric = 3,

    [EnumCaption("Enum_FuelType_Hybrid")]
    Hybrid = 4,
}
