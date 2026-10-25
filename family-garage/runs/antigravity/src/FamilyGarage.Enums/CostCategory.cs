using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace FamilyGarage.Enums;

[Description("The category of cost incurred for a vehicle.")]
public enum CostCategory
{
    [EnumCaption("Enum_CostCategory_Purchase")]
    Purchase = 0,

    [EnumCaption("Enum_CostCategory_InterestAndFees")]
    InterestAndFees = 1,

    [EnumCaption("Enum_CostCategory_Fuel")]
    Fuel = 2,

    [EnumCaption("Enum_CostCategory_Maintenance")]
    Maintenance = 3,

    [EnumCaption("Enum_CostCategory_Repair")]
    Repair = 4,

    [EnumCaption("Enum_CostCategory_Insurance")]
    Insurance = 5,

    [EnumCaption("Enum_CostCategory_Tax")]
    Tax = 6,

    [EnumCaption("Enum_CostCategory_Other")]
    Other = 7,
}
