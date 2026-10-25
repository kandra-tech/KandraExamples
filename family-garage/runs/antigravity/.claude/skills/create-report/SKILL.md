---
name: create-report
description: Add a new report (a parameterized, read-only query with optional CSV/PDF/Excel export) to a Kandra configuration. Use when the user asks to add a new report, listing, or export screen.
---

# Creating a report

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

A report is a parameterized read-only query: a filters Dto, a result grid, optional CSV/PDF export — see
[`reference.md`](reference.md) for the full conceptual explanation (every attribute's real signature, the
`IReportBehavior<,>`/`ReportResult<,>` contracts, `[Aggregate]`/`ICustomAggregate`, and three fully worked
examples, all self-contained so you don't need to go read engine source to use this skill). This recipe is
written against **this repo's one reference configuration, `FamilyGarage`**; substitute your own configuration's
root project names if you're working in a different one.

Copy-paste `.cs` skeletons for every step below live in [`data/`](data/) (`data/README.md` explains the
`{{Placeholder}}` tokens) — start from those rather than a blank file.

**A report spans two class-library projects, not three** — there's no Domain-plane entity to write, since a
report has nothing to persist. The Dto+Validator pair (the one plane the Blazor WASM client also sees) lives in
`{Config}.Forms`, the Behavior+Mapper pair lives in `{Config}.Application`. `reference.md`'s intro explains why.

**The Blazor UI needs no hand-written step at all — this is the one real difference from `create-dictionary`/
`create-document`.** `Kandra.Generators.Ui.Blazor`'s `ReportUiEmitter` is a real, working generator (not a
stub) that emits the whole report page — filters, an extended-filters panel, export buttons, a MudDataGrid
result grid with sort/filter/group/aggregate — straight from the attributes you put on the Dtos in step 1.
Every hand-written report page that used to exist in this repo has already been retired to
`_HandWrittenReference/Report/{Name}/{Name}.razor` (reference only, excluded from compilation) — there is
nothing under `*.Client.MudBlazor/Pages/Reports/` to write or copy for a new report.

Concrete, currently-working examples, all in `FamilyGarage.Forms/Reports/` + `FamilyGarage.Application/Reports/`:
`RestsReport` (the primary one to read end to end — composite display columns, a built-in `[Aggregate]`, a
custom `ICustomAggregate`, `[MultiSelect]`/`[ExtendedFilter]`), `TransactionsReport` (adds `[DateRangeEditor]`
and a second `[Aggregate]` pair), `PriceListReport` (the minimal shape — a single required `[Dropdown]` filter
that gates whether the report runs at all, no aggregation).

## 1. Filters Dto + line Dto + print-data Dto + result Dto + Validator (`{Config}.Forms/Reports/{Name}.cs`)

Add `[Help("configuration/reports/{name}")]` to the form's Dto when the documentation site has a page for it: the generated pages then show a (?) button next to the title that opens that page in the user's language (a relative path is appended to `Kandra:Client:HelpUrl`/`HelpUrls`; an absolute http(s) URL is used as-is). No attribute, no button. On a hierarchical dictionary it goes on the abstract root Dto.

Copy `data/Forms.cs`, fill in placeholders, delete what you don't need. `reference.md` §1 has every attribute's
exact signature (`[KandraReportForm]`, `[ReportViewColumn]`, `[Aggregate]`/`AggregateKind`, `[ExtendedFilter]`,
`[ReportViewGrid]`, plus the editor attributes — `[Dropdown]`/`[Search]`/`[MultiSelect]`/`[DateEditor]`/
`[DateRangeEditor]`); §2 has the base interfaces (`IReportFiltersDto`, `ReportResult<TFilters,TData>`,
`ICustomAggregate`, `ReportGroupingState`); §3–§5 walk `RestsReport`/`TransactionsReport`/`PriceListReport` field
by field. Also fill in the real `[Description]` on the filters Dto class (mandatory — it's the one Dto type
MCP reflects for a report, `reference.md` §1) and on any filter whose meaning isn't obvious from its
name/caption alone; this is what the MCP server's `describe_entity_type`/`list_entity_types` tools surface to
an AI agent, `[Caption]` is only a UI label, not an explanation.

**A worth-knowing trap**: several of these attributes' own XML doc comments in the engine source claim they're
"currently unconsumed by any generator" — that was true before `ReportUiEmitter` existed and is stale now.
`reference.md` §1 calls out exactly which ones and how to re-verify against the generator source instead of
trusting the doc comment.

