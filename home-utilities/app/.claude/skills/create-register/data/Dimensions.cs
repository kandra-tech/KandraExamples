// TEMPLATE — the register's Dimensions class, mandatory for every family.
// Target: {{Config}}.Domain/Entities/Registers/{{Name}}.cs (same file as every other piece of this register).
// Placeholder legend: data/README.md. Concept reference: ../reference.md §2.
//
// [KandraGeneratedEquality] (mandatory here, must be `sealed partial`) fills in Equals/GetHashCode/ToString
// AND BuildEqualityPredicate() (an EF-translatable equality expression), the static VisitKeys(...) (each key
// member as a typed expression) and Clone() (a copy of the key values, navigations left unset) — never
// hand-write any of them.
// Keep this class to genuinely analytical keys only; non-key attributes belong in Resources/Details instead.

using System.ComponentModel;           // [Description] - what MCP describe_register shows an AI agent
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Domain.Registers;
using {{Config}}.Domain.Entities.Dictionaries;

namespace {{Config}}.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class {{Name}}Dimensions : IRegisterDimensions<{{Name}}Dimensions>
{
    // ---- Ordinary FK-paired dimension: Guid Id + [JsonIgnore] navigation. ----
    [Caption("{{DimensionCaptionKey}}")]
    [Description("{{DimensionDescription}}")]   // what it identifies, and any sentinel (Guid.Empty = ...)
    public Guid {{DimensionField}}Id { get; set; }
    [JsonIgnore]
    public {{DimensionDictionaryType}} {{DimensionField}} { get; set; } = null!;

    // more Guid-Id + [JsonIgnore] nav pairs as needed — repeat the block above per dimension

    // ---- OPTIONAL — delete if unused: a deliberately bare scalar dimension with no navigation. ----
    // Use only when there's no honest row a navigation could always point at (a sentinel value like
    // Guid.Empty, or a target type that varies per row) — see InventoryDimensions.PartyId in
    // ../reference.md §8 for the real worked case. Add [ReferenceOf(...)] (../reference.md §9) if you
    // want the register-transactions viewer to resolve it to something better than a raw Guid.
    // [Caption("{{BareScalarCaptionKey}}")]
    // [Description("{{DimensionDescription}}")]   // say what the bare value points at and what its sentinel means
    // public Guid {{BareScalarField}} { get; set; }
}
