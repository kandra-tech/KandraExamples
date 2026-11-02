using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace FamilyGarage.Enums;

[Description("The kind of vehicle service: maintenance or repair.")]
public enum ServiceKind
{
    [EnumCaption("Enum_ServiceKind_Maintenance")]
    Maintenance = 0,

    [EnumCaption("Enum_ServiceKind_Repair")]
    Repair = 1,
}
