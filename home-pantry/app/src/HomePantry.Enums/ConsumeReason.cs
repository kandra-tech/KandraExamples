using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace HomePantry.Enums;

[Description("Reason for consuming or discarding pantry items.")]
public enum ConsumeReason
{
    [EnumCaption("Enum_ConsumeReason_Eaten")]
    Eaten = 0,

    [EnumCaption("Enum_ConsumeReason_Spoiled")]
    Spoiled = 1,

    [EnumCaption("Enum_ConsumeReason_UsedUp")]
    UsedUp = 2,

    [EnumCaption("Enum_ConsumeReason_Other")]
    Other = 3,
}
