using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using FamilyGarage.Enums;

namespace FamilyGarage.Application.Constants;

[KandraConstant]
[Description("The number of days ahead that Upcoming events report and reminders look (default 30).")]
public sealed class UpcomingDaysAheadConstant() : Constant<int>(30), IConstant
{
    public static string Name => "UpcomingDaysAhead";

    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.UpcomingDaysAhead);
}
