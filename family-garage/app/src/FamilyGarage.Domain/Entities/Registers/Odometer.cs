using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Registers;
using FamilyGarage.Domain.Entities.Dictionaries;

namespace FamilyGarage.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class OdometerDimensions : IRegisterDimensions<OdometerDimensions>
{
    [Caption("Vehicle")]
    [Description("The vehicle whose odometer was recorded.")]
    public Guid VehicleId { get; set; }

    [JsonIgnore]
    public Vehicle Vehicle { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class OdometerDetails : IRegisterDetails<OdometerDetails>
{
    [Caption("Kilometres")]
    [Format("N0")]
    [Description("Odometer reading in kilometres, valid from the document date until a newer reading.")]
    public int Kilometres { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_Odometer")]
[RegisterCaption("Odometer")]
public sealed class OdometerRecord : DocumentInfoRecord<OdometerDimensions, OdometerDetails>
{
}

[Description("Periodic information register of vehicle odometer readings in kilometres valid from the document date.")]
public sealed partial class OdometerRegister : DocumentInfoRegister<OdometerRecord>
{
}
