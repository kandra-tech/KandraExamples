// TEMPLATE — flat dictionary, Domain plane.
// Target: {{Config}}.Domain/Entities/Dictionaries/{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §1, §6, §9.

using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using {{Config}}.Enums;

namespace {{Config}}.Domain.Entities.Dictionaries;

// OPTIONAL — delete this doc comment, or replace with your own rationale, once you've decided
// whether this dictionary implements ISubconto (see reference.md §6).
/// <summary>
/// Not ISubconto - not used as a posting analytical dimension in the current chart.
/// Still implements IEntityWithTypeId so its TypeId is discoverable the same way as
/// every other document/dictionary in the domain.
/// </summary>
[KandraDictionaryEntity(TableName = "Dict_{{PluralName}}")]
[KandraGeneratedEquality]
public partial class {{Name}} : DictionaryBase, IEntityWithTypeId
// To make this dictionary a posting/analytical dimension instead, implement ISubconto:
// public partial class {{Name}} : DictionaryBase, ISubconto
{
    public static Guid TypeId { get; } = new({{TypeIdsClass}}.{{Name}});

    // Domain-specific properties go here — nullable for anything optional.
    // Only redeclare Code/Name with `override` if this entity needs a different [MaxLength] than
    // DictionaryBase's default (Code 64 / Name 256) AND the Dto side also needs its own [Caption] —
    // otherwise prefer [KandraMaxLengthOverride(nameof(Code), N)] on the class instead (reference.md §9).
}
