using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Domain.Registers;
using HomePantry.Domain.Entities.Dictionaries;

namespace HomePantry.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class ChoreLogDimensions : IRegisterDimensions<ChoreLogDimensions>
{
    [Caption("Chore")]
    [Description("The chore that was performed.")]
    public Guid ChoreId { get; set; }

    [JsonIgnore]
    public Chore Chore { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class ChoreLogDetails : IRegisterDetails<ChoreLogDetails>
{
    [Caption("DoneBy")]
    [ReferenceOf(typeof(HouseholdMember))]
    [Description("Household member who completed the chore.")]
    public Guid DoneById { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_ChoreLog")]
[RegisterCaption("ChoreLog")]
public sealed class ChoreLogRecord : DocumentInfoRecord<ChoreLogDimensions, ChoreLogDetails>
{
}

[Description("Periodic information register storing the last time each chore was done and by whom, valid from the document date.")]
public sealed partial class ChoreLogRegister : DocumentInfoRegister<ChoreLogRecord>
{
}