`[Aggregate(Kind = AggregateKind.Sum)]` (built-in) vs. `[Aggregate(CustomAggregateType = typeof(YourAggregate))]`
(implements `ICustomAggregate`, e.g. for a measure that shouldn't sum unless grouped by a specific column) — see
`reference.md` §2/§3 for the exact contract and why `RestsQuantityAggregate`/`TransactionsQuantityAggregate` both
guard on `groupingState.FrontGroup`. A required `Guid` filter (`[Required] [Dropdown(...)]`) automatically gates
the report from running until it's picked — no code to write for that, `ReportUiEmitter` generates the guard
(`reference.md` §5/§6).

**A matrix/cross-tab result** (one dimension down, another across, e.g. warehouses x item groups) is not a
hand-built grid: keep the line Dto "long" (one line per non-empty cell, with a row key, a column key and a
numeric value) and add `[PivotTable]` to `Lines` plus `[PivotRow]`/`[PivotColumn]`/`[PivotValue]` on the line
properties. The generated page, CSV and PDF all pivot it for you:

```csharp
public sealed class StockMatrixLineDto
{
    [Caption("Warehouse")] [ReportViewColumn]
    [PivotRow(CaptionProperty = nameof(WarehouseName))]
    public string WarehouseCode { get; set; } = "";
    public string WarehouseName { get; set; } = "";

    [Caption("ItemGroup")] [ReportViewColumn]
    [PivotColumn]
    public string ItemGroupCode { get; set; } = "";

    [Caption("Quantity")] [Format("N3")] [ReportViewColumn]
    [PivotValue]
    public decimal Quantity { get; set; }
}

// on the print-data Dto:
[Required] [PrintTable] [ReportViewGrid] [PivotTable]
public required ICollection<StockMatrixLineDto> Lines { get; set; }
```

`reference.md` §1 has the details.

The print-data Dto derives `ReportPrintForm` (the engine base that carries the generated-at/by audit pair, which goes to
the page footer, and defaults the page frame to `Internal`: a title and a parameters line, no letterhead). The
`[PrintForm]`/`[PrintTable]`/`[PrintColumn]` attributes aren't just decoration — `Kandra.Generators.Printing` reads
them to produce CSV/PDF export automatically. Echo the filters the user chose with `[PrintRole(PrintRole.Parameter)]`
properties (they become one muted "Period: … · Warehouse: …" line under the title), give columns that share a heading
`[PrintColumn(Group = …)]`, and set `[PrintTable(GroupBy = …)]` (over rows the behavior sorts by that property) when the
lines have a natural parent worth a group row and a subtotal. All of that, and a report that is a document sent to a
partner (letterhead, `ExportAsync`), is `create-print-form`'s scope, not this skill's — read it once the report's basic
shape (filters/lines/behavior) is working.

## 2. Repository query (`{Config}.Domain/Commons/IReportsRepository.cs` + `{Config}.Persistence/Repositories/ReportsRepository.cs`)

Add a method to the shared `IReportsRepository` interface (lives in `.Domain`, not `.Persistence` — so the
Behavior can depend on it without pulling in EF Core) and its `ReportsRepository` implementation, unless the
report genuinely doesn't fit an existing repository. Follow `GetRests`'s shape: `IQueryable`-friendly
composition, dimension/date filters applied server-side, projected straight into a plain (non-persisted,
non-attributed) result-row class under `{Config}.Domain/Entities/ReportingModels/`. `reference.md` §9 has the
full contract, including the live example of constructor-injecting register readers directly.

**Rules for the repository (each one was broken by an agent, so they are hard rules):**

- **EF Core and the register readers only. No raw SQL** (`SqlQueryRaw`, `FromSql*`, `ExecuteSql*`, `DbConnection`/ADO).
  The engine's tables (`Sys_*`, `Acc_*`, `App_*`) are not your schema and change between engine versions; raw SQL also
  bypasses the provider differences the three migrations exist for.
- **Constants are read in the behavior, not in the repository.** The repository project cannot see `IConstantsService`
  (it lives in Application), which is exactly what tempted an agent into querying `Sys_Constants` by hand. Read the
  constant in the report behavior with `IConstantsService.GetAsync<TConstant>(asOf, ct)` and pass the value (a currency
  id, a code) to the repository method as a parameter.
- **No UI text in the repository.** It returns data: ids, codes, numbers, dates, and an enum or flag for the *kind* of
  row (`RowKind.OpeningBalance`, `IsTransfer`), never `"Opening balance"`, `"Income"` or `"Transfer to "`. The behavior
  turns kinds into words with an injected `IStringLocalizer` (a resx key per kind) or an enum's `[EnumCaption]`, so the
  report follows the user's language. The same goes for names: return the id (or the dictionary's own `Name`), not a
  composed English sentence.
