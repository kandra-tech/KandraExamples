using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Transfers")]
[KandraGeneratedEquality]
public sealed partial class Transfer : DocumentBase, IEntityWithRows<Transfer>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.Transfer);

    public Transfer() : base(TypeId) { }

    [KandraTablePart(TableName = "Doc_TransferLines")]
    public IList<TransferLine> Lines { get; set; } = [];
}

[KandraGeneratedEquality]
public partial class TransferLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid TransferId { get; set; }

    [JsonIgnore]
    public Transfer Transfer { get; set; } = null!;

    public Guid ProductId { get; set; }

    [JsonIgnore]
    public ProductNode Product { get; set; } = null!;

    public Guid FromLocationId { get; set; }

    [JsonIgnore]
    public LocationNode FromLocation { get; set; } = null!;

    public Guid ToLocationId { get; set; }

    [JsonIgnore]
    public LocationNode ToLocation { get; set; } = null!;

    [Precision(14, 3)]
    public decimal Quantity { get; set; }
}
