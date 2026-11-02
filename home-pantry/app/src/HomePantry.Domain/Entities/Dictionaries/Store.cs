using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using HomePantry.Enums;

namespace HomePantry.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Stores")]
[KandraGeneratedEquality]
public sealed partial class Store : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.Store);
}
