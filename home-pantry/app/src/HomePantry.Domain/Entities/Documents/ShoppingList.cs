using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_ShoppingLists")]
[KandraGeneratedEquality]
public sealed partial class ShoppingList : DocumentBase, IEntityWithRows<ShoppingList>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.ShoppingList);

    public ShoppingList() : base(TypeId) { }

    public Guid? StoreId { get; set; }

    [JsonIgnore]
    public Store? Store { get; set; }

    [KandraTablePart(TableName = "Doc_ShoppingListLines")]
    public IList<ShoppingListLine> Lines { get; set; } = [];
}

[KandraGeneratedEquality]
public partial class ShoppingListLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid ShoppingListId { get; set; }

    [JsonIgnore]
    public ShoppingList ShoppingList { get; set; } = null!;

    public Guid ProductId { get; set; }

    [JsonIgnore]
    public ProductNode Product { get; set; } = null!;

    [Precision(14, 3)]
    public decimal Quantity { get; set; }

    public bool Bought { get; set; }
}
