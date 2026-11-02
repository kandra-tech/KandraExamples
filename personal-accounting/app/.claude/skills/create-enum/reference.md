# Enum engine — reference

Self-contained explanation of every engine type/attribute `SKILL.md` walks through — written so you don't need
to go read the engine's source to use it. Signatures and behavior as last verified against engine source;
if a build error contradicts something here, the code wins (see "If this reference goes stale" at the bottom).
Copy-paste skeletons live in [`data/`](data/).

## What an enum actually is

Not an entity kind with a pipeline of its own. There is **no `*Base`/`*Dto`/`*Behavior`/`*Validator`, no table, no
controller, no Refit client, no nav entry** — it's an ordinary C# `enum` decorated with a few attributes so
three engine consumers can treat it well. It reaches the database and the UI only as the *type of a property*
on some other entity/Dto. The three consumers:

1. **The generated form** — a property typed as an enum gets the `EnumSelect` editor automatically (§4).
2. **Print / CSV export** — `PrintValueFormatter` renders an enum through its `[EnumCaption]` (§4).
3. **Accounting** (opt-in) — `EnumSubconto<TEnum>` adapts a decorated enum into a subconto dimension (§3).

## 1. Where it lives, and why

`{Config}.Enums` — a tiny project whose only reference is `Kandra.Attributes`. Domain, Forms **and** the Blazor
WASM client all reference it, which is the whole point: the same enum type is used unchanged on the entity, on
the Dto and in the browser, so there is no Domain-enum/Dto-enum pair and no mapping code. Putting an enum in
`{Config}.Domain` would make it invisible to the client (`Forms` must never reference server-only projects).

The same project also holds `{Config}TypeIds` (the shared, `partial` type-identity constants class) — see §2.

## 2. Attribute vocabulary

| Attribute | Namespace | Target | Signature | Meaning |
|---|---|---|---|---|
| `[EnumCaption]` | `Kandra.Attributes.Naming` | enum member (field) | `(string value, bool isLiteral = false)` | Display label: a localization key, or literal text when `isLiteral` is true. Extends `VisibleNameAttribute`, same shape as `[Caption]`. |
| `[TypeId]` | `Kandra.Attributes.Entities` | enum **or** class | `(string typeId)` — parsed with `Guid.Parse` | Canonical, permanent identity of the *type*. On an enum, it's what `EnumSubconto<T>` and the client type registry read. |
| `[SubcontoId]` | `Kandra.Attributes.Accounting` | enum member (field) | `(string id)` — parsed with `Guid.Parse` | Fixed, permanent identity of one *member* as a subconto value. Unrelated to the enum-level `[TypeId]`. |
| `[EnumGuidDropdown]` | `Kandra.Attributes.Editors` | Dto property | `(Type enumType)` | Select that lists an enum's compiled-in members but binds the member's `[SubcontoId]` **Guid** instead of the enum value. Property must be a `Guid`. |
| `[EnumGuidMultiSelect]` | `Kandra.Attributes.Editors` | Dto property | `(Type enumType)` | Multi-value counterpart; property must be a Guid collection (`IReadOnlyList<Guid>?`). |
| `[Description]` | BCL (`System.ComponentModel`) | enum (type-level only) | `(string)` ctor | Semantic explanation of what the enum represents — not a UI label (`[EnumCaption]` covers that, per member). Reflected by `Kandra.Mcp`'s `EntityFieldMetadataBuilder` and surfaced through the MCP server's `describe_entity_type`/`list_entity_types` tools as an AI agent's primary way to learn what the enum means. **Mandatory on every enum a Dto property can reference** — plain or subconto-backed alike, `McpEnumDiscovery` (`Kandra.Generators.EntityOperations`) makes any enum reachable from a form's properties a first-class MCP entity regardless of `[TypeId]`. No per-member `[Description]` convention exists — only the type gets one. |

**Only `[EnumCaption]` (per member) and `[Description]` (on the type) are needed for plain use.** `[TypeId]`,
`[SubcontoId]` and the two editors matter only for the subconto role (§3).

### Type identity — the same convention as every other kind

`[TypeId(PersonalAccountingTypeIds.{Name})]` references a constant in `{Config}.Enums/PersonalAccountingTypeIds/`. That class is
`partial` and its main file is permanently empty: **every constant is its own new sibling file**
(`PersonalAccountingTypeIds.CostingMethod.cs`, `PersonalAccountingTypeIds.ItemCategoryType.cs`, ...), so adding an enum never
means editing a shared file and two people adding two enums in parallel never touch the same line. The real
`CostingMethod` sibling file carries a comment worth repeating: it is the *enum type-level* id only — the
per-member `[SubcontoId]` attributes are unrelated and stay on the members.

### The type registry (automatic discovery)

`Kandra.Generators.TypeRegistry` walks the compilation and every referenced assembly whose name starts with
`Kandra` or your configuration's root name, collecting every type carrying `[TypeId]`. For an **enum** it emits
a `TypeRegistryEntry` of kind `Enum` (`DialogDtoType` = the enum itself, no server-side query type, never
hierarchical) — what lets a runtime-picked subconto value be turned back into the right selector control. Build
diagnostics it can raise:

