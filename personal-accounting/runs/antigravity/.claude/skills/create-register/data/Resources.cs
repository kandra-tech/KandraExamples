// TEMPLATE — a custom Resources class. Balance/Turnover families ONLY, and ONLY if the shipped
// Kandra.Domain.Registers.QuantityResources / NonNegativeQuantityResources don't fit (a single guarded-or-not
// quantity is enough for most registers — reuse one of those and skip this file entirely).
// Target: {{Config}}.Domain/Entities/Registers/{{Name}}.cs (same file as every other piece of this register).
// Placeholder legend: data/README.md. Concept reference: ../reference.md §3a.
//
// The four generic-math operators + AdditiveIdentity are STATIC ABSTRACT, closed per concrete type — they
// don't inherit like a virtual instance method would. They must stay TOTAL (never throw, never clamp); only
// the two checks may throw, and only the engine calls them: EnsureNonNegative() on every movement (must cover
// EVERY member), EnsureValidBalance() on each touched balance row after a delta is applied.
//
// [KandraGeneratedEquality] (mandatory here, must be `sealed partial`) fills in Equals/GetHashCode/ToString
// and Clone() over the stored properties (the computed IsZero is skipped) — never hand-write them. The
// register writers use Equals to skip movements that didn't change when a document is re-posted.

using System.ComponentModel;         // [Description] - what MCP describe_register shows an AI agent
using Kandra.Attributes.Entities;  // [KandraGeneratedEquality]
using Kandra.Attributes.Naming;    // [Caption]
using Kandra.Attributes.Printable; // [Format]
using Kandra.Domain.Exceptions;
using Kandra.Domain.Registers;
using Microsoft.EntityFrameworkCore; // [Precision] — EF Core's own attribute, not a Kandra one

namespace {{Config}}.Domain.Entities.Registers;

// [Caption]/[Format] here are register-transactions-viewer display metadata (../reference.md §9) — the same
// role they play on Dimensions/Details, since a register has no separate Dto to carry them instead.
[KandraGeneratedEquality]
public sealed partial class {{Name}}Resources : IRegisterResources<{{Name}}Resources>
{
    [Precision({{Precision}}, {{Scale}})]
    [Caption("{{ResourceCaptionKey}}")]
    [Format("{{FormatString}}")]
    [Description("{{ResourceDescription}}")]   // unit and currency (and as of which date if converted); never negative on a movement, direction is IsExpense; whether the balance may go negative
    public decimal {{ResourceField}} { get; init; }
    // more decimal members as needed, each with its own [Precision(p, s)] [Caption(...)] [Format(...)] [Description(...)]

    public static {{Name}}Resources AdditiveIdentity => new();

    public static {{Name}}Resources operator +({{Name}}Resources a, {{Name}}Resources b)
        => new() { {{ResourceField}} = a.{{ResourceField}} + b.{{ResourceField}} /* + every other member */ };

    public static {{Name}}Resources operator -({{Name}}Resources a)
        => new() { {{ResourceField}} = -a.{{ResourceField}} /* + every other member, negated */ };

    public static {{Name}}Resources operator -({{Name}}Resources a, {{Name}}Resources b) => a + (-b);

    // Movement check: throw for EVERY member — a movement's values are never negative (direction is IsExpense).
    public void EnsureNonNegative()
    {
        // NOTE the literal example below is unescaped C# ($"{nameof(X)} ... ({X})."), not a {{Placeholder}} —
        // write it the same way once {{ResourceField}} is replaced with your real property name, e.g.:
        //   if (Quantity < 0) throw new InsufficientBalanceException($"{nameof(Quantity)} cannot be negative ({Quantity}).");
        if ({{ResourceField}} < 0)
            throw new InsufficientBalanceException($"{{ResourceField}} cannot be negative.");
        // + every other member
    }

    // Balance guard: OPTIONAL — delete this method and the balance gets the same check as a movement (no member
    // may go negative). Keep it to relax that: an empty body lets the balance go negative (e.g. settlements), or
    // check only some members (InventoryResources guards Quantity but not Amount, ../reference.md §8).
    public void EnsureValidBalance()
    {
        if ({{ResourceField}} < 0)
            throw new InsufficientBalanceException($"{{ResourceField}} cannot be negative.");
    }

    public bool IsZero => {{ResourceField}} == 0m; // && every other member == 0
}
