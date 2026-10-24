using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Categories")]
public abstract class CategoryNode : HierarchicalDictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.Category);

    public CategoryKind Kind { get; set; }
}

[KandraDictionaryFolder]
[KandraGeneratedEquality]
public sealed partial class CategoryFolder : CategoryNode
{
}

[KandraGeneratedEquality]
public sealed partial class Category : CategoryNode
{
}
