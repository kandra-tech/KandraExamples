using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Domain.Registers;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;

namespace FamilyGarage.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class PolicyValidityDimensions : IRegisterDimensions<PolicyValidityDimensions>
{
    [Caption("Vehicle")]
    [Description("The vehicle covered by the policy.")]
    public Guid VehicleId { get; set; }

    [JsonIgnore]
    public Vehicle Vehicle { get; set; } = null!;

    [Caption("PolicyKind")]
    [Description("The kind of policy (compulsory insurance, comprehensive insurance, tax, or other).")]
    public PolicyKind PolicyKind { get; set; }
}

[KandraGeneratedEquality]
public sealed partial class PolicyValidityDetails : IRegisterDetails<PolicyValidityDetails>
{
    [Caption("ValidTo")]
    [Description("The date until which the policy is currently valid.")]
    public DateTime ValidTo { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_PolicyValidity")]
[RegisterCaption("PolicyValidity")]
public sealed class PolicyValidityRecord : DocumentInfoRecord<PolicyValidityDimensions, PolicyValidityDetails>
{
}

[Description("Periodic information register tracking the expiration date of policies per vehicle and policy kind.")]
public sealed partial class PolicyValidityRegister : DocumentInfoRegister<PolicyValidityRecord>
{
}
