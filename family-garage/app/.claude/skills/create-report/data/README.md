# Report code templates

Three `.cs` skeletons — one per plane (Forms/Application/ClientLib) — plus this README. Copy the set you need,
then replace every `{{Placeholder}}` and delete anything commented `// OPTIONAL — delete if unused`. These are
not compiled as part of any project (nothing under `.claude/skills/` is referenced from `FamilyGarage.slnx`) — they're
copy-paste source, not real code. See [`../reference.md`](../reference.md) for what each attribute/base type
actually means; see [`../SKILL.md`](../SKILL.md) for the full step order (repository query, localization, nav,
verify) and the full list of other hand-maintained files these three don't cover.

## Every report — use all three

- `Forms.cs` → `{{Config}}.Forms/Reports/{{Name}}.cs` — filters Dto, result-line Dto, print-data Dto, result
  envelope, validator, and (optional) a custom `ICustomAggregate`, all in one file, matching every live report
  (`Rests.cs`/`Transactions.cs`/`PriceListReport.cs`).
- `Application.cs` → `{{Config}}.Application/Reports/{{Name}}.cs` — Behavior + Mapper.
- `ClientLib.cs` → `{{Config}}.ClientLib.Common/ApiClients/I{{Name}}ReportApiClient.cs`.

There is a **fourth** hand-maintained piece this data/ folder deliberately doesn't template, because it's an
edit to an existing shared file rather than a new one: a new method on `IReportsRepository`/`ReportsRepository`
(`../reference.md` §9) — add it directly, following `GetRests`'s shape.

## Placeholder legend

| Token | Meaning | Example |
|---|---|---|
| `{{Config}}` | Your configuration's root namespace — the project-name prefix every plane project shares (`{{Config}}.Domain`, `{{Config}}.Forms`, `{{Config}}.Application`, `{{Config}}.ClientLib.Common`). In this repo that's `FamilyGarage`. | `FamilyGarage` |
| `{{Name}}` | Singular PascalCase report name — becomes `Report{{Name}}Dto`/`{{Name}}ReportLineDto`/`{{Name}}ReportDataDto`/`{{Name}}ReportResult`/`{{Name}}ReportBehavior`, the route `/reports/{{name}}report`, and `[KandraReportForm(Name = "{{Name}}Report")]`'s value. | `Suppliers` |
| `{{DimensionField}}` | One FK-shaped grouping/display column on the result line — becomes a `{{DimensionField}}Id`/`Code`/`Name` wire triple plus a composite `{{DimensionField}}Text` display column. | `Warehouse` |
| `{{MeasureField}}` | One numeric measure column with a group-footer aggregate. | `Amount` |
| `{{BoolFilterField}}` / `{{BoolFilterCaption}}` | A plain `bool` filter needing no editor attribute. | `NonEmptyOnly` / `NonEmptyOnly` |
| `{{MultiSelectFilterField}}` / `{{MultiSelectFilterCaption}}` / `{{MultiSelectSourceDto}}` / `{{MultiSelectSourceEntity}}` | An `IReadOnlyList<Guid>?` filter with `[MultiSelect(typeof(SourceDto))]` — `{{MultiSelectSourceDto}}` is the Forms-plane Dto the picker loads from, `{{MultiSelectSourceEntity}}` is the Domain-plane entity `lookup.GetDictionaryAsync<T>` resolves against for the print header. | `Warehouse` / `Warehouses` / `WarehouseDto` / `Warehouse` |
| `{{RequiredFilterField}}` / `{{RequiredFilterCaption}}` / `{{RequiredFilterSourceDto}}` | OPTIONAL — a `[Required] [Dropdown(...)]` `Guid` filter that gates `FiltersReady()` (`../reference.md` §5/§6). Only fill in if the report shouldn't run until one value is picked. | `PriceType` / `PriceType` / `PriceTypeDto` |
| `{{SourceRowType}}` | The repository's own plain, non-persisted result-row class the Mapper maps from (step 2, `{{Config}}.Domain/Entities/ReportingModels/`) — its name doesn't mechanically follow `{{Name}}`, pick whatever you named it there. | `RestReportLine` |

Every plane template assumes at least one grouping/display column, one numeric measure, one plain bool filter,
and one multi-select filter — the common shape shared by `RestsReport`/`TransactionsReport`. Delete whichever
of those your report doesn't need, and uncomment the `[DateEditor]`/`[DateRangeEditor]`/required-`[Dropdown]`
blocks it does. A report with **no** grouping/aggregation at all (see `PriceListReport`, `../reference.md` §5)
can drop `[Aggregate]` from every line-Dto column and delete the whole `{{Name}}QuantityAggregate` class.

If you're working in this repo's own `FamilyGarage` configuration, `{{Config}}` = `FamilyGarage` — a real, existing
prefix, not one you create. A different configuration substitutes its own equivalent project names.
