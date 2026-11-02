using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Registers;
using FamilyGarage.Domain.Entities.Dictionaries;

namespace FamilyGarage.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class LastDoneDimensions : IRegisterDimensions<LastDoneDimensions>
{
    [Caption("Vehicle")]
    [Description("The vehicle for which the work item was performed.")]
    public Guid VehicleId { get; set; }

    [JsonIgnore]
    public Vehicle Vehicle { get; set; } = null!;

    [Caption("Item")]
    [Description("The work item that was performed.")]
    public Guid ItemId { get; set; }

    [JsonIgnore]
    public ItemNode Item { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class LastDoneDetails : IRegisterDetails<LastDoneDetails>
{
    [Caption("Odometer")]
    [Format("N0")]
    [Description("Odometer reading when the work item was last performed.")]
    public int Odometer { get; set; }

    [Caption("Date")]
    [Description("Date when the work item was last performed.")]
    public DateTime Date { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_LastDone")]
[RegisterCaption("LastDone")]
public sealed class LastDoneRecord : DocumentInfoRecord<LastDoneDimensions, LastDoneDetails>
{
}

[Description("Periodic information register tracking the date and odometer of the last time a work item was performed on a vehicle.")]
public sealed partial class LastDoneRegister : DocumentInfoRegister<LastDoneRecord>
{
}
