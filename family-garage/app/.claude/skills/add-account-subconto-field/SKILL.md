---
name: add-account-subconto-field
description: Add account-code and subconto (analytical dimension) picker fields to a Dto so Kandra's UI generator renders the composite account+subconto editor — used when the account/subconto values are picked by the user at runtime rather than fixed in code. Use when building a manual-posting document (like AccountTransaction), or adding account/subconto filter fields to a report.
---

# Adding account/subconto picker fields to a Dto

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

Most documents (`GoodsReceipt`, `Waybill`, `Transfer`, ...) have **fixed** accounts and subconto CLR types
known at compile time — they post via the `post-to-accounting` skill's `AsOf(...).With<T1,T2>(...).Build()`
builder directly and never need any of this UI. This skill is for the rarer case where the **account itself**
is a user choice: a manual-posting document like `AccountTransaction`, or (not built yet, but the
infrastructure supports it) an account/subconto filter on a report.

Session history: `HANDOFF_061.md` built this whole stack (attributes, generator wiring, `AccountSubcontoEditor`/
`SubcontoValueSelect` components, `AccountTransaction`). `HANDOFF_062.md` is a backend-only refactor of
`post-to-accounting`'s posting API — **nothing in this skill changed because of it**; the UI layer was already
insulated (it only ever deals in `Guid?`/`TypeId`, never in `ISubcontoSet`/`SubcontoSet<T>` server types).

## 1. The Dto field shape

One `string?` account-code property plus up to 5 `Guid?` subconto slot properties, each pointing back at the
account property by name + declaring its own 0-based slot index. Real, current example —
`AccountTransactionDto` (`FamilyGarage.Forms/Documents/AccountTransaction.cs`):

```csharp
// AccountDialog binds a string? regardless of business requiredness (there's no Guid Id on an account -
// AccountNode has none) - [Required] is what actually enforces "must pick an account" here.
[Required] [Caption("SourceAccount")] [AccountDialog]
public string? SourceAccountCode { get; set; }

[Caption("SourceSubconto1")] [SubcontoEditor(nameof(SourceAccountCode), 0)]
public Guid? SourceSubconto1 { get; set; }

[Caption("SourceSubconto2")] [SubcontoEditor(nameof(SourceAccountCode), 1)]
public Guid? SourceSubconto2 { get; set; }

// ... SourceSubconto3/4/5 the same way, slot indices 2/3/4 ...

[Required] [Caption("DestinationAccount")] [AccountDialog]
public string? DestinationAccountCode { get; set; }

[Caption("DestinationSubconto1")] [SubcontoEditor(nameof(DestinationAccountCode), 0)]
public Guid? DestinationSubconto1 { get; set; }
// ... DestinationSubconto2-5 the same way ...
```

- **`[AccountDialog]`** (`Kandra.Attributes.Editors`) — modal tree picker over the whole chart
  (`AccountDialogSelect`). **`[AccountSearch]`** — autocomplete-by-code/name instead (`AccountSearchSelect`).
  Both take an optional `NodeSelection` (default `Leaves`, matching every other hierarchical-style selector in
  this repo) and — unlike `[Dropdown]`/`[Search]`/`[Dialog]` — take **no `SourceType` constructor argument**:
  there's exactly one chart of accounts per running configuration
  (`IChartOfAccountsService.Chart`), not a per-field choice of source Dto.
- **`[SubcontoEditor(accountPropertyName, slotIndex)]`** (`Kandra.Attributes.Editors`) targets a `Guid?`
  property. `accountPropertyName` (via `nameof(...)`) names the sibling account property whose *chosen*
  account determines this slot's allowed TypeId(s) at render time; `slotIndex` (0-4) is this property's
  position in that account's `AccountLeafBase.SubcontoSlots` order.
- **Two independent groups on one Dto just work** — `AccountTransaction` has both `Source...`/`Destination...`
  groups; the grouping key is the account property name, so add a third group (a different account property +
  its own 1-5 subconto properties) the same way if a document ever needs one.
- Add every referenced resx key (`SourceAccount`, `SourceSubconto1`, ...) to your configuration's
  `*.Localization` project (English required, uk/ru optional) — same convention as every other captioned field in this repo.

## 2. What the generator actually emits — don't hand-write this, but know the shape when debugging

`Kandra.Generators.Shared.UiFieldDiscovery.BuildFormModel` groups every `[SubcontoEditor]`-attributed property
by its referenced account property name (`BuildSubcontoGroups` in
`Kandra.Generators.Ui.Blazor/FieldEditorEmitter.cs` re-derives the same grouping at emit time) and
`DocumentUiEmitter` emits **one composite call per group** instead of N+1 independent field calls. Real
generated output (`AccountTransactionDtoForm.g.cs`, `Generated.Net/` under `FamilyGarage.Client.MudBlazor`):

```csharp
form.AddAccountSubcontoEditor(Loc["SourceAccount"], () => Record.SourceAccountCode, v => Record.SourceAccountCode = v,
    global::Kandra.ClientLib.Common.Ui.UiSelectorKind.Dialog,
    new global::System.Func<global::System.Guid?>[] { () => Record.SourceSubconto1, () => Record.SourceSubconto2, () => Record.SourceSubconto3, () => Record.SourceSubconto4, () => Record.SourceSubconto5, },
    new global::System.Action<global::System.Guid?>[] { v => Record.SourceSubconto1 = v, v => Record.SourceSubconto2 = v, v => Record.SourceSubconto3 = v, v => Record.SourceSubconto4 = v, v => Record.SourceSubconto5 = v, },
    nodeSelection: global::Kandra.ClientLib.Common.Ui.UiNodeSelection.Leaves);
// ... a second AddAccountSubcontoEditor call for DestinationAccount/DestinationSubconto1-5 ...
```

