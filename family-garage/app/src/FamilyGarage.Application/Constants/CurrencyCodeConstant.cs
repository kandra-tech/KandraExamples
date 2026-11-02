using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using FamilyGarage.Enums;

namespace FamilyGarage.Application.Constants;

[KandraConstant]
[Description("The currency code shown on forms (default UAH).")]
public sealed class CurrencyCodeConstant() : Constant<string>("UAH"), IConstant
{
    public static string Name => "CurrencyCode";

    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.CurrencyCode);
}
