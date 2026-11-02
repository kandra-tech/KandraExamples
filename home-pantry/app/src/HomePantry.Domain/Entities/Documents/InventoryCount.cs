using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_InventoryCounts")]
[KandraGeneratedEquality]
public sealed partial class InventoryCount : DocumentBase, IEntityWithRows<InventoryCount>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.InventoryCount);

    public InventoryCount() : base(TypeId) { }

    public Guid LocationId { get; set; }

    [JsonIgnore]
    public LocationNode Location { get; set; } = null!;

    [KandraTablePart(TableName = "Doc_InventoryCountLines")]
    public IList<InventoryCountLine> Lines { get; set; } = [];
}

[KandraGeneratedEquality]
public partial class InventoryCountLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid InventoryCountId { get; set; }

    [JsonIgnore]
    public InventoryCount InventoryCount { get; set; } = null!;

    public Guid ProductId { get; set; }

    [JsonIgnore]
    public ProductNode Product { get; set; } = null!;

    [Precision(14, 3)]
    public decimal CountedQuantity { get; set; }
}