If your new fields *aren't* grouping into one call (each renders as an independent Guid field instead), check
the generator diagnostics first — three validation rules, each a distinct KANDRAUI code, "skip rather than emit
broken" on failure:
- **KANDRAUI007** — the named account property doesn't exist, or doesn't carry `[AccountSearch]`/`[AccountDialog]`.
- **KANDRAUI008** — more than one editor kind in the same group (mixing `[SubcontoEditor]` and
  `[SubcontoMultiSelectEditor]` pointing at the same account property).
- **KANDRAUI009** — slot indices within one group aren't unique and contiguous from 0 (e.g. you used 0, 1, 3
  and skipped 2).

## 3. Runtime dispatch — what actually renders, and why

`AccountSubcontoEditor.razor` (`Kandra.ClientLib.MudBlazor/Shared/Components/Selectors/`, engine-level, one
implementation covers every configuration) is the component `AddAccountSubcontoEditor` opens. On every account
change it injects `ChartOfAccountsTreeCache` (a client-side singleton — the whole chart is a few dozen nodes,
fetched once per app session) to read that account's declared `AccountSubcontoSlotDto[]` (already carries each
slot's `MemberTypes[].TypeId`, no extra server round-trip needed) and feeds each slot's resolved TypeId to a
child `SubcontoValueSelect`, which dispatches — via `ITypeRegistry.ByTypeId[typeId]` + `DynamicComponent` — to
the right concrete picker: `HierarchicalDictionaryDialogSelect<,>`/`DictionaryDialogSelect<,>` (Dictionary
kind), `DocumentDialogSelect<,>` (Document kind), or `EnumGuidSelect<>` (Enum kind). Renders nothing when
`TypeId` is null.

**On account change, every slot value clears** (`SlotSetters[i](null)` for every slot) — but *not* on the
component's very first parameter-set, so loading an already-saved document doesn't wipe its persisted
account+subconto values. If you touch this file: `[CascadingParameter] FormSaveContext? SaveContext` feeds
`EffectiveReadOnly => ReadOnly || SaveContext?.ReadOnly == true`, explicitly threaded to every child selector —
the composite isn't itself a `MudBaseInput<T>`, so it doesn't get the generator's usual ambient-ReadOnly
propagation for free; without this explicit read it would render editable even on a read-only View page.

## 4. Composite ("OneOf") slots — an extra picker, and a real limitation

A slot with more than one declared `MemberTypes` (e.g. account 68's `Subconto.OneOf<Waybill, GoodsReceipt>()`
slot) gets an extra small `MudSelect` ("Subconto N type") letting the user choose which member type applies —
only then does the real value selector (typed to whichever member was picked) appear. Choosing a different
member type clears whatever value the slot held under the previous one.

**Known, deliberate limitation — the chosen member type never reaches the server.** Only the raw `Guid?` value
round-trips through the Dto; there's no paired "which type did they mean" field. Server-side, the untyped
`AccountWithSubcontoBuilder.With(params Guid?[])` this document's behavior calls (see `post-to-accounting`
skill §5) **throws** if it hits a composite slot with an actual non-null value — deliberately rejected rather
than silently guessing wrong. If you're the one fixing this: `With(params SubcontoSlot?[])` (already built,
no live caller yet — see `post-to-accounting` skill §5's table) is the server-side half of the fix; the
missing half is widening the Dto to carry a paired TypeId per slot (a `[SubcontoEditor]` binding two
properties instead of one — a real attribute/generator change, discuss the shape before building it).

## 5. Multi-select variant — generic infrastructure, no live consumer yet

`[SubcontoMultiSelectEditor(accountPropertyName, slotIndex)]` (targets `IReadOnlyList<Guid>?`) +
`AccountSubcontoMultiSelectEditor.razor` + `SubcontoValueMultiSelect.razor` — the report-filter-shaped sibling
of everything above, same grouping contract. Built and demoed (`/dev/selector-demo`) but **not wired into any
real report** — if you're adding account+subconto filter fields to a report, this is already-built
infrastructure to reuse, not something to build from scratch; only the report Dto's own fields and query logic
need adding.

## 6. Try it without building a document first

`/dev/selector-demo` (admin-only, `FamilyGarage.Client.MudBlazor/Pages/Dev/SelectorDemo.razor`) has a working
single- and multi-select composite editor wired to real chart accounts (68, 28), plus all four
`SubcontoValueSelect` TypeKind branches individually. Per this repo's convention (don't delete dev artifacts
proactively), this page stays — use it to sanity-check a new `[SubcontoEditor]` field's dispatch before wiring
a whole document/report around it.

## 7. Verify

`dotnet build FamilyGarage.slnx`, check the generated `{Name}DtoForm.g.cs` has one `AddAccountSubcontoEditor` call
per group (not N+1 independent field calls) — if the Blazor client is already running, a `preview_stop`/
`preview_start` restart is needed to pick up the rebuilt client bundle (a plain rebuild alone regenerates the
`.g.cs` but the already-running dev server keeps serving the old compiled bundle). Then live in the browser:
pick different accounts and confirm the right number/kind of subconto fields render per account, confirm
switching accounts clears previously-set values, and confirm a composite slot's type-picker appears before its
value selector.
