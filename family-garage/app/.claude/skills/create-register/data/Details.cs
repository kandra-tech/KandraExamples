// TEMPLATE — a Details class: the non-aggregated payload. Any family, OPTIONAL — use the shipped
// Kandra.Domain.Registers.NoDetails instead if the register needs nothing beyond dimensions (+ resources).
// Target: {{Config}}.Domain/Entities/Registers/{{Name}}.cs (same file as every other piece of this register).
// Placeholder legend: data/README.md. Concept reference: ../reference.md §3b.
//
// On a Movement (Balance/Turnover), Details rides along with each posted row — movement-scoped attributes.
// On an Info record (DocumentInfo/PeriodicInfo), Details IS the stored value itself — there's no separate
// "Resources" concept for the Info family.
//
// [KandraGeneratedEquality] (mandatory here, must be `sealed partial`) fills in Equals/GetHashCode/ToString
// and Clone() over the stored properties — never hand-write them. The register writers use Equals to skip
// rows that didn't change when a document is re-posted, and Clone() to give every row its own instance.

using System.ComponentModel;        // [Description] - what MCP describe_register shows an AI agent
using Kandra.Attributes.Entities; // [KandraGeneratedEquality], and [ReferenceOf] if you uncomment that block below
using Kandra.Attributes.Naming;    // [Caption]
using Kandra.Attributes.Printable; // [Format]
using Kandra.Domain.Registers;
using Microsoft.EntityFrameworkCore; // [Precision] — EF Core's own attribute, not a Kandra one

namespace {{Config}}.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class {{Name}}Details : IRegisterDetails<{{Name}}Details>
{
    [Precision({{Precision}}, {{Scale}})]
    [Caption("{{DetailCaptionKey}}")]
    [Format("{{FormatString}}")]
    [Description("{{DetailDescription}}")]   // what the value is (unit, currency), and on an Info record: valid from Period until the next record
    public decimal {{DetailField}} { get; set; }

    // OPTIONAL — delete if unused: a plain non-numeric stored field, no [Precision] needed.
    // [Caption("{{DetailCaptionKey2}}")]
    // [Description("{{DetailDescription}}")]   // and what null means
    // public string? {{DetailField2}} { get; set; }

    // OPTIONAL — delete if unused: a reference field with no EF navigation to pair against — see
    // ../reference.md §9 for [ReferenceOf] static/dynamic modes (e.g. InventoryDetails.SupplierId).
    // [Caption("{{DetailCaptionKey3}}")]
    // [Description("{{DetailDescription}}")]   // which entity the id points at, and what null means
    // [ReferenceOf(typeof({{TargetDictionaryOrDocumentType}}))]
    // public Guid? {{DetailField3}}Id { get; set; }
}
