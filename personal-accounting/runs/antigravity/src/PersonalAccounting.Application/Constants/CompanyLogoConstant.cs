using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Application.Constants;

[KandraConstant]
[Description("The blob reference id of the company logo printed in the letterhead of print forms; null prints no " +
    "logo. The value is history-aware (a reprint of an old document shows the logo that was valid on its " +
    "date) and the blob must stay attached, so do not edit it by hand in the constants page: use the " +
    "CompanySettings data processor, which uploads the image, keeps every earlier logo attached and writes " +
    "this constant.")]
public sealed class CompanyLogoConstant() : Constant<Guid?>(null), IConstant
{
    public static string Name => "CompanyLogo";

    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.CompanyLogo);
}