| Id | Meaning |
|---|---|
| `KANDRATYPEREG003` | Duplicate `[TypeId]` value across two types — you reused a GUID. |
| `KANDRATYPEREG004` | `[TypeId]` value is not a valid GUID literal. |

(`KANDRATYPEREG001`/`002` concern classes missing/doubling a Dictionary/Document form attribute — enums are
exempt.) You never register an enum by hand.

## 3. The subconto role

`Kandra.Domain.Accounting.EnumSubconto<TEnum>` (`where TEnum : struct, Enum`) adapts **any decorated enum** into
an `ISubconto`, so a small closed set — payment method, shipment kind — can be a chart-of-accounts dimension
without a backing dictionary table. The enum owns its own GUIDs right next to the values they identify, so
nothing separate can drift out of sync.

What it reads, by reflection at first use of each closed generic type:

- `TypeId` ← the enum's `[TypeId]`. **Missing → `TypeInitializationException`** wrapping an `InvalidOperationException`
  ("must be decorated with [TypeId]"): it is thrown from the closed generic type's static initializer, so the
  first use of `EnumSubconto<T>` fails, and — .NET caches a failed type initializer — so does every later use in
  that process. Fix the attribute and restart, not retry.
- one id per member that has `[SubcontoId]`; `new EnumSubconto<T>(value)` for a member **without** it throws
  `InvalidOperationException` immediately (fail-fast, not silently unmapped).
- `Id` ← the wrapped member's `[SubcontoId]`; the `IRootEntity.Id` setter throws (identity is fixed by the value).
- `EnumSubconto<T>.FromId(guid)` — the reverse: a runtime-picked Guid back to a concrete instance; throws if no
  member carries that id.

Consequences worth internalising:

- **A `[SubcontoId]` GUID is permanent once posted against.** Posting rows store it; changing or reusing one
  orphans or re-labels history. Never regenerate GUIDs on an existing enum.
- **Adding a member later needs its own fresh `[SubcontoId]`** if it will be posted — forgetting it fails at
  the first posting of that value, not at build time.
- On the client, `Kandra.Forms.Accounting.SubcontoEnumValues<TEnum>` (`ByValue` / `ByGuid`) does the same
  Guid↔member resolution without pulling in server-only `EnumSubconto<>`; `[EnumGuidDropdown]`/
  `[EnumGuidMultiSelect]` use it.
- Chart-of-accounts side (declaring `EnumSubconto<T>` on an `AccountLeaf`, building postings) is documented in
  the Kandra platform's chart-of-accounts posting spec (§4.7, "Enum-backed subconto") and belongs to the
  `post-to-accounting` skill.
- **No live consumer in the source configuration today:** grep of that configuration finds no `EnumSubconto<>`,
  `[EnumGuidDropdown]` or `[EnumGuidMultiSelect]` usage outside the engine, even though both real enums are fully
  decorated. Treat the
  `Guid`-property + `[EnumGuidDropdown]` pattern as following the attribute's own contract, not as a
  copy-from-real-code recipe.

## 4. What the UI and print layers do with an enum property

