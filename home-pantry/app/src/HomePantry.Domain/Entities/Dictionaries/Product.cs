using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using HomePantry.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Products")]
public abstract class ProductNode : HierarchicalDictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.Product);
}

[KandraDictionaryFolder]
[KandraGeneratedEquality]
public sealed partial class ProductFolder : ProductNode
{
}

[KandraGeneratedEquality]
public sealed partial class Product : ProductNode
{
    public ProductUnit Unit { get; set; }

    public string? Barcode { get; set; }

    [Precision(14, 3)]
    public decimal MinimumStock { get; set; }

    public Guid? DefaultLocationId { get; set; }

    [JsonIgnore]
    public Location? DefaultLocation { get; set; }

    public int DefaultShelfLifeDays { get; set; }
}
