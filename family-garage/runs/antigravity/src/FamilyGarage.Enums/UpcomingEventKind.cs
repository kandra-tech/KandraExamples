using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace FamilyGarage.Enums;

[Description("The kind of a due event in the Upcoming report: a service job, a policy that ends, or a finance payment.")]
public enum UpcomingEventKind
{
    [EnumCaption("Enum_UpcomingEventKind_Maintenance")]
    Maintenance = 1,

    [EnumCaption("Enum_UpcomingEventKind_Policy")]
    Policy = 2,

    [EnumCaption("Enum_UpcomingEventKind_Payment")]
    Payment = 3
}
