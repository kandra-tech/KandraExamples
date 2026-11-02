// TEMPLATE — lineless (header-only) document, Domain plane.
// Target: {{Config}}.Domain/Entities/Documents/{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §1, §7.
//
// Use this instead of Domain.WithLines.cs only when the document genuinely has no tabular line section
// (e.g. a universal manual posting document like AccountTransaction). IEntityWithRows<{{Name}}> is still
// mandatory — every Document entity implements it regardless (reference.md §1) — but with no
// [KandraTablePart] property to generate from, RowNavigations/RemoveOrphans must be hand-written as
// trivial no-ops, right here in this file.

using System.Linq.Expressions;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using {{Config}}.Enums;

namespace {{Config}}.Domain.Entities.Documents;

// OPTIONAL — delete this doc comment, or replace with your own rationale.
/// <summary>
/// Not ISubconto - not meant to be referenced as a posting analytical dimension itself.
/// </summary>
[KandraDocumentEntity(TableName = "Doc_{{Name}}")]
[KandraGeneratedEquality]
public sealed partial class {{Name}} : DocumentBase, IEntityWithTypeId, IEntityWithRows<{{Name}}>
{
    public static Guid TypeId { get; } = new({{TypeIdsClass}}.{{Name}});

    public {{Name}}() : base(TypeId) { }

    public static IEnumerable<Expression<Func<{{Name}}, object>>> RowNavigations => [];

    public void RemoveOrphans({{Name}} fetched, IRowRemover remover) { }

    // Header fields go here — nullable for optional. See AccountTransaction (reference.md §10) for the
    // real worked example: account-code + up to 5 subconto-value fields per side, using [AccountDialog]/
    // [SubcontoEditor] on the Forms-plane Dto (add-account-subconto-field skill covers those attributes).

    public string? Description { get; set; }
}
