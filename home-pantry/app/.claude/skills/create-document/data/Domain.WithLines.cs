// TEMPLATE — line-bearing document, Domain plane.
// Target: {{Config}}.Domain/Entities/Documents/{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §1, §7.

using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using {{Config}}.Enums;
using Microsoft.EntityFrameworkCore;
// using {{Config}}.Domain.Commons;   // only needed if the line implements IOrderLine instead of IEntityRow — see below

namespace {{Config}}.Domain.Entities.Documents;

// OPTIONAL — delete this doc comment, or replace with your own rationale, once you've decided
// whether this document implements ISubconto (see reference.md §7).
/// <summary>
/// Not ISubconto - not used as a posting analytical dimension in the current chart. Still implements
/// IEntityWithTypeId so its TypeId is discoverable the same way as every other document/dictionary.
/// </summary>
[KandraDocumentEntity(TableName = "Doc_{{Name}}")]
[KandraGeneratedEquality]
public sealed partial class {{Name}} : DocumentBase, IEntityWithRows<{{Name}}>, IEntityWithTypeId
// To make this document a posting/analytical dimension instead (e.g. a "settlement document"
// reference on some account's subconto slot), implement ISubconto instead of IEntityWithTypeId:
// public sealed partial class {{Name}} : DocumentBase, IEntityWithRows<{{Name}}>, ISubconto
{
    public static Guid TypeId { get; } = new({{TypeIdsClass}}.{{Name}});

    public {{Name}}() : base(TypeId) { }

    // Header fields go here — nullable for optional, dictionary FKs as Guid + [JsonIgnore] nav property:
    // public Guid SomeDictionaryId { get; set; }
    // [JsonIgnore] public SomeDictionary SomeDictionary { get; set; }

    public string? Description { get; set; }

    // OPTIONAL — a header total, if this document has one. Recompute it server-side in the Behavior's
    // OnSaveAsync (reference.md §5) — never trust a client-submitted value.
    // [Precision(14, 2)]
    // public decimal Total { get; set; }

    // RowNavigations/RemoveOrphans (IEntityWithRows<{{Name}}>'s two members) are generated for you from
    // this [KandraTablePart] property — do NOT hand-write them (reference.md §1). If this document has no
    // lines at all, use Domain.Lineless.cs instead, not this file with an empty Lines collection.
    [KandraTablePart(TableName = "Doc_{{Name}}Lines")]
    public IList<{{Name}}Line> Lines { get; set; } = new List<{{Name}}Line>();
}

[KandraGeneratedEquality]
public partial class {{Name}}Line : IEntityRow
// IEntityRow is the only engine-level requirement for a line (just Guid Id, below). What else the line
// carries is entirely up to this document's own domain — it may reference zero, one, or several
// dictionaries via FK, none of which the engine assumes anything about. If this line does turn out to be
// an "order line" shape (references exactly one item-like dictionary + has Quantity + Price — see
// GoodsReceiptLine), you MAY implement {{Config}}.Domain.Commons.IOrderLine instead of bare IEntityRow —
// that's a {{Config}}-specific convenience interface, not an engine requirement, and most lines in a
// different domain won't fit it. Don't reach for it by default.
{
    public Guid Id { get; set; }

    public Guid {{Name}}Id { get; set; }
    [JsonIgnore]
    public {{Name}} {{Name}} { get; set; }

    // Line fields go here — there is no fixed shape. Typical patterns, none mandatory:
    //
    // A dictionary FK (a line may reference zero, one, or several dictionaries — name/target type are
    // entirely yours to choose, this is just one example):
    // public Guid SomeDictionaryId { get; set; }
    // [JsonIgnore] public SomeDictionary SomeDictionary { get; set; }
    //
    // An "order line" quantity/amount shape (only if it genuinely applies — see GoodsReceiptLine):
    // [Precision(14, 3)] public decimal Quantity { get; set; }
    // [Precision(14, 5)] public decimal Price { get; set; }
    // [Precision(14, 5)] public decimal Sum { get; set; }
    //
    // Lot/serial tracking (only if this document creates inventory batches — see GoodsReceiptLine):
    // public DateOnly? ExpirationDate { get; set; }
    // public string? SerialNumber { get; set; }
}
