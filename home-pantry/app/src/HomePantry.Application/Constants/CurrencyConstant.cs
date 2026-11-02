using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using HomePantry.Enums;

namespace HomePantry.Application.Constants;

[KandraConstant]
[Description("The currency code shown on forms and reports. Default is UAH.")]
public sealed class CurrencyConstant() : Constant<string>("UAH"), IConstant
{
    public static string Name => "Currency";

    public static Guid TypeId { get; } = new(HomePantryTypeIds.Currency);
}
