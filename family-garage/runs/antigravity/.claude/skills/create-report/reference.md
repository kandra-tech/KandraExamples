# Report engine — reference

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

Self-contained explanation of every engine type/attribute `SKILL.md` walks through — written so you don't need
to go read `src/KandraCore/*` source to use it. Exact signatures as of the last time this was verified against the
engine; if a build error contradicts something here, the code wins (see "If this reference goes stale" at the
bottom). Ready-to-copy `.cs` skeletons matching everything below live in [`data/`](data/).

## What a Report actually is

A parameterized, read-only query: a filters Dto, a result grid, optional CSV/PDF export
(`docs/kandra-architecture.md` §2.4). No persisted state of its own, no type identity (`IEntityWithTypeId`), no
numbering, no EF migration — a Report doesn't own a table (unless you're also adding a new entity/register to
query, which is a separate `create-dictionary`/`create-document`/`create-register` concern).

**A Report spans two class-library projects, not three** — there's no Domain-plane entity to write, because a
Report has nothing to persist:

- **`{Config}.Forms/Reports/{Name}.cs`** — one file holding *five* things: the filters Dto (`IReportFiltersDto`),
  the result-line Dto (one row of the grid), the print/business-data Dto (`[PrintForm]`, extends
  `ReportPrintForm`), the result envelope (`ReportResult<TFilters,TData>` closed generic), and the
  `AbstractValidator<TFilters>`. This is the plane the Blazor WASM client also references — same hard rule as
  every other entity kind (root `CLAUDE.md`): never let it pull in `.Application`/`.Domain`/`.Persistence`.
- **`{Config}.Application/Reports/{Name}.cs`** — the Behavior (`IReportBehavior<TFilters,TResult>`) + a static
  Mapperly mapper for the result lines, server-only.

**There is still no declarative composition model** — the domain-shaped parts (filters/result Dtos, a
behavior that queries a hand-written repository method, optional export) are hand-written, same as every other
entity kind. **The Blazor UI is the one part of a Report that genuinely differs from Dictionary/Document/
DataProcessor: it is fully generated, with zero hand-written `.razor` files left in this repo.**
`Kandra.Generators.Ui.Blazor`'s `ReportUiEmitter` emits the whole page — filters, extended-filters panel, export
buttons, a MudDataGrid result grid with sort/filter/group/aggregate — straight from the attributes on the Dtos
in §1. All four of `FamilyGarage`'s hand-written report pages (`RestsReport`/`TransactionsReport`/
`ExchangeRatesReport`/`PriceListReport.razor`) were retired the same way a Document/Dictionary/DataProcessor
generator cutover retires its hand-written pages: **moved, not deleted**, to
`FamilyGarage.Client.MudBlazor/_HandWrittenReference/Report/{Name}/{Name}.razor` (excluded from compilation,
kept only as the generator's own modeling reference — confirmed empty: `Pages/Reports/` has no `.razor` files
left). **This means the checklist below has no "Blazor UI" step at all** — unlike `create-dictionary`/
`create-document`, where hand-written pages are still very much a real, required step.

### Hand-maintained files

| What | File | Why it's manual |
|---|---|---|
| Filters + line + print-data + result Dto, Validator | New file `{Config}.Forms/Reports/{Name}.cs` | Core Forms-plane file — hand-written for every entity kind, no declarative composition model yet. |
| Behavior + Mapper | New file `{Config}.Application/Reports/{Name}.cs` | Same, Application-plane. |
| Repository query method | Edit to `{Config}.Domain/Commons/IReportsRepository.cs` — **plus** its implementation, `{Config}.Persistence/Repositories/ReportsRepository.cs` | Reports query directly against entities/registers — there's no report-specific ORM/query-generation layer. The interface lives in `.Domain` (so `.Application`'s Behavior can depend on it without pulling in `.Persistence`/EF Core), the implementation in `.Persistence`. One shared `IReportsRepository`/`ReportsRepository` currently holds all four `FamilyGarage` reports' queries — add a method, don't create a new repository unless the report genuinely doesn't fit (§9). |
| Localization keys | Edit to `{Config}.Localization/*.resx` (English required, uk/ru optional) | The report's own name (`[KandraReportForm(Name = "...")]`'s value, e.g. `"RestsReport"`) is a resx key that does **double duty** — it's both the generated page's `Title` and its nav Entity node's default caption (§6) — plus every `[Caption]` key on every filter/line/header field. No generator populates resx. |
| Refit client interface | New file `{Config}.ClientLib.Common/ApiClients/I{Name}ReportApiClient.cs` | Thin — same "declaration only, Refit's own generator implements the body" gap as every other entity kind's client interface (§7). |
| Nav entry | Edit to `{Config}.Application/Navigation/*Interfaces.cs` | Not derived automatically — `add-interface-node`. A Report `Entity` node is the **one kind with no auto-derived permission check** — always visible once its containing Interface/Folder is (§8). |

