using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using HomeUtilities.Enums;

namespace HomeUtilities.Application.Constants;

[KandraConstant]
[Description("The day of the month by which meter readings are expected (default 25).")]
public sealed class ExpectedReadingsDayConstant() : Constant<int>(25), IConstant
{
    public static string Name => "ExpectedReadingsDay";

    public static Guid TypeId { get; } = new(HomeUtilitiesTypeIds.ExpectedReadingsDay);
}