- **Editor.** `UiFieldDiscovery` classifies any enum-typed property as `UiFieldClrKind.Enum` and gives it the
  default `UiEditorKind.EnumSelect`; the Blazor emitter turns that into `AddEnumSelect<TEnum>(...)` and the
  `EnumSelect<TEnum>` component, a `MudSelect` over **`Enum.GetValues<TEnum>()`** — every member, in
  ascending numeric-value order (that's what `Enum.GetValues` returns — not declaration order), with
  `GetCaption(...)` as the label. Consequences: there's no way to hide a retired member from the dropdown
  (leave it out of use by convention, or split the enum), and the order users see follows your explicit
  values, so number members in the order you want them shown.
- **Nullable enums are not handled.** The classifier unwraps `Nullable<T>`, but the `EnumSelect` emitter branch
  — unlike the `DateOnly` and Guid editor branches — has no `IsNullable` handling and passes the property to
  non-nullable `Func<TEnum>`/`Action<TEnum>` parameters. Declare Dto enum properties **non-nullable** and model
  "not chosen" with an explicit member (`None = 0`). (Not exercised end-to-end here; if you need `T?`, try it
  and read the generated `*.g.cs` before committing to it.)
- **Label resolution.** `EnumLocalizationExtensions.GetCaption(this Enum, IStringLocalizer)`: no `[EnumCaption]`
  → the raw member name; `isLiteral` → the literal text; otherwise `localizer[key]`. It goes through the normal
  localization chain, so the resx files apply. Everything that shows an enum value uses this one method.
- **Print / CSV.** `PrintValueFormatter` renders an enum through `GetCaption`, "falling back to its raw member
  name only when the enum member carries no `[EnumCaption]`" — shared engine code, not something each print
  form does.
- **List column.** The reference `ItemDto.Category` uses `[ListFormColumn(Filterable = false)]`. The query layer
  does have enum support (`FieldTypeEnum.Enum` in `Kandra.Application/Queryable/FieldType.cs`); this skill has not
  verified how the grid's filter UI behaves for an enum column, so `Filterable = false` is what the reference
  configuration ships, not a rule the engine states.
- **Persistence.** EF Core's default enum mapping: the **underlying integer**. No `HasConversion` for enums
  exists in `Kandra.Persistence`/`Kandra.Domain`, and that configuration's SQLite snapshot maps `ItemDto`'s
  `Category` to `b.Property<int>("Category")`. So the database stores numbers, which is why member values must
  be explicit and never renumbered (SKILL.md step 2).

## 5. Localization

Same three-file mechanism as everywhere: `{Config}.Localization/PersonalAccounting.resx` (neutral/English),
`PersonalAccounting.ru.resx`, `PersonalAccounting.uk.resx`, one `<data name="Enum_{Name}_{Member}">` per member per file (see
[`data/Localization.resx.xml`](data/Localization.resx.xml)). A missing key **compiles and runs** — it renders the
raw key. A key present in `.resx` but absent from `.uk.resx` shows the fallback for Ukrainian users only, which
is why SKILL.md's verify step says to switch the UI language. The two real enums' keys are named
`Enum_ItemCategoryType_*` and `Enum_CostingMethod_*` in that configuration's resx files.

## 6. Worked examples

Both live in the source configuration's `Enums` project and are decorated identically (subconto-ready), differing only in members:

```csharp
[TypeId(PersonalAccountingTypeIds.ItemCategoryType)]
[Description("The broad category an Item belongs to - a classification dimension, distinct from the Item hierarchy's own folders.")]
public enum ItemCategoryType
{
    [EnumCaption("Enum_ItemCategoryType_Goods")]
    [SubcontoId("4ef39b2a-f5aa-4469-aa34-e719a3ddd4b5")]
    Goods = 0,

    [EnumCaption("Enum_ItemCategoryType_Production")]
    [SubcontoId("e330dcbd-1c6b-4b73-bb40-efc4cd23e7b8")]
    Production = 1,
    // Material = 2, Service = 3, Kit = 4 ...
}
```

- **`ItemCategoryType`** — used as a plain property: `Item.Category` (Domain, same type) and `ItemDto.Category`
  (`[Required] [Caption("Category")] [ListFormColumn(Filterable = false)]`). The full journey of a plain enum:
  declared once, referenced on both sides, stored as `int`, rendered as a localized select, printed as its
  caption. The GUIDs are unused by this property.
- **`CostingMethod`** — `MovingAverage`, `Fifo`, `SpecificIdentification`, `Fefo`. A domain-logic enum consumed
  directly by inventory costing code (a `CostingMethodSetting` document records which method applies where); it is
  a good example of an enum whose members drive behavior, not just labels.

Both carry real `[Description]` content worth reading as a model for how much detail is useful (a sentence or
two, type-level only — no per-member `[Description]`). Neither is yet used through `EnumSubconto<>` — see §3.

## Hand-maintained files

| What | File | Why it's manual |
|---|---|---|
| The enum | New `{Config}.Enums/{Name}.cs` | It's the artifact itself. |
| Type identity (subconto role only) | New `{Config}.Enums/PersonalAccountingTypeIds/PersonalAccountingTypeIds.{Name}.cs` — never an edit to `PersonalAccountingTypeIds.cs` | No generator mints GUIDs. |
| Localization keys | Edit `{Config}.Localization/*.resx` ×3 | One `Enum_{Name}_{Member}` per member per file. |
| EF migration for the *property* | New migration per provider | The enum has no table; the entity column it's used on does. |

Not in this table: Dto/Behavior/Validator, Refit client, nav entry, Blazor pages, DI/EF registration — none
apply to a bare enum.

## Gotchas

- Explicit values, never renumbered — the database stores integers (§4).
- Every member needs `[EnumCaption]` and a resx entry, or it renders the raw name/key.
- A `[SubcontoId]`/`[TypeId]` GUID is permanent and must be unique (`KANDRATYPEREG003` catches type-level
  duplicates at build; member-level duplicates are **not** caught by the build).
- Adding a postable member later needs a fresh `[SubcontoId]`; the omission fails at first posting.
- The enum select lists every member in numeric-value order — no hiding a retired one, and value order is display order.
- Keep the enum in `{Config}.Enums`, not `{Config}.Domain`, or the client can't see it.
- Prefer `None = 0` over a nullable Dto property (§4).

## If this reference goes stale

A point-in-time snapshot. If the code disagrees — a compile error citing a signature, a `KANDRATYPEREG*` id that
doesn't exist, an `EnumSelect` that now handles nullable — trust the code and update this file. Fastest
re-verification: grep the type name in the engine source under `Kandra.Attributes/`, `Kandra.Localization/`,
`Kandra.Domain/Accounting/`, `Kandra.Forms/Accounting/`, `Kandra.Generators.TypeRegistry/`,
`Kandra.Generators.Shared/UiFieldDiscovery.cs`, `Kandra.Generators.Ui.Blazor/FieldEditorEmitter.cs`,
`Kandra.Printing/PrintValueFormatter.cs`, or `Kandra.ClientLib.MudBlazor/Shared/Components/Selectors/`.
