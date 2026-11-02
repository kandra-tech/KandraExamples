using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Invoices")]
[KandraGeneratedEquality]
public sealed partial class Invoice : DocumentBase, IEntityWithRows<Invoice>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomeUtilitiesTypeIds.Invoice);

    public Invoice() : base(TypeId) { }

    public Guid ProviderId { get; set; }

    [JsonIgnore]
    public Provider Provider { get; set; } = null!;

    public Guid PropertyId { get; set; }

    [JsonIgnore]
    public Property Property { get; set; } = null!;

    public DateTime Month { get; set; }

    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }

    public string? Description { get; set; }

    public string? PaymentDescription { get; set; }

    [KandraTablePart(TableName = "Doc_InvoiceLines")]
    public IList<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}

[KandraGeneratedEquality]
public partial class InvoiceLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    [JsonIgnore]
    public Invoice Invoice { get; set; } = null!;

    public Guid ServiceId { get; set; }

    [JsonIgnore]
    public Service Service { get; set; } = null!;

    public Guid? MeterId { get; set; }

    [JsonIgnore]
    public Meter? Meter { get; set; }

    public TariffZone Zone { get; set; } = TariffZone.Single;

    [Precision(18, 3)]
    public decimal Quantity { get; set; }

    [Precision(18, 4)]
    public decimal Price { get; set; }

    [Precision(18, 2)]
    public decimal Amount { get; set; }
}
