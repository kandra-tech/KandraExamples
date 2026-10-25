---
name: create-document
description: Add a new document (a transactional entity that posts/submits and writes register movements — orders, receipts, transfers, and similar) to a Kandra configuration. Use when the user asks to add a new document, transaction type, or posting entity.
---

# Creating a document

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

A document is the full-featured entity kind: persisted, has a form + grid, usually tabular line sections, and
posts/submits — writing register movements (and/or chart-of-accounts ledger transactions) when submitted and
reversing them when unsubmitted (see [`reference.md`](reference.md) for the full conceptual explanation — base
classes, every relevant attribute's real signature, the behavior/register-write contract, numbering, and two
fully worked examples, all self-contained so you don't need to go read engine source to use this skill). This
recipe is written against **this repo's one reference configuration, `FamilyGarage`** — its project names
(`FamilyGarage.Domain`, `FamilyGarage.Forms`, ...) stand in for "your configuration's own equivalent project";
substitute your configuration's own root name if you're working in a different one.

**Before you create one:** a document is an *event on a date*. Anything that must be *entered as history* (a rate or
price valid from a date, an opening balance, a plan) is also a document: "set rates" with a line per currency, "opening
balances" with a line per account, "budget" with a line per category - never a dictionary row or a field on a dictionary
(see [`plan-configuration`](../plan-configuration/reference.md), sections 2, 3 and 5). **Posting contract:** `OnSubmitAsync`
must write the document's *complete* record set to each register on every call (the writer diffs it in place), because
the engine calls it again whenever a posted document is saved; `OnUnsubmitAsync` and `OnDeleteAsync` unpost/delete by the
document id. That one rule is all "edit = reverse and repost" needs. What a
movement stores in a text detail (`Comment`) is the document's own user-typed text or nothing: never a literal written by
code ("Opening balance"), because stored text cannot be translated; a report shows the document type instead. Prove it with a test (`test-configuration`, in a
configuration that ships it) rather than by clicking.

Copy-paste `.cs` skeletons for every step below live in [`data/`](data/) (`data/README.md` explains the
`{{Placeholder}}` tokens) — start from those rather than from a blank file.

**A document spans three separate class-library projects**, not one file: the EF entity (+ optional line
entity) lives in `{Config}.Domain`, the Dto+Validator pair (the one plane the Blazor WASM client also sees)
lives in `{Config}.Forms`, and the server-only Behavior+Mapper pair lives in `{Config}.Application`.
`reference.md`'s intro explains why the split exists and what each project may/may not reference — same rule
as `create-dictionary`.

**As of this writing, fewer supporting pieces need hand-writing than for a dictionary — and one big one you
might expect to hand-write doesn't need it at all.** Beyond the four generator-discovered core files (steps
2–4 below), plan on: a new type-id constant file (step 1) and a new numbering-bucket constant file (step 4) —
each its own file, never an edit to the shared `FamilyGarageTypeIds.cs`/`NumberingBuckets.cs` — resx key
edits (step 6, a larger key set than a dictionary's), a new hand-written Refit client interface (step 7), and a
nav entry edit (step 8). **Unlike a dictionary, there is no Blazor UI to write** — a document's List/Add/
Update/View/Form pages, Submit/Unsubmit buttons, and Print button are 100% generated from the Dto's attributes
(`reference.md` §12). If you've built a dictionary before and are expecting a "copy the closest `.razor` page
set" step here, skip it — that step doesn't exist for documents anymore. Skipping any of the steps that do
remain still compiles and passes startup DI validation — it just leaves the feature invisible or broken in the
browser. See `reference.md`'s "Hand-maintained files" table for the full list with exact paths and why each is
manual.

## 1. Mint a type identity

Generate a fresh GUID and copy `data/TypeIdConstant.cs` to `{Config}.Enums/FamilyGarageTypeIds/FamilyGarageTypeIds.{Name}.cs`.
**Never add the constant into `FamilyGarageTypeIds.cs` itself** — that file is `partial` and permanently empty, same
convention `create-dictionary` uses (documents already share the same `FamilyGarageTypeIds/` folder — nothing to set up,
just add one more sibling file). Never reuse an existing constant's value. See `reference.md` §7.

## 2. Decide with-lines vs. lineless, then write the Domain entity

Most documents have at least one tabular line section (order lines, movement lines) — use
`data/Domain.WithLines.cs`. A document with no line section at all (rare — a single manual posting entry like
`AccountTransaction`) uses `data/Domain.Lineless.cs` instead, which hand-implements
`IEntityWithRows<T>.RowNavigations`/`RemoveOrphans` as no-ops since there's no `[KandraTablePart]` property for
the generator to work from. Copy the matching template to
`{Config}.Domain/Entities/Documents/{Name}.cs`, fill in the placeholders, add header/line fields. Decide whether
the entity implements `ISubconto` — only if this document itself is a valid posting/analytical dimension
(e.g. a "settlement document" reference); most internal documents (transfers) just implement
`IEntityWithTypeId`. `reference.md` §1/§7/§10 has the full base-class shapes and both worked examples.

## 3. Dto + Validator

Add `[Help("configuration/documents/{name}")]` to the form's Dto when the documentation site has a page for it: the generated pages then show a (?) button next to the title that opens that page in the user's language (a relative path is appended to `Kandra:Client:HelpUrl`/`HelpUrls`; an absolute http(s) URL is used as-is). No attribute, no button. On a hierarchical dictionary it goes on the abstract root Dto.

Copy `data/Forms.WithLines.cs` or `data/Forms.Lineless.cs` (matching step 2's choice) to
`{Config}.Forms/Documents/{Name}.cs`, fill in placeholders and fields — header `[Caption]` +
`[Search]`/`[Dropdown]`/`[Required]`/`[ListFormColumn]`, line fields use `[GridColumn]` instead of
`[ListFormColumn]` (distinct attribute — `reference.md` §2 has every attribute's exact signature, including the
document-specific cross-cutting ones: `[SupportedPrintForm]`, `[UiBehavior]`, `[SourceFor]`,
`[RegisterTransactions]`/`[AccountTransactions]`, each pointing at its own dedicated skill). Also fill in the
real `[Description]` on the header Dto class, on the line Dto class, and on any field whose meaning isn't
obvious from its name/caption alone (mandatory on both classes — `reference.md` §2); this is what the MCP
server's `describe_entity_type`/`list_entity_types` tools surface to an AI agent, `[Caption]` is only a UI
label, not an explanation. Add every referenced resx key to your configuration's `*.Localization` project's
resx files (English required, uk/ru optional; see [`localize-configuration`](../localize-configuration/SKILL.md)), including the
three keys nothing in the Dto references directly (`Add{Name}` — the List page's "add" button —
`{Name}AddedSuccessfully`/`{Name}UpdatedSuccessfully` — read straight off the Dto's name by the generated
List/Add/Update pages, `reference.md` §9) — never use `isLiteral: true` to skip that step.

Each tabular section on the Dto is an `ICollection<TLine>` property marked
`[TablePart]` (`Kandra.Attributes.Layout`) — any name, and a document may have several (they render as one grid each,
in declaration order, interleaved with the header fields by where you declare them — a field declared after the
collection appears below its grid). Give each a `[Caption]` for its visible heading, and — when a document
has more than one — an `AddCaption` resx key so the buttons differ (`[TablePart(AddCaption = "AddSettlement")]`;
defaults to `AddLine`). A Dto with **no** `[TablePart]` property keeps working through the legacy convention (one
collection literally named `Lines`), but an `ICollection<TLine>` that is neither marked nor named `Lines` is not
rendered at all — the generator warns `KANDRAUI011`, so watch the build output. To split a long header into tabs or titled sections, or to give a grid its own tab, use `[FormTab]`/`[FormSection]` on the Dto and
`[TabRef]`/`[SectionRef]` on the properties (`[TabRef]` works on a `[TablePart]` too) — see `reference.md` and the
`create-dictionary` reference for the rules; each tab/section caption is a resx key. Everything a form does on its own while it is open - recalculating a row sum or a document total, clearing or filling a dependent field, showing or hiding a field, a grid, a section or a tab depending on another field (say, a counterparty only for some operations, or one grid per referenced document type), a server lookup, UI-only data the form fetched (an item's category) - lives in **one UI behavior class**: `[UiBehavior(typeof(XxxUiBehavior))]` on the header Dto, the class in the same file deriving from `DocumentUiBehavior<XxxDto>`, and `[OnChange]`/`[Visible]`/`[OnLineRemove]`/`Visible =` naming its methods; the validator states its own conditions and never references it. See the `create-ui-behavior` skill and `reference.md` ("UI behavior"). 

**A document that prints** declares `[SupportedExportFormats(ExportFormat.Pdf)]` + `[SupportedPrintForm(typeof({Name}PrintForm))]`
on its Dto and carries the print form in the same file: a `{Name}PrintForm : DocumentPrintForm` built on roles and
types (parties, a table, `VatMode`/`Taxes`/`Total`/`CurrencyCode`, signatures), with no sections or styles. The
commented skeleton at the end of `data/Forms.WithLines.cs` and the `OnPrintAsync` skeleton in
`data/Application.WithLines.cs` are the starting point; the `create-print-form` skill has the vocabulary, the draft
and VAT rules, and the diagnostics.

## 4. Behavior + Mapper (+ numbering bucket)

Copy `data/Application.WithLines.cs` or `data/Application.Lineless.cs` to
`{Config}.Application/Documents/{Name}.cs`. If this document auto-numbers its `Code` (nearly all do), also copy
`data/NumberingBucketConstant.cs` to `{Config}.Domain/Numbering/NumberingBuckets.{Name}.cs` — same rule as
step 1: **never add the constant into `NumberingBuckets.cs` itself**. Every current document's `OnNewAsync`
uses a year-suffixed bucket key (`$"{NumberingBuckets.{Name}}:{yy}"`) so numbers reset per calendar year —
`reference.md` §8. Write `OnSaveAsync` for any server-authoritative recompute (line `Sum`, header `Total` —
never trust a client-submitted calculated value). If this document writes a register and/or posts to
accounting when submitted, write `OnSubmitAsync`/`OnUnsubmitAsync`/`OnDeleteAsync` using the `ISubmitScope
registers` parameter — **the only legal way to reach a register writer or the posting service**
(`reference.md` §6); swap in whichever register(s) actually apply, or see `create-register`/`post-to-accounting`
if the register/posting wiring doesn't exist yet. To also notify someone from a hook (e.g. a document needs a follow-up), see
`send-notifications` — it's a plain injectable service, not part of `ISubmitScope`. A behavior that neither moves a register nor posts to
accounting is still valid — just leave those three hooks at their no-op default, but the class and its
`[KandraDocumentBehavior]` attribute must still exist. So must the `{Name}Mapper` Mapperly stub in the same file
(`IEntityMapper<{Name}, {Name}Dto>`, plus the private line pair for a line-bearing document) — without it the
build fails with `KANDRAGEN014`. Keep the **line pair** of the mapper
(`ToDto({Name}Line)` with `RequiredMappingStrategy.Target`, `ToEntity({Name}LineDto)` with `Source`) exactly as in the
skeleton: those strategies are what let a line entity carry navigations (`Account`, `Currency`) that its Dto lacks.
Without them Mapperly reports `RMG012`/`RMG020` for every navigation, and `[MapperIgnore*]` on each is the wrong fix.
Keep the **`{Name}QueryDtoValidator`** at the end of the skeleton too (`PagedQueryDtoValidator<{Name}QueryDto>`): the
list service needs it, and without it the build and startup stay green while opening the list fails with
"No service for type IValidator<{Name}QueryDto>" (`ServiceResolutionTests` in a scaffolded configuration catches it). Keep the Dto's `Lines` initialized (`= [];`): Mapperly maps a
null collection as-is, so a payload that omits it would otherwise fail on `ToEntity`.

Three traps in the behavior that compile cleanly and fail only at runtime:

- **Hook signatures are copied, never retyped.** Every `IDocumentBehavior<T>` hook has a default no-op body, so a method
  whose signature differs from the interface's is just an extra method the engine never calls: no error, no warning. The
  document's `OnNewAsync` is `OnNewAsync({Name} entity, DateTime? date, Guid? sourceDocumentId, CancellationToken ct)`;
  the dictionary-shaped `OnNewAsync(entity, ct)` compiles on a document and leaves every new document without a number
  and a date. The same goes for an invented name (`OnPostAsync`). Keep the skeleton's signatures, and check a new document
  gets its `Code` (`test-configuration`: `NewAsync` returns it).
- **Refuse a post with `InvalidStateException`, worded through the localizer**:
  `throw new InvalidStateException(localizer["Refuel_OdometerBelowPrevious", entity.Odometer, previous].Value);` (inject
  `IStringLocalizer`). The API answers 409 and the form shows the message. `InvalidOperationException` (or any exception the
  engine does not know) is a **500** with a stack trace in Development and a generic error in production; English text in
  an exception the user reads is untranslated text. A check shared by several documents (an odometer that may not go
  down) is one helper method, not five copies.
- **Code never finds a dictionary row by its name.** `categories.Where(c => c.Name == "Fuel")` breaks the moment a user
  renames the row, enters it in another language, or runs an installation without the demo data (the row then exists
  nowhere). When posting needs a particular entry, pick one: a field on the document the user fills (defaulted in
  `OnNewAsync` or by the UI behavior), a constant that points at the entry (`Constant<Guid?>`, set on a settings form), or,
  when the set is closed and fixed by the domain, an enum instead of a dictionary (`plan-configuration`, red flags).

## 5. Nothing else to register by hand

Same as dictionaries: entity/Dto/Behavior discovery, EF configuration, and DI wiring are all generated
(`Kandra.Generators.EfConfig`, `.Di.Application`, `.Di.Persistence`) purely from the attributes above. Do
**not** hand-write an `EntityConfigurations/*.cs` file or a manual `services.AddDocument<...>()` call.

## 6. EF migration

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

## 7. Refit client interface (hand-written — no generator for this piece yet)

Copy `data/ClientLib.cs` to `{Config}.ClientLib.Common/ApiClients/I{PluralName}ApiClient.cs`, fill in the
placeholders. `[PathPrefix]` must match the `Name` you set on `[KandraDocumentForm(Name = "...")]` in step 3
exactly. The body is empty for the common case — `IDocumentApiClient<>` (extended transitively through
`IEntityApiClient<>`) already declares the full CRUD + document-lifecycle set (Submit/Unsubmit/Print/register-
transactions/account-transactions/links). `reference.md` §11 has the full method list.

## 8. Nav entry (hand-edited — no Blazor UI to write, unlike a dictionary)

Add a `InterfaceNodeDefinition.Entity(typeof({Name}Dto))` node into your configuration's Interface tree
(`{Config}.Application/Navigation/*Interfaces.cs`) — see `add-interface-node` if this is unfamiliar. **That's
the only UI-adjacent step for a document.** Unlike `create-dictionary`'s step 9, there is no
"copy the closest existing entity's page set" step here: List/Add/Update/View/Form pages, Submit/Unsubmit
buttons, and a correctly-gated Print button are all generated automatically from what you already declared in
step 3 (`reference.md` §12) — there is nothing under `Pages/Document/{Name}/` to create, and nothing under
`_HandWrittenReference/Document/*` is ever a template to copy from for a new document (it's a retired
pre-generator reference only).

## 9. Verify

`dotnet build FamilyGarage.slnx` (confirms generators picked everything up — check the generated
`{PluralName}Controller.g.cs` under your WebApi host's `Generated.Net/`, and the generated
`{Name}DtoAdd.g.cs`/`Update.g.cs`/`View.g.cs`/`List.g.cs`/`Form.g.cs` under your Client project's
`Generated.Net/Kandra.Generators.Ui.Blazor/Kandra.Generators.UiFormGenerator/`), `dotnet test FamilyGarage.slnx`,
then in the browser: create a document with lines (if applicable), Submit it, confirm the expected register
balance changed (e.g. via a Rests-style report for inventory-moving documents) and/or the expected ledger
entries appear (chart-of-accounts view, if this document posts), Unsubmit, confirm it reverted, and confirm
Delete is rejected while still posted.

## If something here doesn't match the engine

`reference.md` is a point-in-time snapshot of engine source, not a live view — if a build error contradicts a
signature it or these templates show, the code wins. Re-verify by grepping the type/attribute name under
`src/KandraCore/Kandra.Attributes/`, `Kandra.Domain/`, `Kandra.Forms/`, `Kandra.Application.Abstractions/`,
`Kandra.Validators/`, `Kandra.ClientLib.Common/ApiClients/`, or `Kandra.Generators.Ui.Blazor/`, then fix
`reference.md`/`data/` so the next person doesn't hit the same surprise.

## Optional last step: demo objects

If you ship demo data (`ImportPackages/*-demo.json`), add a few documents of the new type (after the dictionaries and stock they depend on)
to it: stable `$id`s, `$ref`/`$code` references, date tokens, a `version` bump, then a dry run. See
[`author-import-package`](../author-import-package/SKILL.md).