Explicitly **not** needed, unlike a Dictionary/Document: no type-id constant (no `IEntityWithTypeId`), no
numbering bucket, no EF migration for the report itself, no Blazor UI page (generated, see above), no manual DI
registration (§4 in `SKILL.md` — `[KandraReportBehavior]` alone is sufficient, same generator-discovery
mechanism as every other entity kind's Behavior attribute).

## 1. Attribute vocabulary

All namespaces are under `Kandra.Attributes.*` unless noted. Grouped by which of the report's four Dto shapes
each attribute targets.

### On the filters Dto (`IReportFiltersDto`, class-level)

| Attribute | Namespace | Signature | Meaning |
|---|---|---|---|
| `[KandraReportForm]` | `.Application` | `Name` (string, **required**), `ResultDtoType` (Type) — plus inherited `ValidatorType`/`SkipGeneration` from `KandraFormAttribute` | Marks the Dto as a report filters form. `Name` drives the controller class name, the route (`/reports/{name.ToLowerInvariant()}`), the client Refit name, **and** (§6/§8) the resx key used for both the generated page's title and its default nav caption — set it explicitly, it is not derived from the class name. |
| `[SupportedExportFormats(ExportFormat.Csv \| ExportFormat.Pdf)]` | `.Printable` | `Formats` (`ExportFormat` flags enum, ctor-only) | Declares which export buttons `ReportUiEmitter` renders — one per flag actually set, no more/no less (§6). |
| `[SupportedPrintForm(typeof(...))]` | `.Printable` | `PrintFormType` (Type), repeatable | Links the filters Dto to the `[PrintForm]` class that renders it — that's the print-data Dto (below), **not** the `ReportResult<,>` envelope. Also what `ReportUiEmitter`'s 2-hop discovery walk follows to find the result-line type (§6). |
| `[ExecuteFormTitle(value, isLiteral = false)]` | `.Naming` (`Forms/Naming`) | inherited from `VisibleNameAttribute` | Declared on every report Dto, matching the Document/Dictionary title-attribute convention — **but currently unconsumed by any generator** (verified directly against `ReportUiEmitter.cs`, not just its own stale doc comment — see §6's callout). Keep adding it for consistency/forward-compatibility, but don't expect it to control anything today; the real title comes from `Loc[form.Name]`. |
| `[Description]` | BCL (`System.ComponentModel`) | class or property | Semantic explanation of what the Dto/field means and how it's used — not a UI label (`[Caption]` covers that). Reflected by `Kandra.Mcp`'s `EntityFieldMetadataBuilder` and surfaced through the MCP server's `describe_entity_type`/`list_entity_types` tools as an AI agent's primary way to learn what an entity means. **Mandatory on the filters Dto class** (it's the one Dto type `[KandraReportForm]` registers with MCP — the result-line Dto and the print-data Dto below are never reflected by it); add on a filter property whenever its purpose isn't obvious from its name/caption alone. |

### On the result-line Dto (one row of the grid, property-level)

| Attribute | Namespace | Signature | Meaning |
|---|---|---|---|
| `[ReportViewColumn]` | `.Layout` | `Sortable`/`Filterable` (bool, default `true`), `Groupable` (bool, default `false`), `DefaultGroupOrder` (int, default `0` = not grouped), `DefaultSort` (`ReportSortDirection`: `None`/`Ascending`/`Descending`) | Makes a property an actual generated grid column, in `[Order]` order — a property without this attribute (an `Id`, or a wire-only field backing a computed display property) never becomes a column. **Also currently mis-documented as "unconsumed" in its own XML doc comment** — it is real, read by `ReportColumnEmitter` (§6). |
| `[Aggregate]` | `.Layout` | `Kind` (`AggregateKind`: `Sum`/`Avg`/`Count`/`Max`/`Min`, default `Sum`), `CustomAggregateType` (Type?, must implement `Kandra.Forms.Reports.ICustomAggregate`) | Adds a group-footer aggregate to the column. `CustomAggregateType`, when set, always wins over `Kind` (emitted as MudBlazor's `AggregateType.Custom`) — not compile-time constrained (`Kandra.Attributes` has zero project references by design, same precedent as `SupportedPrintFormAttribute.PrintFormType`), a mismatch fails at the generated call site instead. **Also mis-documented as unconsumed in its own doc comment** — real, see §2/§6. |
| `[Order(n)]` | `Kandra.Attributes` (root namespace) | `Value` (int, ctor-only) | Shared UI-layout/print-layout ordinal override — declaration order is the default in both, this overrides it. |
| `[Format("...")]` | `.Printable` | string | The **same** format string print output already needs doubles as the grid cell's own display format (`PropertyColumn.Format` / a built-in `[Aggregate]`'s `NumberFormat`) — one attribute, no separate "grid formatting" step. A `bool` column's `[Format("TrueLabel\|FalseLabel")]` (pipe-syntax) is special-cased into a custom `CellContext` template instead (re-derives `Kandra.Printing.PrintValueFormatter`'s own bool convention client-side, since `Kandra.Printing` is server-only) — see `IsExpense` in §4. |
| `[Caption("...")]` | `.Naming` | `(value, isLiteral=false)` | Column header / field label — same resx-key-by-default rule as every other entity kind. |
| `[PrintColumn]` / `[PrintColumn(StyleKey = "...")]` | `.Printable` | see `create-print-form` | The CSV/PDF export counterpart of `[ReportViewColumn]` — usually both go on the same property. Full print-styling vocabulary (`[Totals]`, `[PrintStyle]`, `[PrintSection]`, ...) is `create-print-form`'s scope, not repeated here. |

A composite "Code - Name" column is a plain computed property carrying its own `[ReportViewColumn]`/
`[PrintColumn]`/`[Caption]`/`[JsonIgnore]`, backed by two plain (no-attribute) `Code`/`Name` wire properties —
see `WarehouseText`/`ItemText` in §3. Combine via `CodeNameRef.Combine(code, name)` (`Kandra.DtoCommon.Printing`)
— the one shared "Code - Name" convention in this codebase, never a hand-rolled `$"{code} - {name}"`.

### On the print-data Dto (`[PrintForm]`, extends `ReportPrintForm`)

| Attribute | Namespace | Meaning |
|---|---|---|
| `[ReportViewGrid]` | `.Layout` | Marks the `Lines` collection property as the source of the results grid. `ReportUiEmitter`'s 2-hop discovery walk (filters Dto → `[SupportedPrintForm]` → this Dto's `[ReportViewGrid]`-marked property → its element type's `[ReportViewColumn]` properties) needs this to find the line type — a Report's filters and result are two separate Dtos, unlike every other entity kind's single-Dto shape (`UiFieldDiscovery.TryBuildReportResultColumns`). **Also mis-documented "unconsumed" in its own doc comment** — real. |
| `[PivotTable(RowTotals, ColumnTotals)]` + `[PivotRow]` / `[PivotColumn]` / `[PivotValue]` | `.Layout` | Cross-tab (pivot) result. `[PivotTable]` goes on the same `Lines` property as `[PrintTable]`/`[ReportViewGrid]`; on the line type, exactly one `[PivotRow]` (vertical dimension), one `[PivotColumn]` (horizontal dimension) and one numeric `[PivotValue]` (summed into cells, formatted by its own `[Format]`). `[PivotRow]`/`[PivotColumn]` take optional `CaptionProperty` (a string sibling shown next to the key - e.g. a name next to a code) and `SortProperty` (a string sibling used as the ordinal sort key, when the key's string order is wrong). The Lines stay a sparse long list - one line per non-empty cell; the engine's `PivotBuilder` pivots them both on the page (with a pivot/table toggle when `[ReportViewGrid]` + `[ReportViewColumn]`s are present too) and in CSV/PDF export (wide PDFs split into column bands). A missing/duplicate marker, a non-numeric value or an unknown sibling name is a `KANDRAPIVOT001`-`004` build error. FamilyGarage's `ChessReport` (debit accounts x credit accounts) is a live example. |
| `[PrintForm]` / `[PrintTable]` / `[PrintTitle]` / `[Page]` / `[PrintRole(PrintRole.Parameter)]` / `[PrintColumn(Group = …)]` / `[PrintTable(GroupBy = …)]` | `.Printable` | The print vocabulary a report uses — `create-print-form`'s scope. `[PrintForm]`/`[PrintTable]`/`[ReportViewGrid]` always travel together on the same `Lines` property. A `Parameter` property is one "Caption: value" part of the muted parameters line under the title (the report's filters, echoed); `Group` merges adjacent columns under a shared header; `GroupBy` writes a group row and a subtotal row per value of a line property (the behavior sorts the rows by it). `[PrintSection]`/`[PrintStyle]`/`[PrintSectionRef]` still exist as the escape hatch for fixed layouts and are not used by reports. |

### Editors — filter fields (reuses the Dictionary/Document editor vocabulary, `.Editors`)

| Attribute | Signature | Meaning |
|---|---|---|
| `[Dropdown(sourceDtoType)]` | `(Type)` | Plain `<select>` — see `create-dictionary` reference.md §2 for the full contract. Pairs with `[Required]` on a `Guid` filter field to make the report **not run until picked** — `ReportUiEmitter` auto-generates a `FiltersReady()` override requiring every such field `!= Guid.Empty` (§6, `PriceListReportDto.PriceTypeId` in §5). |
| `[Search(sourceDtoType)]` | `(Type)`, `NodeSelection`, `RootFolderCode` | Autocomplete FK picker — same contract as `create-dictionary`. |
| `[MultiSelect(sourceDtoType)]` | `(Type)` | Multi-value chip selector for an `IReadOnlyList<Guid>?` filter property — the common shape for "filter by a set of warehouses/items" (§3/§4). `sourceDtoType` is always a `*Dto`, never an entity type. |
| `[DateEditor(kind)]` | `(DateEditorKind, default DateTime)` | `DateEditorKind.Date`/`.Time`/`.DateTime` — a single date/time filter field (`ReportRestsDto.DateAsOf`, `PriceListReportDto.DateAsOf`). |
| `[DateRangeEditor]` | none | Calendar-with-presets editor for a `Kandra.Forms.Querying.DateRangeDto?` property (`From`/`To`, both nullable) — `ReportTransactionsDto.DateRange` in §4 is the live example. |
| `[AccountDialog]`/`[AccountSearch]` + `[SubcontoEditor(nameof(Account), slot)]` | account: none; slot: `(string accountProperty, int slotIndex)` | Chart-of-accounts account filter (`string?` code) plus up to five positional subconto slot filters (`Guid?`, slot 0-4) — rendered as **one** composite account+subconto editor at the account field's position, exactly like a Document's; changing the account clears the slots and re-runs, changing a slot re-runs. `[Required]` on the account makes the report wait until one is picked (it binds `string?` regardless, so `[Required]` — not nullability — is the signal there). `ReportAccountCardDto` (Account Card) is the live example; see the `add-account-subconto-field` skill for the field shapes. |
| `[AccountMultiSelect]` | none | Several chart-of-accounts leaves as an `IReadOnlyList<string>?` of account codes — chips plus a searchable checkbox picker; empty/null conventionally means "every account". FamilyGarage's `ReportTrialBalanceDto.AccountCodes` (Trial Balance) is a live example. |
| `[ExtendedFilter]` | none | Moves the field into a collapsed "advanced filters" panel instead of the always-visible top section. **Also mis-documented "unconsumed"** — real, `ReportUiEmitter` splits `ui.OwnFields` into `regularFields`/`extendedFields` by this exact flag (§6). |

**A recurring, worth-flagging pattern**: at the time of writing, `ReportViewColumnAttribute`, `AggregateAttribute`,
`ReportViewGridAttribute`, and `ExtendedFilterAttribute` **all still carry an XML doc comment claiming they're
"currently unconsumed by any generator... Report UI generation doesn't exist yet"**. That was true before
`ReportUiEmitter` was built and is **stale now** — every one of them is read by `ReportUiEmitter`/
`ReportColumnEmitter`/`UiFieldDiscovery` today (verified directly against those three files' source, not just
against the doc comments, which is exactly the trap here). **Don't trust an attribute's own doc comment over
the generator source that actually reads it** — grep `Kandra.Generators.Ui.Blazor`/`Kandra.Generators.Shared`
for the attribute's simple name to check current reality, and fix the doc comment while you're there if you
confirm it's still wrong.

## 2. Base interfaces and classes

```csharp
// Kandra.Forms.Reports.IReportFiltersDto — marker only, no members
public interface IReportFiltersDto { }

// Kandra.Forms.Reports.IReportResultDto — the CacheTag every result carries
public interface IReportResultDto
{
    string? CacheTag { get; set; } // opaque per-user export-cache token; set by RunAsync, echoed back on export
}

// Kandra.Forms.Reports.IReportResult<TFilters> / <TFilters,TData>
public interface IReportResult<TFilters> : IReportResultDto { TFilters Filters { get; set; } }
public interface IReportResult<TFilters, TData> : IReportResult<TFilters> { TData Data { get; set; } }

// Kandra.Forms.Reports.ReportResult<TFilters,TData> — the base every concrete report result closes with no
// members of its own: sealed class RestsReportResult : ReportResult<ReportRestsDto, RestsReportDataDto>;
public abstract class ReportResult<TFilters, TData> : IReportResult<TFilters, TData>
{
    public string? CacheTag { get; set; }
    [Required] public required TFilters Filters { get; set; }
    [Required] public required TData Data { get; set; }
}
```

The result Dto is deliberately split into a technical envelope (`ReportResult<,>` — Filters/Data/CacheTag) and
the actual business/print data (the `[PrintForm]` Dto extending `ReportPrintForm`) so the wire/cache envelope
stays free of print concerns — `{Name}ReportResult` never needs its own `[PrintForm]`/`[PrintTable]`.

```csharp
// Kandra.Application.Abstractions.Behaviors.IReportBehavior<TFilters,TResult>
public interface IReportBehavior<TFilters, TResult>
    where TFilters : class, new()
    where TResult : IReportResult<TFilters>
{
    string ObjectName { get; }   // hand-picked, no convention — see §8

    Task OnNewAsync(TFilters filters, CancellationToken cancellationToken) => Task.CompletedTask;

    // lookup is passed in, not constructor-injected - same "resolve once, hand down" role ISubmitScope
    // plays for OnSubmitAsync - a behavior never needs its own dictionary repositories just to resolve
    // id-filter display names for the print header.
    Task<TResult> GetDataAsync(TFilters filters, ILookupRepository lookup, CancellationToken cancellationToken);

    // Default = doesn't support export. layoutTag is opaque/report-defined - null picks the generator-
    // registered "default" renderer, non-null picks a hand-registered alternate layout.
    IFileData? Export(TResult report, ExportFormat format, string? layoutTag,
        IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone) => null;
}
```

`OnNewAsync` lets a report prefill sensible filter defaults server-side (`ReportRestsDto.NonEmptyOnly = true` in
§3) instead of every client hard-coding them — mirrors `IDataProcessorBehavior<,>.OnNewAsync`.

```csharp
// Kandra.Forms.Reports.ICustomAggregate — a report-line-specific group-footer aggregate: the attribute
// references it by Type, implementation is a plain static-method class, no DI.
public interface ICustomAggregate
{
    static abstract string? Calculate<T>(IEnumerable<T> items, ReportGroupingState groupingState, string? format);
}

// Kandra.Forms.Reports.ReportGroupingState — ordered set of currently-grouped column property names,
// outermost-first (a List<string>, not a Queue<string> - ungrouping needs arbitrary-position removal).
public sealed class ReportGroupingState
{
    public IReadOnlyList<string> Grouped { get; }
    public string? FrontGroup { get; }  // outermost/first-grouped column, or null if nothing grouped
    public void Set(string propertyName, bool grouped);
}
```

`format` on `Calculate<T>` is the column's own `[Format]` string (e.g. `"N3"`), passed through so a custom
aggregate's computed value renders with the same format as the column's individual cells, not a hardcoded one —
`null` when the column carries no `[Format]`. A custom aggregate is invoked via a **direct static generic call**
at the generated column's emission site (`{CustomAggregateType}.Calculate(items, GroupingState, format)`), never
through the interface reference itself — the interface only enforces the contract shape at the implementing
class's own declaration.

## 3. Worked example: `RestsReport`

Full, current, working source — `FamilyGarage.Forms/Reports/Rests.cs` + `FamilyGarage.Application/Reports/Rests.cs` +
`IReportsRepository.GetRests`. Read it end to end; this section calls out the non-obvious parts only. Its
`ReportRestsDto`'s real `[Description]` content (class-level + every filter) is worth reading as a model for
how much detail is useful — a sentence or two per field, not a paragraph.

- **Filters** (`ReportRestsDto`) — `NonEmptyOnly` (plain `bool`, defaulted to `true` server-side in
  `OnNewAsync`), `DateAsOf` (`DateTime?`, `[DateEditor(DateEditorKind.Date)]`, "balance as of this date instead
  of current" per the design doc), `WarehouseId`/`ItemId` (`IReadOnlyList<Guid>?`, `[MultiSelect(...)]
  [ExtendedFilter]` — both collapsed into the advanced panel).
- **Line Dto** (`ReportRestLineDto`) — two composite "Code - Name" columns (`WarehouseText`/`ItemText`, each
  `[JsonIgnore]` computed properties over plain wire `Code`/`Name` pairs, `Groupable = true`), a plain
  `BranchName` column, and two numeric columns each carrying **both** `[Totals(Aggregate.Sum)]` (print-form
  totals row, `create-print-form`'s concern) **and** `[Aggregate(...)]` (grid group-footer aggregate, this
  skill's concern) — `Amount` uses the built-in `Kind = AggregateKind.Sum`, `Quantity` uses a custom aggregate
  instead (below) because summing raw quantities across different items mixes incompatible units of measure.
- **Custom aggregate** (`RestsQuantityAggregate : ICustomAggregate`) — only sums when the grid is grouped by the
  Item column: `groupingState.FrontGroup == nameof(ReportRestLineDto.ItemText)`. Compares against `ItemText`
  (the property that actually carries `[ReportViewColumn(Groupable = true)]`), not `ItemName` — grouping by the
  visible "Item" column groups by `ItemText`, so that's the name `FrontGroup` reports. Falls back to `""` (not
  `"0"`/`null`) when ungrouped or grouped by something else.
- **Print-data Dto** (`RestsReportDataDto : ReportPrintForm`) — three `[PrintRole(PrintRole.Parameter)]` fields
  (resolved id-filter display names — `Warehouses`/`Items` as `IReadOnlyCollection<CodeNameRef>?`, `DateAsOf`),
  which the engine joins into the muted parameters line under the title (empty ones skipped), plus `Lines`
  (`[Required] [PrintTable(GroupBy = nameof(ReportRestLineDto.WarehouseText))] [ReportViewGrid]`): the behavior
  sorts the rows by warehouse and the PDF writes a group row and a subtotal row per warehouse. The audit pair comes
  from `ReportPrintForm` and goes to the page footer.
- **Behavior** (`RestsReportBehavior`) — `ObjectName => "Rest"` (not `"RestsReport"`, §8). `GetDataAsync` calls
  the repository, maps to line Dtos, then a private `EnrichPrintHeaderAsync` fills `GeneratedAt`/
  `GeneratedBy` (via the shared `PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, ct)`
  helper — cosmetic header metadata, swallows a missing/non-human caller rather than failing the report) and
  resolves the id-filter display names through `lookup.GetDictionaryAsync<TEntity>(ids, ct)`. `Export` delegates
  to `IPrintRendererResolver.Render(...)` — never hand-write CSV/PDF rendering code, `create-print-form`'s territory.
- **Mapper** (`[Mapper] static partial class RestsReportMapper`) — one
  `public static partial List<ReportRestLineDto> ToDtos(IEnumerable<RestReportLine> lines)` with
  `[MapperRequiredMapping(RequiredMappingStrategy.Target)]`; the behavior calls it directly (no DI). A line-Dto
  member named differently from its source gets `[MapProperty]` on an explicit private element method -
  `TransactionsReportMapper` maps `DocumentId`/`DocumentNumber` to `OrderId`/`OrderNumber` that way.
  `RestReportLine` is a plain projection-shaped class in `FamilyGarage.Domain.Entities.ReportingModels` — not a
  `[Kandra*Entity]`, not persisted, purely the repository query's own result shape.

## 4. Second example: `TransactionsReport` — `[DateRangeEditor]` + a second `[Aggregate]` pair

Same shape as Rests, worth reading for two things Rests doesn't show:

- `ReportTransactionsDto.DateRange` (`Kandra.Forms.Querying.DateRangeDto?`, `[DateRangeEditor]`) — the validator
  (`ReportTransactionsDtoValidator`) enforces `From < To` only `.When(...)` both are actually set:
  `r.From is null || r.To is null || r.From.Value < r.To.Value`.
- `ReportTransactionLineDto.IsExpense` (`bool`, `[Format("Expense|Receipt")]`) — the pipe-syntax bool format
  string mentioned in §1, rendered as a real `Expense`/`Receipt` label in the grid cell via a generated
  `CellContext` template, not a raw `True`/`False`.
- The exact same `Quantity`/`Amount` `[Aggregate]` pair as Rests (`TransactionsQuantityAggregate`, same
  front-group-is-Item guard) — confirms this is a real, repeated pattern, not a one-off.
- Three `[MultiSelect] [ExtendedFilter]` fields (`SourceId`/`DestinationId`/`ItemId`) plus a plain `OrderId`
  filter deliberately left **without** an editor attribute — the code comment on it explains why: no single
  `*Dto` source type spans every document kind a transaction can originate from
  (`GoodsReceipt`/`Waybill`/`Transfer`), so it's left unwired rather than pointed at an arbitrary one. A
  filter field with no editor attribute is legal — it's just not rendered by the generated UI.

## 5. Third example: `PriceListReport` — required-filter gating, no `[Aggregate]`/`[MultiSelect]` at all

The simplest of the four live reports, worth reading as a *minimal* report shape:

```csharp
public sealed class PriceListReportDto : IReportFiltersDto
{
    [Required] [Caption("PriceType")] [Dropdown(typeof(PriceTypeDto))]
    public Guid PriceTypeId { get; set; }

    [Required] [Caption("DateAsOf")] [DateEditor(DateEditorKind.Date)]
    public DateTime DateAsOf { get; set; }
}
```

Both filters are `[Required]`, and `PriceTypeId` is a `[Required] Guid` — `ReportUiEmitter` auto-generates
`protected override bool FiltersReady() => Filters.PriceTypeId != global::System.Guid.Empty;` on the generated
page (§6), so the report simply doesn't run until a price type is picked, matching the retired hand-written
page's own "don't run until `PriceTypeId != Guid.Empty`" guard exactly. No `[MultiSelect]`/`[ExtendedFilter]`/
`[Aggregate]` anywhere in this report — none of those are mandatory, they're each opt-in per report.

## 6. The generated UI — what actually drives what

`Kandra.Generators.Ui.Blazor.ReportUiEmitter` (mirrors `DataProcessorUiEmitter`'s shape more than
`DocumentUiEmitter`'s — a Report is one single-purpose page, no List/Add/Update/View split) emits exactly one
`spc.AddSource` call per report: a sealed subclass of `ReportPageBase<TFilters,TResult,TData>`
(`Kandra.ClientLib.MudBlazor.Shared.Pages`) into
`{Config}.Client.MudBlazor/Generated.Net/Kandra.Generators.Ui.Blazor/Kandra.Generators.UiFormGenerator/{Name}.g.cs`
(`{Name}` = `form.Name`, e.g. `RestsReport.g.cs` — **not** `{Name}Report.g.cs`, since `form.Name` already ends
in `Report`). Inspect that file directly if the generated page doesn't look right, per
`inspect-generated-output`.

What's read from the attributes in §1, concretely:

- **Route**: `/reports/{form.Name.ToLowerInvariant()}` (e.g. `/reports/restsreport`).
- **`Title`**: `Loc[form.Name]` — literally `Loc["RestsReport"]`, **not** derived from `[ExecuteFormTitle]`
  despite every report Dto carrying one (§1's callout — verified against `ReportUiEmitter.EmitPage`, which
  never references `ExecuteFormTitleAttribute` at all). The same resx key doubles as the nav Entity node's
  default caption — `EntityRouteConventions.Resolve` returns `CaptionKey = reportAttr.Name` directly for a
  Report Dto, with no `[NavigationName]` fallback attempt (unlike Document/Dictionary, which do check for one)
  — so one resx entry (`form.Name`) covers both.
- **Regular vs. extended filter fields**: `ui.OwnFields` partitioned by `IsExtendedFilter` (`[ExtendedFilter]`)
  — `HasExtendedFilters` (and the whole collapsed panel) is only emitted at all if at least one field is
  extended.
- **`FiltersReady()`**: auto-generated **only** when at least one filter field is a required `Guid` or a required
  `string` (a non-nullable one, or an `[AccountDialog]`/`[AccountSearch]` account field marked `[Required]`) —
  ANDs `Filters.{Field} != Guid.Empty` / `!string.IsNullOrEmpty(Filters.{Field})` across every such field (§5). A
  report with none keeps the base class's `=> true` (always runnable on load).
- **Export buttons**: one `row.AddButton(Loc["ExportTo{FormatName}"], ...)` per flag set in
  `[SupportedExportFormats]` — `ExportToCsv`/`ExportToPdf` are **engine-level** resx keys already present in
  `Kandra.Localization` (not something a new report's own resx needs to add).
- **Result columns**: `ui.ResultColumns`, each emitted via `ReportColumnEmitter.EmitColumn` into one
  `columns.ReportColumn<TLine, TValue>(...)` call carrying caption/sortable/filterable/groupable/
  initialDirection/aggregate/onGroupingChanged/format/cellTemplate — everything in §1's line-Dto attribute
  table, with nothing left to configure by hand.
- **Filter re-fetch**: every filter field is wired `alwaysOnChanged: "await OnFilterChanged();"` unconditionally
  — unlike a Document/Dictionary field editor, which only re-fetches on an explicit `[OnChange]`. Matches
  every retired hand-written report page's own "re-run on any filter change" convention. `[SubcontoEditor]`/
  `[SubcontoMultiSelectEditor]` slot fields are grouped with their account field into one
  `AddAccountSubcontoEditor`/`AddAccountSubcontoMultiSelectEditor` call (same `BuildSubcontoGroups` as
  `DocumentUiEmitter`), carrying the same re-fetch as its `onChanged:` argument.

Client-side page lifecycle (`ReportPageBase<TFilters,TResult,TData>`, the abstract base every generated page
closes) is worth knowing if a report seems to "not run": `OnInitializedAsync` calls `RepService.NewAsync()`
(hits the Behavior's `OnNewAsync`) to seed `Filters`; `OnParametersSetAsync`/every filter change calls
`FetchData()`, which checks `FiltersReady()` first and sets `Report = null` if it isn't (renders nothing below the
filters — the loading indicator only shows while a ready report is actually fetching); `BuildRenderTree` itself guards `Filters is null` (the one Blazor render that happens before
`OnInitializedAsync`'s task resolves — the exact bug class §7 of
[[feedback_report_generator_gotchas]] describes hitting once already, now fixed with this guard) separately
from `Report is null`.

## 7. Client Refit interface — hand-written declaration, generated implementation + service

```csharp
// Kandra.ClientLib.Common.ApiClients.IReportApiClient<TFilters,TResult> — the shared base every report's
// client interface closes. Report controllers inherit ControllerBase directly (not EntityControllerBase),
// so there's no CRUD-style base to mirror here - just three members.
public interface IReportApiClient<TFilters, TResult> : IKandraProtectedApiClient
    where TFilters : class, IReportFiltersDto
    where TResult : class, IReportResultDto
{
    [Get("new")]    Task<TFilters> NewAsync(CancellationToken cancellationToken = default);
    [Post("run")]   Task<TResult> RunAsync([Body] TFilters body, CancellationToken cancellationToken = default);
    [Post("export")] Task<HttpResponseMessage> ExportAsync([Body] ReportExportRequest<TFilters> request, CancellationToken cancellationToken = default);
}
```

The concrete interface is thin — everything comes from the shared base:

```csharp
[PathPrefix("/api/v1/reports/RestsReport")]
public interface IRestsReportApiClient : IReportApiClient<ReportRestsDto, RestsReportResult>
{
}
```

`[PathPrefix]` must match `[KandraReportForm(Name = "...")]`'s value exactly (§1), same rule as every other
entity kind's client interface. **Everything past the interface declaration is generated**:
`Kandra.Generators.ClientLib`'s `ClientEmitter` discovers every `[KandraReportBehavior]`-attributed Behavior
(via its closed `IReportBehavior<TFilters,TResult>`) and emits, per report:

```csharp
services.AddScoped<IReportApiClient<TFilters, TResult>>(p => p.GetRequiredService<IYourReportApiClient>());
services.AddScoped<IReportService<TFilters, TResult>, ReportService<TFilters, TResult>>();
```

— bridging your hand-written closed Refit interface to the generic one, and registering the
`IReportService<TFilters,TResult>` wrapper (`NewAsync`/`RunAsync`/`ExportAsync`) that `ReportPageBase` actually
injects (§6). **Never hand-write either registration** — same "check the generated DI file, don't hand-add a
call" rule as every other entity kind. Report export (CSV/PDF) happens server-side, so — unlike some earlier
engine iterations — `IReportService<,>.ExportAsync` needs no per-report client-side wiring either.

(`Kandra.Generators.ClientLib`'s own `KnownTypeNames` constants for these two calls happen to be named
`IReportServiceClient`/`ReportServiceClient` internally — purely to distinguish them from the unrelated
server-side `Kandra.Application.Abstractions.Services.IReportService`, in the same emitter's own vocabulary.
The actual emitted, runtime type is `Kandra.ClientLib.Common.Services.Crud.IReportService<TFilters,TResult>`/
`ReportService<TFilters,TResult>` — exactly what `ReportPageBase.RepService` (§6) is typed as.)

## 8. A report's permission name is its `[KandraReportForm(Name)]`

A report is gated like every other kind, and its permission object name is the `Name` of its `[KandraReportForm]`
(e.g. `"RestsReport"`):

- `EntityClaimsEmitter` (`Kandra.Generators.Di.Application`) emits `O|{Name}|View` for each report, and the client policy
  generator a `View{Name}` policy;
- `EntityRouteConventions.Resolve` gives an `InterfaceNodeDefinition.Entity(typeof(Report{Name}Dto))` node
  `InterfaceNodeVisibility.Claim(ApplicationArea.Report, Name, ApplicationAction.View)`;
- the report service checks `IReportBehavior<,>.ObjectName`, so **`ObjectName` must be that same string**. Anything else
  ("Rest", "Dashboard") makes the report refuse every caller, admins aside. Object names must also be unique across areas,
  because a claim string carries no area (the reference configuration guards both with a permission-catalog test).

(An earlier version of this skill said reports had no auto-derived permission and `ObjectName` was a free choice. That
changed; trust this section.)

## 9. Repository query

`{Config}.Domain/Commons/IReportsRepository.cs` (interface — note it lives in `.Domain`, not `.Persistence`, so
`.Application`'s Behavior can depend on it without pulling in EF Core) + `{Config}.Persistence/Repositories/
ReportsRepository.cs` (implementation). One shared repository currently holds every `FamilyGarage` report's query
method — `GetRests`/`GetTransactions`/`GetExchangeRates`/`GetPrices` all live side by side on the same
interface/class. Add a new method there for a new report unless it genuinely doesn't fit (e.g. a report over an
entirely unrelated subsystem) — don't reflexively spin up a new repository interface per report.

Reports query registers/entities directly — `IQueryable`-friendly composition, dimension/date filters applied
server-side, projected straight into the line-Dto-shaped result class (`RestReportLine`, a plain, non-persisted,
non-attributed class under `{Config}.Domain/Entities/ReportingModels/`). `ReportsRepository`'s constructor is
also the live example of constructor-injecting register **readers** directly (`ITurnoverReader<InventoryMovement>`,
`IBalanceReader<InventoryMovement,InventoryBalance>`, `IInfoRegisterReader<ExchangeRateRecord>`, ...) — readers
are plain ungated services, unlike writers (`create-register` reference.md §5/§7 has the full contract).

## 10. Localization

Every `[Caption]` key on every filter/line/header field, plus the report's own `[KandraReportForm(Name = ...)]`
value itself (§6's double-duty title/nav-caption key), needs an entry in the `{Config}.Localization`
resx files (English required, uk/ru optional) — same rule and same "missing key silently renders the raw key name, no build error, no
exception" failure mode as every other entity kind's `[Caption]` keys (`create-dictionary` reference.md §8).
Engine-level keys a new report's own resx does **not** need to add: `ExportToCsv`/`ExportToPdf`/
`ExtendedFilters` (already in `Kandra.Localization`, resolved through the localization chain — root `CLAUDE.md`'s
`LocalizationChain`).

## If this reference goes stale

This document is a snapshot, not a live view — and §1 already caught one real, live case of the *engine's own
doc comments* being stale (several `Kandra.Attributes.Layout.*` XML doc comments claiming "unconsumed by any
generator" for attributes `ReportUiEmitter`/`ReportColumnEmitter` demonstrably do consume). If the code
disagrees with something here — a compile error citing a signature that doesn't match, a generated page missing
a feature this document says it has — trust the actual generator/attribute source over both this file and that
attribute's own doc comment, then fix whichever was wrong. Re-verify by grepping the type/attribute name under
`src/KandraCore/Kandra.Attributes/{Application,Editors,Layout,Naming,Printable}/`, `Kandra.Forms/Reports/`,
`Kandra.Application.Abstractions/Behaviors/IReportBehavior.cs`, `Kandra.ClientLib.Common/ApiClients/
IReportApiClient.cs`, `Kandra.ClientLib.MudBlazor/Shared/Pages/ReportPageBase.cs`, or
`Kandra.Generators.Ui.Blazor/{ReportUiEmitter,ReportColumnEmitter}.cs` + `Kandra.Generators.Shared/
UiFieldDiscovery.cs` (`TryBuildReportResultColumns`) — every type named above lives in exactly one of those.
