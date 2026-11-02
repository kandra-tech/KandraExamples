using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace HomeUtilities.Enums;

[Description("The specific tariff zone for a meter reading or rate: single, day, or night.")]
public enum TariffZone
{
    [EnumCaption("Enum_TariffZone_Single")]
    Single = 0,

    [EnumCaption("Enum_TariffZone_Day")]
    Day = 1,

    [EnumCaption("Enum_TariffZone_Night")]
    Night = 2,
}
