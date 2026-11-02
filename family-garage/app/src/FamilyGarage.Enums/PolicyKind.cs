using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace FamilyGarage.Enums;

[Description("The kind of vehicle policy: compulsory insurance, comprehensive insurance, tax, or other.")]
public enum PolicyKind
{
    [EnumCaption("Enum_PolicyKind_CompulsoryInsurance")]
    CompulsoryInsurance = 0,

    [EnumCaption("Enum_PolicyKind_ComprehensiveInsurance")]
    ComprehensiveInsurance = 1,

    [EnumCaption("Enum_PolicyKind_Tax")]
    Tax = 2,

    [EnumCaption("Enum_PolicyKind_Other")]
    Other = 3,
}
