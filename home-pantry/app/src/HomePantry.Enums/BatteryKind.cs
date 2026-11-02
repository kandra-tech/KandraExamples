using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace HomePantry.Enums;

[Description("Kind of battery, either rechargeable or disposable.")]
public enum BatteryKind
{
    [EnumCaption("Enum_BatteryKind_Rechargeable")]
    Rechargeable = 0,

    [EnumCaption("Enum_BatteryKind_Disposable")]
    Disposable = 1
}
