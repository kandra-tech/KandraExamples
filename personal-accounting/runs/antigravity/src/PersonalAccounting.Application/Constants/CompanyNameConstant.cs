using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Application.Constants;

[KandraConstant]
[Description("The company's legal name, printed in the letterhead and as a party of print forms. Empty until set.")]
public sealed class CompanyNameConstant() : Constant<string>(""), IConstant
{
    public static string Name => "CompanyName";

    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.CompanyName);
}
