---
name: create-enum
description: Add a new enum (a small, closed, compile-time set of named values — item category, costing method, payment kind, status) to a Kandra configuration, either as a plain localized property type or as an accounting subconto dimension. Use when the user asks to add an enum, a fixed list of choices, a status/type/kind field with a closed value set, or an enum-backed subconto.
---

# Creating an enum

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

An enum is the lightest entity kind — a plain C# `enum`, not a persisted entity of its own — but the engine
integrates it into three places worth getting right: the **generated form** (a dropdown listing every member with
a localized label), **print/CSV export** (the same localized label instead of the raw member name), and, if you
opt in, **accounting** (the enum becomes a closed set of subconto values). See [`reference.md`](reference.md)
for the exact attribute signatures, what each consumer does with them, and a full audit of what is and isn't
hand-maintained. Project names below (`{Config}.Enums`, `{Config}.Localization`, ...) stand in for your
configuration's own — `{Config}` is the root name you passed to `dotnet new kandra-config -n`. The worked
examples, `ItemCategoryType` and `CostingMethod`, come from a real, previously-built Kandra configuration.

**An enum needs one small file, its resx keys, and nothing else.** No Dto, Behavior, Validator, Refit client,
nav entry, Blazor page, or migration *for the enum itself* — it has no table and no endpoint. It only reaches the
database and the UI through an **entity/Dto property typed as the enum** (step 5), and that property is what
needs a migration.

## 1. Decide which role it plays

| | Plain enum | Subconto-backing enum |
|---|---|---|
| Use when | A property just needs a closed value set (`Item.Category`) | The values must be **accounting dimension values** posted against accounts (`EnumSubconto<T>`) |
| Attributes | `[EnumCaption]` per member | `[EnumCaption]` **plus** `[TypeId]` on the enum and `[SubcontoId]` per member |
| Type identity (step 3) | not needed | required — a new sibling file in `FamilyGarageTypeIds/` |

Start plain. Adding `[TypeId]`/`[SubcontoId]` later is additive, but a `[SubcontoId]` GUID, once shipped and
posted against, is permanent (`reference.md` §3). Note that both real enums this recipe draws on carry the full
subconto decoration even though no chart-of-accounts slot in that configuration uses either as a subconto today — that's an
option they hold open, not something plain use requires.

## 2. Write the enum (`{Config}.Enums/{Name}.cs`)

Copy [`data/Enum.Plain.cs`](data/Enum.Plain.cs) (or [`data/Enum.Subconto.cs`](data/Enum.Subconto.cs) for the
subconto role) — `data/README.md` explains every `{{Placeholder}}`.

```csharp
using System.ComponentModel;
using Kandra.Attributes.Naming;

namespace {Config}.Enums;

[Description("One or two sentences: what this enum represents and what its members mean collectively.")]
public enum {Name}
{
    [EnumCaption("Enum_{Name}_{MemberA}")]
    {MemberA} = 0,

    [EnumCaption("Enum_{Name}_{MemberB}")]
    {MemberB} = 1,
}
```

- **Always assign explicit numeric values, and never renumber or reuse one.** EF stores the enum as its
  underlying integer (no value conversion is configured — `reference.md` §5), so reordering members or
  inserting one in the middle silently re-labels every existing row. Append new members with the next unused
  number; to retire a member, leave it in place.
  This holds for **every enum stored on an entity**, not only this skill's own: an explicit `= 2` and an
  implicit 2 compile to the same thing, so no build check catches a missing value or a reorder. The engine and
  the reference configuration guard it with a snapshot test that walks the EF model and pins
  `Enum.Member = value` for every persisted enum (`PersistedEnumValuesTests` in their test projects); copy
  that test into your configuration's tests once it has persisted enums.
- **`[EnumCaption]` on every member.** It's the localization key (or literal text with `isLiteral: true`). A
  member without one still works but renders its raw C# name, untranslated, in the form, the grid and exports.
- **`[Description]` on the type is mandatory** (plain or subconto role alike — any enum reachable from a Dto
  property is a first-class MCP entity, `reference.md` §2) — it's what the MCP server's
  `describe_entity_type`/`list_entity_types` tools surface to an AI agent, `[EnumCaption]` is only a per-member
  UI label. No per-member `[Description]` convention exists.
