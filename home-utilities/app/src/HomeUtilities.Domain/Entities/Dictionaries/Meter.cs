using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Meters")]
[KandraGeneratedEquality]
public sealed partial class Meter : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomeUtilitiesTypeIds.Meter);

    public Guid PropertyId { get; set; }

    [JsonIgnore]
    public Property? Property { get; set; }

    public Guid ServiceId { get; set; }

    [JsonIgnore]
    public Service? Service { get; set; }

    public string? SerialNumber { get; set; }

    public MeterZones Zones { get; set; }

    public DateTime NextVerificationDate { get; set; }
}
