// TEMPLATE — hierarchical dictionary (folder/leaf TPH tree), Domain plane.
// Target: {{Config}}.Domain/Entities/Dictionaries/{{Name}}.cs
// {{Name}} names the LEAF type — the abstract root becomes {{Name}}Node, the folder {{Name}}Folder.
// Placeholder legend: data/README.md. Concept reference: ../reference.md §1, §6, §9.

using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using {{Config}}.Enums;

namespace {{Config}}.Domain.Entities.Dictionaries;

// [KandraDictionaryEntity] goes on the abstract root ONLY — never repeat it on the Folder/leaf subclasses.
// The dictionary's ONE TypeId lives here, on the abstract root - folder and leaf inherit it, never
// redeclare it. Folder vs leaf is a property of the row (the TPH discriminator), not of the TypeId.
[KandraDictionaryEntity(TableName = "Dict_{{PluralName}}")]
public abstract class {{Name}}Node : HierarchicalDictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new({{TypeIdsClass}}.{{Name}});
}

/// <summary>
/// Not ISubconto - a folder is a structural grouping node, not a postable value.
/// </summary>
[KandraDictionaryFolder]
[KandraGeneratedEquality]
public sealed partial class {{Name}}Folder : {{Name}}Node
{
}

// OPTIONAL — delete this doc comment, or replace with your own rationale, once you've decided whether
// the leaf implements ISubconto (see reference.md §6).
/// <summary>
/// Implements ISubconto so a {{Name}} can be used directly as a posting analytical
/// dimension - no separate wrapper type.
/// </summary>
[KandraGeneratedEquality]
public sealed partial class {{Name}} : {{Name}}Node, ISubconto
// If this leaf is NOT a posting dimension, just drop ISubconto - the TypeId is inherited from {{Name}}Node:
// public sealed partial class {{Name}} : {{Name}}Node
{
    // Leaf-only business fields go here — nullable for anything optional.
}
