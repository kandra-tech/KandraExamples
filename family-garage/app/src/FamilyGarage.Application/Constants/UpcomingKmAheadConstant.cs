using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using FamilyGarage.Enums;

namespace FamilyGarage.Application.Constants;

[KandraConstant]
[Description("The number of kilometres ahead that Upcoming events report looks (default 1000).")]
public sealed class UpcomingKmAheadConstant() : Constant<int>(1000), IConstant
{
    public static string Name => "UpcomingKmAhead";

    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.UpcomingKmAhead);
}
