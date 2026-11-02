---
name: create-dictionary
description: Add a new dictionary (reference/master data — customers, items, warehouses, and similar lookup entities) to a Kandra configuration, flat or hierarchical. Use when the user asks to add a new dictionary, catalog, lookup, or master-data entity.
---

# Creating a dictionary

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

A dictionary is reference data: persisted, has a form + grid, optional hierarchy, no posting, no register
writes (see [`reference.md`](reference.md) for the full conceptual explanation — base classes, every relevant
attribute's real signature, behavior/validator contracts, numbering, and two fully worked examples, all
self-contained so you don't need to go read engine source to use this skill). This recipe is written against
**this repo's one reference configuration, `FamilyGarage`** — its project names (`FamilyGarage.Domain`,
`FamilyGarage.Forms`, ...) stand in for "your configuration's own equivalent project"; substitute your
configuration's own root name if you're working in a different one.

**Before you create one, check it really is a dictionary:** a dictionary is a *thing people refer to* (an account, a
category, a currency). A value that holds from a date (a rate, a price), a balance or any amount that results from
events is **not** a dictionary field: it is a register written by a document (see
[`plan-configuration`](../plan-configuration/reference.md), section 1). Entering history through a dictionary row
(`ExchangeRate`, `Price`) or keeping a running amount on the row (`OpeningBalance`, `Stock`) is the most common
modeling mistake.

Copy-paste `.cs` skeletons for every step below live in [`data/`](data/) (`data/README.md` explains the
`{{Placeholder}}` tokens) — start from those rather than from a blank file.

**A dictionary spans three separate class-library projects**, not one file: the EF entity lives in
`{Config}.Domain`, the Dto+Validator pair (the one plane the Blazor WASM client also sees) lives in
`{Config}.Forms`, and the server-only Behavior+Mapper pair lives in `{Config}.Application`. `reference.md`'s
intro explains why the split exists and what each project may/may not reference.

**As of this writing, several supporting pieces still have to be hand-written — nothing generates them.** Beyond
the four generator-discovered core files (steps 3–5 below), plan on: a new type-id constant file (step 2) and a
new numbering-bucket constant file (step 5) — each its own file, never an edit to the shared `FamilyGarageTypeIds.cs`/
`NumberingBuckets.cs`, which are `partial` and permanently empty precisely so this step is always additive —
resx key edits (step 4), a new hand-written Refit client interface (step 8), and a nav entry edit (step 9). **There is no
Blazor page to write**: the list, add, edit and view pages are generated from the Dto. Skipping any of these still compiles and passes startup DI
validation — it just leaves the feature invisible or broken in the browser. See `reference.md`'s
"Hand-maintained files" table for the full list with exact paths and why each one is manual.

## 1. Decide flat vs. hierarchical

- **Flat** (most dictionaries: `Branch`, `Currency`, `Warehouse`) — single class extends `DictionaryBase`
  directly. Use `data/Domain.Flat.cs` + `data/Forms.Flat.cs` + `data/Application.Flat.cs` +
  `data/ClientLib.Flat.cs`.
- **Hierarchical** (`Counterparty`, `Item`) — needed when the dictionary should support folder-style
  grouping (customers-in-a-tree, product categories, ...). This is meaningfully more machinery (a TPH
  root/Folder/Leaf split, `Depth`/`Path` materialized-path columns, cycle-prevention on move) — reach for
  it whenever the data has a parent/child shape (a category with a parent, a group of items): do not fake a tree with a
  `ParentId` on a flat dictionary. A selector on a document field picks leaves by default; add
  `NodeSelection = HierarchicalNodeSelection.Any` (or `Folders`) to its `[Search]`/`[Dialog]` when a folder may be chosen.
  It is more machinery than a flat one, so do not use it for a plain list. Read
  `docs/hierarchical-dictionaries-architecture.md` and `reference.md`'s Counterparty walkthrough end to end
  before starting one. Use `data/Domain.Hierarchical.cs` + `data/Forms.Hierarchical.cs` +
  `data/Application.Hierarchical.cs` + `data/ClientLib.Hierarchical.cs`. **Converting an already-existing flat
  dictionary to hierarchical** (rather than creating a new hierarchical one from scratch) is a different task
  with its own pitfalls — see
  [`convert-dictionary-to-hierarchical`](../convert-dictionary-to-hierarchical/SKILL.md) instead of this
  skill for that case.

**A hierarchical dictionary changes how the rest of the code refers to it** - the frictions an agent hit:

- Its form, Refit client and mapper work on the abstract **root** Dto/entity: `{Name}NodeDto` / `{Name}Node` (folder:
  `{Name}FolderDto`, leaf: `{Name}Dto`).
