using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Application.Constants;

[KandraConstant]
[Description("The ISO 4217 code of the base currency (e.g. UAH). Set exchange rates and reports convert other currencies to this currency. Default is UAH.")]
public sealed class BaseCurrencyConstant() : Constant<string>("UAH"), IConstant
{
    public static string Name => "BaseCurrency";

    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.BaseCurrency);
}
