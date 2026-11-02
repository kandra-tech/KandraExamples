using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Domain.Registers;
using HomePantry.Domain.Entities.Dictionaries;

namespace HomePantry.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class BatteryLogDimensions : IRegisterDimensions<BatteryLogDimensions>
{
    [Caption("Battery")]
    [Description("The battery that was charged or replaced.")]
    public Guid BatteryId { get; set; }

    [JsonIgnore]
    public Battery Battery { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class BatteryLogDetails : IRegisterDetails<BatteryLogDetails>
{
    [Caption("Replaced")]
    [Description("Whether the battery was replaced rather than recharged.")]
    public bool Replaced { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_BatteryLog")]
[RegisterCaption("BatteryLog")]
public sealed class BatteryLogRecord : DocumentInfoRecord<BatteryLogDimensions, BatteryLogDetails>
{
}

[Description("Periodic information register storing the last time each battery was charged or replaced, valid from the document date.")]
public sealed partial class BatteryLogRegister : DocumentInfoRegister<BatteryLogRecord>
{
}
