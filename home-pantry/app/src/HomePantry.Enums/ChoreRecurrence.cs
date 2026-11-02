using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace HomePantry.Enums;

[Description("Recurrence frequency for a household chore.")]
public enum ChoreRecurrence
{
    [EnumCaption("Enum_ChoreRecurrence_Daily")]
    Daily = 0,

    [EnumCaption("Enum_ChoreRecurrence_Weekly")]
    Weekly = 1,

    [EnumCaption("Enum_ChoreRecurrence_Monthly")]
    Monthly = 2,

    [EnumCaption("Enum_ChoreRecurrence_EveryNDays")]
    EveryNDays = 3
}
