using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Purchases")]
[KandraGeneratedEquality]
public sealed partial class Purchase : DocumentBase, IEntityWithRows<Purchase>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.Purchase);

    public Purchase() : base(TypeId) { }

    public Guid StoreId { get; set; }

    [JsonIgnore]
    public Store Store { get; set; } = null!;

    public Guid? ShoppingListId { get; set; }

    [JsonIgnore]
    public ShoppingList? ShoppingList { get; set; }

    [Precision(14, 2)]
    public decimal Total { get; set; }

    [KandraTablePart(TableName = "Doc_PurchaseLines")]
    public IList<PurchaseLine> Lines { get; set; } = [];
}

[KandraGeneratedEquality]
public partial class PurchaseLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid PurchaseId { get; set; }

    [JsonIgnore]
    public Purchase Purchase { get; set; } = null!;

    public Guid ProductId { get; set; }

    [JsonIgnore]
    public ProductNode Product { get; set; } = null!;

    [Precision(14, 3)]
    public decimal Quantity { get; set; }

    [Precision(14, 2)]
    public decimal Price { get; set; }

    [Precision(14, 2)]
    public decimal Amount { get; set; }

    public Guid LocationId { get; set; }

    [JsonIgnore]
    public LocationNode Location { get; set; } = null!;

    public DateTime BestBeforeDate { get; set; }
}
