using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Items")]
public abstract class ItemNode : HierarchicalDictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.Item);
}

[KandraDictionaryFolder]
[KandraGeneratedEquality]
public sealed partial class ItemFolder : ItemNode
{
}

[KandraGeneratedEquality]
public sealed partial class Item : ItemNode
{
    public ItemKind Kind { get; set; }
    public string Unit { get; set; } = "pcs";

    [Precision(18, 2)]
    public decimal DefaultPrice { get; set; }

    public int ServiceIntervalKm { get; set; }
    public int ServiceIntervalMonths { get; set; }
}
