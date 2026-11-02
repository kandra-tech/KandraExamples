using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using HomePantry.Enums;

namespace HomePantry.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Equipment")]
[KandraGeneratedEquality]
public sealed partial class Equipment : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.Equipment);

    public Guid LocationId { get; set; }

    [JsonIgnore]
    public LocationNode? Location { get; set; }

    public DateTime? BoughtOn { get; set; }

    public DateTime? WarrantyUntil { get; set; }

    public Guid? ManualFileId { get; set; }

    public Guid? PhotoFileId { get; set; }
}
