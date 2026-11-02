using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using HomePantry.Enums;

namespace HomePantry.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Locations")]
public abstract class LocationNode : HierarchicalDictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.Location);
}

[KandraDictionaryFolder]
[KandraGeneratedEquality]
public sealed partial class LocationFolder : LocationNode
{
}

[KandraGeneratedEquality]
public sealed partial class Location : LocationNode
{
    public bool IsFreezer { get; set; }
}
