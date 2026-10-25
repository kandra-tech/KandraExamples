---
name: convert-dictionary-to-hierarchical
description: Convert an existing flat dictionary (single class extends DictionaryBase) into a hierarchical one (folder/leaf tree, extends HierarchicalDictionaryBase) without losing its physical table. Use when the user asks to add grouping/folders/categories to a dictionary that already exists and already has data, as opposed to creating a brand-new hierarchical dictionary from scratch (see create-dictionary for that case).
---

# Converting a flat dictionary to hierarchical

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

This is a different task from `create-dictionary`'s hierarchical branch: that skill assumes the dictionary
doesn't exist yet, so there's no existing table, no existing rows, and no existing consumers to keep
working. Converting an **existing** flat dictionary means every one of those things is already there and
must keep compiling/working through the change. First hierarchical-from-flat conversion done in this repo:
`Item` (was flat, `FamilyGarage.Domain.Entities.Dictionaries.Item : DictionaryBase`) → `ItemNode`/`ItemFolder`/
`Item` — read those three files (Domain/Forms/Application) alongside `Counterparty` (the reference
hierarchical implementation `create-dictionary` already points at) before starting.

## 1. Naming: don't reuse `{Name}Item` if `{Name}` already means "item"

`create-dictionary`'s hierarchical convention names the abstract TPH root `{Name}Node` (e.g.
`CounterpartyNode`), not `{Name}Item` — that stutters whenever `{Name}` itself is a generic "item" word
(`ItemItem` for the `Item` dictionary). This repo's whole codebase was swept to this convention (including
renaming the pre-existing `CounterpartyItem` → `CounterpartyNode`) specifically so `Item`'s conversion
wouldn't be the odd one out — see `docs/hierarchical-dictionaries-architecture.md` §2.5 for the rationale.
Use `{Name}Node` for any future conversion too.

## 2. The class split (Domain/Forms/Application) — same shape as a from-scratch hierarchical dictionary

Follow `create-dictionary` §2–4 exactly: `{Name}` (the existing flat entity) becomes the **leaf** subclass of
a new abstract `{Name}Node : HierarchicalDictionaryBase`, plus a new `{Name}Folder : {Name}Node`
(`[KandraDictionaryFolder]`). Same split in Forms (`{Name}NodeDto`/`{Name}FolderDto`/`{Name}Dto`, three
validators) and Application (`IHierarchicalDictionaryBehavior<{Name}Node>` replacing
`IFlatDictionaryBehavior<{Name}>`, and the Mapperly mapper re-declared over the roots -
`IEntityMapper<{Name}Node, {Name}NodeDto>` with `[MapDerivedType]` for the folder and leaf pairs, copied from
`CounterpartyMapper`). The one thing to preserve carefully: the dictionary's **original** `TypeId` guid moves up to the new
`{Name}Node` (declare it there with `IEntityWithTypeId`, and delete the leaf's own `TypeId` property — it's
inherited now); in Forms, `[TypeId]` moves from `{Name}Dto` to `{Name}NodeDto`. Same guid, so posted subconto
values and change-log entries recorded before the conversion still match. The folder gets **no** TypeId of
its own. The leaf keeps any interfaces it already implements (e.g. `ISubconto`) — external code referencing
the concrete leaf type by name (report filters, `AccountLeaf<{Name}>` in a chart-of-accounts builder, document
row FKs) doesn't need to change, since the leaf class keeps its name.

## 3. Things that only bite you *because* the dictionary already existed

- **The EF migration is `AlterColumn`/`AddColumn`, not `CreateTable`.** `dotnet ef migrations add` against
  the already-mapped table adds `ParentId`/`Depth`/`Path`/`IsFolder` to the existing physical table. `Path`
  is non-nullable but EF will happily give it `defaultValue: ""` for pre-existing rows — **that default is
  not a valid materialized path** (a real one is `"/" + PathEncoding.EncodeSegment(ownId)` for a root row,
  computed in real C#, not portably expressible in migration SQL across SQLite/SqlServer/PostgreSQL). Decide
  explicitly with the user whether existing rows are disposable dev data (then just migrate and tell people
  to recreate their local dev DB) or need a real backfill (then a two-phase migration + a small idempotent
  C# startup routine using `PathEncoding.EncodeSegment`, not a data migration in the `.cs` migration file
  itself).
- **SQL Server needs an explicit collation pin on the new `Path` column**, or its usually-case-insensitive
  default collation silently corrupts `Path.StartsWith` prefix matching for descendant queries. Copy the
  one-line pattern from whichever provider `DbContext` subclass already has it for another hierarchical
  dictionary (search for `UseCollation("Latin1_General_BIN2")` in `*.Persistence.Databases/Contexts/
  *DbContextSqlServer.cs`) and add the equivalent line for the newly-converted entity. This is easy to miss
  entirely since nothing fails to compile or fails a SQLite-backed test without it.
- **Existing tests that construct the entity directly** (`new {Name} { ... }`, bypassing the CRUD service
  that normally fills in `Path`/`Depth`) will start failing a `NOT NULL constraint` on `Path` the moment the
  column stops having a workable implicit default in code (it's a C# non-nullable `string`, no
  auto-initialization). Grep the test projects for `new {Name} {` and add
  `Path = "/" + PathEncoding.EncodeSegment(theKnownId)` to each (root-level, unless the fixture is
  deliberately building a multi-level tree).
- **Any hand-written Blazor page or component that references the dictionary's old flat route or old
  service interface must be found and updated — this will not show up as a build error.** The obvious spot
  (`Pages/Dictionary/{Name}/*.razor`) gets rewritten anyway per `create-dictionary` §7 (copy
  `Pages/Dictionary/Counterparty/*.razor` as the template — tree popover, breadcrumbs, Up button, separate
  Add-item/Add-folder buttons). The non-obvious spots that silently keep the *old* shape and will crash or
  misbehave at runtime instead of failing to compile:
  - **The sidebar `NavMenu.razor` nav link.** Flat dictionaries route as `/dictionary/{name}/{Page:int}`
    (nav link literally `Href="dictionary/{name}/1"`); hierarchical ones route as
    `/dictionary/{name}/{FolderId:guid?}` (nav link `Href="dictionary/{name}"`, no trailing segment). A
    leftover `/1` link doesn't match the new route (`"1"` isn't a valid `Guid`) — Blazor's client-side
    router fails to resolve it, which surfaces as what looks like the whole WASM app crashing/restarting,
    not a helpful "route not found" message. **Grep `NavMenu.razor` for the old route literally before
    calling the UI rewrite done.**
  - **Any `*SelectSearch.razor` component** used by document line editors to pick a leaf value of this
    dictionary (e.g. `ItemSelectSearch.razor` for `GoodsReceipt`/`Waybill`/`Transfer` line items) —
    `@inject IFlatDictionaryLookupService<{Name}Dto>` / `.LookupItems()` becomes
    `@inject IHierarchicalDictionaryLookupService<{Name}NodeDto>` / `.LookupLeaves()`. Copy the pattern from
    `CounterpartySelectSearch.razor`. Verify by actually opening a document creation page in the browser and
    searching for the value in its autocomplete — this component's own project doesn't reference the
    `Pages/Dictionary/{Name}/` folder, so it's easy to overlook.

## 4. Verify

Same as `create-dictionary` §8, plus: after the Blazor rewrite, click the sidebar nav link for this
dictionary **from another already-loaded page** (client-side navigation, not a fresh browser `navigate()`)
— a fresh full-page load can mask a router crash that only reproduces on in-app SPA navigation. Then open a
document that references this dictionary as a line item and confirm the picker still resolves a leaf value.
