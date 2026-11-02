using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace HomeUtilities.Enums;

[Description("The tariff zone configuration supported by a utility meter: single zone or day-night.")]
public enum MeterZones
{
    [EnumCaption("Enum_MeterZones_Single")]
    Single = 0,

    [EnumCaption("Enum_MeterZones_DayNight")]
    DayNight = 1,
}