- The key shape `Enum_{Name}_{Member}` is a **convention**, not enforced — but keep it, it's what every existing
  key uses and it makes a missing translation greppable.
- Put the enum in `{Config}.Enums`, not `{Config}.Domain`: that project is referenced by Domain, Forms **and**
  the Blazor client, and the enum must be visible to all three (`reference.md` §1).
- Model "not chosen" as an explicit member (`None = 0`) rather than making the Dto property nullable — the
  generated enum select has no nullable branch (`reference.md` §4).

## 3. Mint a type identity and subconto ids (subconto role only)

Skip this step for a plain enum.

**The enum's `[TypeId]`** — fresh GUID, its own new file, **never an edit to `FamilyGarageTypeIds.cs`** (same
partial-class convention Documents, Dictionaries and Constants use):

```csharp
// {Config}.Enums/FamilyGarageTypeIds/FamilyGarageTypeIds.{Name}.cs
namespace {Config}.Enums;

public static partial class FamilyGarageTypeIds
{
    public const string {Name} = "{FreshGuid}";
}
```

Copy [`data/TypeIdConstant.cs`](data/TypeIdConstant.cs), then put `[TypeId(FamilyGarageTypeIds.{Name})]` on the
enum. **`[SubcontoId("{FreshGuid}")]` on every member** that can be posted — a different fresh GUID per member,
unrelated to the enum-level `[TypeId]`. Never reuse a GUID anywhere; a duplicate `[TypeId]` fails the build
(`KANDRATYPEREG003`).

## 4. Localization keys

Add every `[EnumCaption]` key to `{Config}.Localization/FamilyGarage.resx` (and `.ru.resx`/`.uk.resx` when translating), using [`data/Localization.resx.xml`](data/Localization.resx.xml) as the shape. A missing key does
**not** fail the build — the label renders as the raw key (`Enum_ItemCategoryType_Kit`). Never pass
`isLiteral: true` to dodge translating.

## 5. Use it

**As a property** (both sides use the *same* type — no mapping, no second enum):

```csharp
// {Config}.Domain/Entities/.../{Entity}.cs
public {Name} {Property} { get; set; }

// {Config}.Forms/.../{Entity}.cs
[Required]
[Caption("{Property}")]
[ListFormColumn(Filterable = false)]     // optional; see reference.md §4
public {Name} {Property} { get; set; }
```

The generated form renders it as a select of every member with its localized caption, and print/CSV export
renders the same caption — nothing to wire. **This is the step that needs an EF migration** (a new integer
column on the entity's table) — regenerate all three providers as in `create-dictionary` step 7 /
`create-document` step 6. Add a `[Caption]` resx key for the property name itself as usual.

**As a subconto dimension** (subconto role): bind a `Guid` Dto property with `[EnumGuidDropdown(typeof({Name}))]`
(multi: `[EnumGuidMultiSelect]` on `IReadOnlyList<Guid>?`), and reference `EnumSubconto<{Name}>` in the chart of
accounts. See `reference.md` §3 and the `post-to-accounting` / `add-account-subconto-field` skills; the
chart-of-accounts wiring is theirs, not this skill's.

## 6. Nothing else to register by hand

No generator wiring, no DI line, no registry edit. A `[TypeId]`-attributed enum is discovered automatically by
`Kandra.Generators.TypeRegistry` on the next build (`reference.md` §2).

## 7. Verify

`dotnet build FamilyGarage.slnx` — no `KANDRATYPEREG*` diagnostic (subconto role), then live: open a form with the
property and confirm the select lists **every** member with a translated label, **switch the UI language** and
confirm the labels follow (this is the only way to catch a key missing from `.ru`/`.uk`), save, reopen, and
export/print a row that shows the value and confirm the label, not the member name, appears.

## If something here doesn't match the engine

`reference.md` is a point-in-time snapshot of engine source — if a build error contradicts it, the code wins.
Re-verify against the engine source (the `Kandra.*` packages' repositories) by grepping the type name under
`Kandra.Attributes/{Naming,Entities,Accounting,Editors}/`, `Kandra.Localization/`, `Kandra.Domain/Accounting/EnumSubconto.cs`, `Kandra.Generators.TypeRegistry/`, or
`Kandra.ClientLib.MudBlazor/Shared/Components/Selectors/`, then fix `reference.md`/`data/`.
