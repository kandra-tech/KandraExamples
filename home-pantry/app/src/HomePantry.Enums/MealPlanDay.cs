using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace HomePantry.Enums;

[Description("Day of the week for meal planning.")]
public enum MealPlanDay
{
    [EnumCaption("Enum_MealPlanDay_Monday")]
    Monday = 1,

    [EnumCaption("Enum_MealPlanDay_Tuesday")]
    Tuesday = 2,

    [EnumCaption("Enum_MealPlanDay_Wednesday")]
    Wednesday = 3,

    [EnumCaption("Enum_MealPlanDay_Thursday")]
    Thursday = 4,

    [EnumCaption("Enum_MealPlanDay_Friday")]
    Friday = 5,

    [EnumCaption("Enum_MealPlanDay_Saturday")]
    Saturday = 6,

    [EnumCaption("Enum_MealPlanDay_Sunday")]
    Sunday = 7
}
