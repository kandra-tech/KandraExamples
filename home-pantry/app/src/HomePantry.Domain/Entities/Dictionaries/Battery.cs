using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using HomePantry.Enums;

namespace HomePantry.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Batteries")]
[KandraGeneratedEquality]
public sealed partial class Battery : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.Battery);

    public string UsedIn { get; set; } = string.Empty;

    public int ChargeIntervalDays { get; set; }

    public BatteryKind BatteryKind { get; set; } = BatteryKind.Rechargeable;
}