- **No silent fallbacks.** A missing exchange rate is not `1`, a missing constant is not a hard-coded `"UAH"`, and a bare
  `catch { return default; }` hides the bug. Return the gap (a nullable value, a `MissingRate` flag the line shows) or
  throw with a clear message.
- **Read registers through their readers**, never `db.Reg_X` directly: the tables keep un-posted rows (`Active = false`)
  and the movements of deleted documents, and a reader filters them for you (`create-register` §8a, which also says how
  to read a turnover's `IsExpense` rows signed). The latest value per key is `SliceOfLast`, not a query per key in a loop.
- **Group by ids, not by names.** A switch over a dictionary row's `Name` (`case "Fuel":`) breaks for a renamed or
  translated row; group by the id and let the line carry the row's own name.
- What else the configuration needs here (a horizon, a threshold) is a parameter of the method that the behavior fills
  from a constant or the filters, the same value in every report that uses it, never a number repeated in each method.

What the "never a literal" rule looks like in practice, for a row that can be one of several kinds:

```csharp
// Repository: data only
lines.Add(new UpcomingReportLine { VehicleId = v.Id, Kind = UpcomingKind.Work, ItemId = item.Id, DueDate = due, RemainingKm = km });
// Behavior: words, in the user's language
line.KindText = localizer[$"UpcomingKind_{line.Kind}"].Value;
```

## 2a. Reports whose rows are computed (dashboards, plan-vs-fact, statements, journals)

Nothing requires a report line to map 1:1 onto a table. The repository method may combine several reads (a register
reader for balances, a turnover reader for period totals, an EF query for the plan document) and compute the rows in
memory; the line class under `ReportingModels/` is a plain class with just the columns you show. Patterns that are
known to work:

- **A dashboard with several small tables** (balances per account, month totals, top categories): one *long* line Dto
  with a `Section` column, `[ReportViewColumn(Groupable = true, DefaultGroupOrder = 1)]` on it and
  `[PrintTable(GroupBy = nameof(Line.Section))]` on the print form's `Lines`, so the page, CSV and PDF show one group per
  section. A report with no filters has an empty filters Dto (plus, usually, an "as of" date).
- **Plan versus fact**: one row per planned category: planned (from the plan document), actual (from the turnover
  register for the period), remaining = planned - actual, percent used, and a `bool IsOverspent` column the grid can
  highlight; sort overspent first if useful.
- **Statement with running balance**: opening = the balance register as of the period start (`IBalanceReader.Balances(at, ...)`),
  then the period's movements ordered by date with a running total computed in the repository, closing = last running
  value (equal to the as-of balance at the period end; assert that in a test). Bounds: `Balances(at)` includes movements
  dated `at`, so the opening read is `Balances(from.AddTicks(-1), ...)`; end the period at `to.Date.AddDays(1).AddTicks(-1)`
  (`create-register` section 8a).
- **Journal across document types**: read the register's movement log (`IBalanceReader`/`ITurnoverReader.Movements`), not the
  document tables: each movement carries its document (`Document.Code`/`Date`), dimensions, direction and details, so put the
  columns a journal shows (category, comment, counter account) into the movement's Details when the document posts.
- Compute in the **repository**, return lines; the behavior only maps and fills the header (`GeneratedAt`, parameters).
- Prove each of these with a test that posts documents and compares the rows with hand-computed numbers
  (`test-configuration`); a report that "renders" can still be wrong.

## 3. Behavior + Mapper (`{Config}.Application/Reports/{Name}.cs`)

Copy `data/Application.cs`, fill in placeholders. **`ObjectName` must equal the `[KandraReportForm(Name = ...)]` value exactly** (`"{Name}Report"`): the generated
claims, the nav node's permission check and the report endpoint all use that name, so any other string makes the
report answer "permission denied" (`reference.md` §8).
`OnNewAsync` is optional — only implement it to prefill a sensible filter default server-side. `Export` is an
optional default-interface method — only implement it if `[SupportedExportFormats]` declared any format; delegate
to `IPrintRendererResolver`, never hand-write CSV/PDF rendering code (`create-print-form`'s territory). The
`{Name}ReportMapper` is a static Mapperly `[Mapper]` class the behavior calls directly — no DI, no `IMapper`.

## 4. DI registration — nothing to do

`[KandraReportBehavior(typeof(Report{Name}Dto))]` on the Behavior (step 3) is all that's needed —
`Kandra.Generators.Di.Application`'s `BehaviorDiscovery` finds it and emits the matching `services.AddReport<...>()`
call, same generated method dictionaries/documents/data-processors use. Check the generated
`GeneratedFamilyGarageApplicationDiExtensions.g.cs` (or the build output) if registration doesn't seem to be taking
effect — never hand-add an `AddReport<>()` call to `ConfigureServices.cs` yourself.

## 5. No EF migration needed

Reports don't own persisted state (unless you're also adding new entities/registers to query — that's a
separate `create-dictionary`/`create-document`/`create-register` concern with its own migration, not part of
the report itself).

## 6. Blazor UI — generated, nothing to write; nav entry — one hand-edit

Nothing to write for the page itself (see the intro above and `reference.md` §6 for exactly what's read from
which attribute, and what the generated page's route/title/`FiltersReady()` behavior is). The report still
needs a nav entry to be reachable from the sidebar — use [`add-interface-node`](../add-interface-node/SKILL.md):
`InterfaceNodeDefinition.Entity(typeof(Report{Name}Dto))` in `FamilyGarage.Application/Navigation/
FamilyGarageInterfaces.cs`. The node is gated like every other kind: it shows only to users with `View` on the report's name, the same claim
the behavior's `ObjectName` (step 3) checks (`reference.md` §8).

## 7. Refit client interface (hand-written declaration — no generator for this piece yet)

Copy `data/ClientLib.cs` to `{Config}.ClientLib.Common/ApiClients/I{Name}ReportApiClient.cs`, fill in
placeholders. `[PathPrefix]` must match `[KandraReportForm(Name = "...")]`'s value exactly. The body is empty —
`IReportApiClient<TFilters,TResult>` already declares the full `NewAsync`/`RunAsync`/`ExportAsync` set; Refit's
own source generator implements it at compile time. Everything past the declaration — bridging it to the
generic interface, and registering the `IReportService<,>` wrapper the generated page actually injects — is
generated by `Kandra.Generators.ClientLib`; `reference.md` §7 has the exact emitted DI calls if you want to
confirm rather than trust.

## 8. Localization

Add the report's own `[KandraReportForm(Name = "...")]` value (e.g. `"RestsReport"`) plus every `[Caption]` key
referenced by its filter/line/header fields to your configuration's `*.Localization` project's **en/ru/uk** resx
files. That one report-name key does double duty — it's both the generated page's title and its nav Entity
node's default caption (`reference.md` §6/§10). `ExportToCsv`/`ExportToPdf`/`ExtendedFilters` are already
engine-level keys — don't redeclare them.

## 9. Verify

`dotnet build FamilyGarage.slnx`, then inspect
`*.Client.MudBlazor/Generated.Net/Kandra.Generators.Ui.Blazor/Kandra.Generators.UiFormGenerator/{Name}Report.g.cs`
directly if the generated page doesn't look right (per the engine's `inspect-generated-output` skill) — don't
guess from the attributes alone. `dotnet test FamilyGarage.slnx`, then run the report live in the browser: it appears
in the sidebar where you added it (step 6), filters round-trip and re-fetch on change, a required filter (if
any) blocks the initial run until picked, sort/filter/group each grid column that declares it, grouping produces
the aggregate you expect (built-in and custom), and every rendered export button produces a sane file.

## If something here doesn't match the engine

`reference.md` is a point-in-time snapshot of engine source, not a live view — if a build error contradicts a
signature it or these templates show, the code wins (this includes the engine's own attribute doc comments,
several of which are stale — see `reference.md` §1's callout). Re-verify by grepping the type/attribute name
under `src/KandraCore/Kandra.Attributes/`, `Kandra.Forms/Reports/`, `Kandra.Application.Abstractions/Behaviors/`,
`Kandra.ClientLib.Common/ApiClients/`, `Kandra.ClientLib.MudBlazor/Shared/Pages/`, or
`Kandra.Generators.Ui.Blazor/`, then fix `reference.md`/`data/` so the next person doesn't hit the same
surprise.

## Optional last step: demo objects

If you ship demo data (`ImportPackages/*-demo.json`), add enough demo documents for the new report to show something
to it: stable `$id`s, `$ref`/`$code` references, date tokens, a `version` bump, then a dry run. See
[`author-import-package`](../author-import-package/SKILL.md).
