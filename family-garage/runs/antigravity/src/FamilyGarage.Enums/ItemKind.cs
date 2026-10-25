using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace FamilyGarage.Enums;

[Description("The classification of an item as a part, work (labor) or consumable.")]
public enum ItemKind
{
    [EnumCaption("Enum_ItemKind_Part")]
    Part = 0,

    [EnumCaption("Enum_ItemKind_Work")]
    Work = 1,

    [EnumCaption("Enum_ItemKind_Consumable")]
    Consumable = 2,
}
