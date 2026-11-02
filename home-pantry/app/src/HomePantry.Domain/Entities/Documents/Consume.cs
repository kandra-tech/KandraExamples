using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Consumes")]
[KandraGeneratedEquality]
public sealed partial class Consume : DocumentBase, IEntityWithRows<Consume>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.Consume);

    public Consume() : base(TypeId) { }

    public ConsumeReason Reason { get; set; }

    [KandraTablePart(TableName = "Doc_ConsumeLines")]
    public IList<ConsumeLine> Lines { get; set; } = [];
}

[KandraGeneratedEquality]
public partial class ConsumeLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid ConsumeId { get; set; }

    [JsonIgnore]
    public Consume Consume { get; set; } = null!;

    public Guid ProductId { get; set; }

    [JsonIgnore]
    public ProductNode Product { get; set; } = null!;

    public Guid LocationId { get; set; }

    [JsonIgnore]
    public LocationNode Location { get; set; } = null!;

    [Precision(14, 3)]
    public decimal Quantity { get; set; }
}