- A reference to it from another Dto points at the root: `[Search(typeof({Name}NodeDto))]` (a leaf Dto type there finds
  nothing); add `NodeSelection = HierarchicalNodeSelection.Any` when a folder may be picked.
- Its server CRUD service is `IHierarchicalDictionaryCrudService<{Name}NodeDto, {Name}QueryDto>`, not
  `IDictionaryCrudService<,>`; insert a folder by passing a `{Name}FolderDto`, a leaf by passing a `{Name}Dto` (set
  `ParentId` to the folder's id).
- A report that reads leaves from the database filters by type (`dbContext.Set<{Name}Node>().OfType<{Name}>()`).

The rest of this checklist applies to both shapes; where they diverge, the note says so and `reference.md`
has the detail.

## 2. Mint a type identity

Generate a fresh GUID and copy `data/TypeIdConstant.cs` to
`{Config}.Enums/FamilyGarageTypeIds/FamilyGarageTypeIds.{Name}.cs` (hierarchical: still one file — the dictionary has a single
TypeId, declared on the abstract Node and inherited by folder and leaf). **Never add the constant into `FamilyGarageTypeIds.cs` itself** — that file is `partial` and permanently
empty; every constant is its own sibling file precisely so this step never edits a file another in-flight
dictionary might also be touching. Never reuse an existing constant's value. See `reference.md` §6 for why
this centralization exists and how both planes reference the same constant.

## 3. Domain entity

Copy `data/Domain.Flat.cs` or `data/Domain.Hierarchical.cs` to
`{Config}.Domain/Entities/Dictionaries/{Name}.cs`, fill in the placeholders, add domain-specific fields
(nullable for anything optional). Decide whether the entity (leaf, for hierarchical) implements `ISubconto` —
only if it's a valid posting/analytical dimension for the chart-of-accounts subsystem; skip it for anything
purely structural. `reference.md` §1/§6/§9 has the full base-class shapes and the Currency/Counterparty
variants (hand-typed `Code`, narrower `[MaxLength]`, TPH folder/leaf split).

## 4. Dto + Validator

Add `[Help("configuration/dictionaries/{name}")]` to the form's Dto when the documentation site has a page for it: the generated pages then show a (?) button next to the title that opens that page in the user's language (a relative path is appended to `Kandra:Client:HelpUrl`/`HelpUrls`; an absolute http(s) URL is used as-is). No attribute, no button. On a hierarchical dictionary it goes on the abstract root Dto.

Copy `data/Forms.Flat.cs` or `data/Forms.Hierarchical.cs` to `{Config}.Forms/Dictionaries/{Name}.cs`, fill in
placeholders and fields (`[Caption]` + `[Search]`/`[Dropdown]`/`[Required]`/`[ListFormColumn]` per field —
`reference.md` §2 has every attribute's exact signature). Also fill in the real `[Description]` on the Dto
class (mandatory — hierarchical: on the abstract root and the folder Dto) and on any field whose meaning isn't
obvious from its name/caption alone; this is what the MCP server's `describe_entity_type`/`list_entity_types`
tools surface to an AI agent, `[Caption]` is only a UI label, not an explanation. Add every referenced resx key
(`Edit{Name}`/`Create{Name}`/`View{Name}`/`{PluralName}`/`Nav_{PluralName}`, plus every field `[Caption]`, plus the three
keys the generated List/Add/Update pages read straight off the Dto's name: `Add{Name}` (the List page's add button —
missing, it renders as the raw upper-cased key), `{Name}AddedSuccessfully` and `{Name}UpdatedSuccessfully`) to
your configuration's `*.Localization` project's resx files (English required, uk/ru optional; see [`localize-configuration`](../localize-configuration/SKILL.md)) — never use `isLiteral: true` to skip
that step (`reference.md` §8). `Code`/`Name` are `virtual` on the base — only override one with its own
`[Caption]`/`[ListFormColumn]` if the generic label is misleading for this entity (see Currency's
`[Caption("CurrencyCode")]` in `reference.md` §9); most dictionaries inherit both unmodified.

## 5. Behavior + Mapper

Copy `data/Application.Flat.cs` or `data/Application.Hierarchical.cs` to
`{Config}.Application/Dictionaries/{Name}.cs`. If this entity auto-numbers its `Code` (most do; a
hand-typed-code dictionary like `Currency` skips this entirely — `reference.md` §7), also copy
`data/NumberingBucketConstant.cs` to `{Config}.Domain/Numbering/NumberingBuckets.{Name}.cs` (hierarchical: two
files, one per bucket) — same rule as step 2: **never add the constant into `NumberingBuckets.cs` itself**,
that file is `partial` and permanently empty. A no-op behavior body is fine, but the class and its
`[KandraDictionaryBehavior]` attribute must still exist — the engine fails DI validation at startup without a
behavior for every dictionary. The `{Name}Mapper` class in the same file is required too: it's a hand-written
Mapperly `[Mapper]` stub implementing `IEntityMapper<,>` (Mapperly generates the bodies; an unmapped member is a
build error), and the build fails with `KANDRAGEN014` if it's missing.
Keep the **`{Name}QueryDtoValidator`** at the end of the skeleton (`PagedQueryDtoValidator<{Name}QueryDto>`): the list
service needs it, and without it the build and startup stay green while opening the list fails with "No service for
type IValidator<{Name}QueryDto>" (`ServiceResolutionTests` in a scaffolded configuration catches it). Never put mapping attributes on the
entity or the Dto - all mapping configuration lives on the mapper class.

## 6. Nothing else to register by hand

Entity/Dto/Behavior discovery, EF configuration, and DI wiring are all generated
(`Kandra.Generators.EfConfig`, `.Di.Application`, `.Di.Persistence` — see
[`src/KandraCore/CLAUDE.md`](../../../../KandraCore/CLAUDE.md)) purely from the attributes above. Do **not** hand-write an
`EntityConfigurations/*.cs` file or add a manual `services.AddDictionary<...>()` call.

## 7. EF migration

```bash
cd src/FamilyGarage.Persistence.Databases   # or your configuration's equivalent *.Persistence.Databases project
dotnet ef migrations add Add{Name} --project . --startup-project ../FamilyGarage.WebApi/FamilyGarage.WebApi.csproj --context FamilyGarageDbContextSqlite     --output-dir Migrations/Sqlite
dotnet ef migrations add Add{Name} --project . --startup-project ../FamilyGarage.WebApi/FamilyGarage.WebApi.csproj --context FamilyGarageDbContextSqlServer  --output-dir Migrations/SqlServer
dotnet ef migrations add Add{Name} --project . --startup-project ../FamilyGarage.WebApi/FamilyGarage.WebApi.csproj --context FamilyGarageDbContextPostgreSql --output-dir Migrations/PostgreSql
```

All three providers must be regenerated together, even if you'll only run against SQLite locally. Besides the
new migration file itself, this command also rewrites each provider's existing
`Migrations/{Provider}/FamilyGarageDbContext{Provider}ModelSnapshot.cs` — expect that diff, it's `dotnet ef`'s own
output, never hand-edit it yourself (same rule as the `Designer.cs` files).

## 8. Refit client interface (hand-written — no generator for this piece yet)

Copy `data/ClientLib.Flat.cs` or `data/ClientLib.Hierarchical.cs` to
`{Config}.ClientLib.Common/ApiClients/I{PluralName}ApiClient.cs`, fill in the placeholders. `[PathPrefix]` must
match the `Name` you set on `[KandraDictionaryForm(Name = "...")]` in step 4 exactly. For the common case the
body is empty — the shared `IDictionaryApiClient<>`/`IHierarchicalDictionaryApiClient<>` base (extended
transitively through `IEntityApiClient<>`) already declares the full CRUD set; Refit's own source generator
implements the interface at compile time, you only write the declaration. `reference.md` §10 has both base
interfaces' exact method signatures.

## 9. Nav entry (the UI itself is generated)

**Do not write Razor pages.** The list, add, edit and view pages of a dictionary (flat or hierarchical) are generated
into the client project's `Generated.Net/` from the Dto's attributes (`Kandra.Generators.Ui.Blazor`); if a page does
not look right, change the Dto's attributes. The one UI step is a navigation entry: an
`InterfaceNodeDefinition.Entity(typeof({Name}Dto))` node in `{Config}.Application/Navigation/*Interfaces.cs` (see
[`add-interface-node`](../add-interface-node/SKILL.md)); its caption is the Dto's `[NavigationName]` resx key.

## 10. Verify

`dotnet build FamilyGarage.slnx` (confirms generators picked everything up — check the generated
`{PluralName}Controller.g.cs` exists under your WebApi host's `Generated.Net/`), `dotnet test FamilyGarage.slnx`, then
run the app and exercise create/edit/list/delete in the browser.

## If something here doesn't match the engine

`reference.md` is a point-in-time snapshot of engine source, not a live view — if a build error contradicts a
signature it or these templates show, the code wins. Re-verify by grepping the type/attribute name under
`src/KandraCore/Kandra.Attributes/`, `Kandra.Domain/`, `Kandra.Forms/`, `Kandra.Application.Abstractions/`,
`Kandra.Validators/`, or `Kandra.ClientLib.Common/ApiClients/`, then fix `reference.md`/`data/` so the next
person doesn't hit the same surprise.

## Optional last step: demo objects

If you ship demo data (`ImportPackages/*-demo.json`), add a few rows of the new dictionary
to it: stable `$id`s, `$ref`/`$code` references, date tokens, a `version` bump, then a dry run. See
[`author-import-package`](../author-import-package/SKILL.md).
