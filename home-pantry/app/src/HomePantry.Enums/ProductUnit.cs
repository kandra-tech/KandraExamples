using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace HomePantry.Enums;

[Description("Units of measurement for products in the pantry.")]
public enum ProductUnit
{
    [EnumCaption("Enum_ProductUnit_Pcs")]
    Pcs = 0,

    [EnumCaption("Enum_ProductUnit_G")]
    G = 1,

    [EnumCaption("Enum_ProductUnit_Kg")]
    Kg = 2,

    [EnumCaption("Enum_ProductUnit_Ml")]
    Ml = 3,

    [EnumCaption("Enum_ProductUnit_L")]
    L = 4,

    [EnumCaption("Enum_ProductUnit_Pack")]
    Pack = 5,
}
