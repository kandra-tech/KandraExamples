using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using HomeUtilities.Enums;

namespace HomeUtilities.Application.Constants;

[KandraConstant]
[Description("The currency code shown on forms and printouts (default UAH).")]
public sealed class CurrencyCodeConstant() : Constant<string>("UAH"), IConstant
{
    public static string Name => "CurrencyCode";

    public static Guid TypeId { get; } = new(HomeUtilitiesTypeIds.CurrencyCode);
}
