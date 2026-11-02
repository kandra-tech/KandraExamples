using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Payments")]
[KandraGeneratedEquality]
public sealed partial class Payment : DocumentBase, IEntityWithTypeId, IEntityWithRows<Payment>
{
    public static Guid TypeId { get; } = new(HomeUtilitiesTypeIds.Payment);

    public Payment() : base(TypeId) { }

    public static IEnumerable<Expression<Func<Payment, object>>> RowNavigations => [];

    public void RemoveOrphans(Payment fetched, IRowRemover remover) { }

    public Guid ProviderId { get; set; }

    [JsonIgnore]
    public Provider Provider { get; set; } = null!;

    public Guid PropertyId { get; set; }

    [JsonIgnore]
    public Property Property { get; set; } = null!;

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    public string? Comment { get; set; }

    public string? PaymentDescription { get; set; }

    public Guid? InvoiceId { get; set; }

    [JsonIgnore]
    public Invoice? Invoice { get; set; }
}
