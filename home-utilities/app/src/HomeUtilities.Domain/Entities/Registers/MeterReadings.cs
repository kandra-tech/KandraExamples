using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Registers;
using Microsoft.EntityFrameworkCore;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class MeterReadingDimensions : IRegisterDimensions<MeterReadingDimensions>
{
    [Caption("MeterReading_Meter")]
    [Description("The meter whose reading is recorded.")]
    public Guid MeterId { get; set; }
    [JsonIgnore]
    public Meter Meter { get; set; } = null!;

    [Caption("MeterReading_Zone")]
    [Description("The tariff zone: Single, Day, or Night.")]
    public TariffZone Zone { get; set; }
}

[KandraGeneratedEquality]
public sealed partial class MeterReadingDetails : IRegisterDetails<MeterReadingDetails>
{
    [Precision(18, 3)]
    [Caption("MeterReading_Reading")]
    [Format("N3")]
    [Description("Cumulative meter reading value as of Period.")]
    public decimal Reading { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_MeterReadings")]
[RegisterCaption("MeterReadings")]
public sealed class MeterReadingRecord : DocumentInfoRecord<MeterReadingDimensions, MeterReadingDetails>
{
}

[Description("Periodic information register of meter readings per meter and zone valid from document date.")]
public sealed partial class MeterReadingsRegister : DocumentInfoRegister<MeterReadingRecord>
{
}
