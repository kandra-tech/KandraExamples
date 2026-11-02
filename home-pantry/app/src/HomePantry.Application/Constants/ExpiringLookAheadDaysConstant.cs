using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using HomePantry.Enums;

namespace HomePantry.Application.Constants;

[KandraConstant]
[Description("The number of days ahead that 'Expiring soon' checks look ahead. Default is 5 days.")]
public sealed class ExpiringLookAheadDaysConstant() : Constant<int>(5), IConstant
{
    public static string Name => "ExpiringLookAheadDays";

    public static Guid TypeId { get; } = new(HomePantryTypeIds.ExpiringLookAheadDays);
}
