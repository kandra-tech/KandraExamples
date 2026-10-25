# Dashboard reference

Point-in-time snapshot of the engine's dashboard surface, written against the sales overview (the example to read). If a
build error contradicts it, the code wins: look under `Kandra.Attributes/Dashboards/`, `Kandra.Forms/Dashboards/`,
`Kandra.Application.Abstractions/Dashboards/` and `Kandra.Application.Abstractions/Behaviors/IDashboardBehavior.cs`.

## 1. Types

| Type | Namespace | Notes |
|---|---|---|
| `[KandraDashboardForm(Name, ResultDtoType, ValidatorType)]` | `Kandra.Attributes.Application` | On the filters Dto. |
| `[KandraDashboardBehavior(typeof(FiltersDto))]` | `Kandra.Attributes.Application` | On the behavior; DI registration is generated from it. |
| `IDashboardFiltersDto` | `Kandra.Forms.Dashboards` | Marker on the filters Dto (may have no properties). |
| `DashboardResult<TFilters>` | `Kandra.Forms.Dashboards` | Result base; carries `Filters`. |
| `IDashboardBehavior<TFilters, TResult>` | `Kandra.Application.Abstractions.Behaviors` | `ObjectName`, `OnNewAsync(filters, ct)`, `GetDataAsync(filters, lookup, ct)`. No export. |
| `IDashboardApiClient<TFilters, TResult>` | `Kandra.ClientLib.Common.ApiClients` | `New`/`Run`/`Settings`; the per-dashboard Refit interface derives it. |
| `CategoryChartData` | `Kandra.Forms.Dashboards` | `Labels`, optional `Keys`, `Series` (`Name`, `Values`); `AddLabel(label, key?)`, `AddSeries(name, values)`. |
| `KpiData` | same | `Value?`, `Previous?`, `Unit?`, `Trend`, `Sparkline?`. |
| `TableData<TRow>` | same | `Rows`; `TRow` uses `[Caption]`/`[Format]` on its properties. |
| `[KpiTile]`, `[BarChart(Stacked)]`, `[LineChart(Area)]`, `[PieChart(Donut)]`, `[TableWidget]`, `[TimeSeriesChart(Area)]`, `[HeatMapChart]`, `[SankeyChart]`, `[ScatterChart]`, `[RadarChart]`, `[RoseChart]` | `Kandra.Attributes.Dashboards` | One per result property. |
| `[DrillDown(typeof(Target))]`, `[DrillDown(typeof(Target), nameof(B.Method))]` | same | Target = a report or dashboard filters Dto; without a method a KPI tile is clickable and any other widget gets a header button. |
| `TimeChartData`, `HeatMapData`, `FlowData`, `PointChartData` (`Label`/`Key` per point), `KpiData.Key` | `Kandra.Forms.Dashboards` | See the shapes table in SKILL.md. |
| `[Hint("key", isLiteral = false)]` | `Kandra.Attributes.Naming` | Widget tooltip as a resource key (like `[Caption]`); wins over `[Description]`. |
| `[Widget(Order, Width, Height)]` | same | `WidgetHeight.Small` 110 px, `Medium` 260 px, `Large` 400 px of body; without it: width 4, medium, after the ordered ones. |
| `[AutoRefresh(default, Intervals \| Min/Max/Step)]` | same | On the filters Dto. |
| `DashboardPeriods`, `DashboardBucketSize`, `DashboardPeriodBucket` | `Kandra.Application.Abstractions.Dashboards` | `Last`, `Buckets`, `IndexOf`, `Today`, `LocalDate`, `StartOfDayUtc`. |
| `InterfaceDefinition(..., HomeDashboard: typeof(Dto))` | `Kandra.Application.Abstractions.Navigation` | Optional home page of an Interface. |

## 2. Worked examples

- **Business overview** (the home page; `BusinessOverview.cs`, `BusinessOverviewDashboardBehavior.cs`): no filters, `[AutoRefresh(600, ...)]`.
  Eight KPI tiles that each lead somewhere (`[DrillDown(typeof(SalesOverviewDashboardDto))]` etc.: the area dashboards, and the
  Counterparty settlements report), a line chart with a header link to the Sales dashboard, and a bar chart of debts by age whose
  bars open the settlements report (`DashboardDrills.Aging`). Data: `IDashboardsRepository.GetSalesByHour`/`GetPurchasesByHour` over
  twelve months, once; the same two reads feed the month tiles, the 30-day sparklines and the monthly series. About 12 statements.
- **Stock** (`Stock.cs`, `StockDashboardBehavior.cs`): table row drills (`(StockExpiringRowDto row)` opens the Stock turnover report by batch
  for that item and warehouse), KPI tiles that open the dedicated reports (Goods by expiry date, Goods not selling: a figure whose list can be large gets its own report, not an id list in a filter), and the
  expiry read: the open positions come from the balance table (`GetStockPositions`), and the expiration date of each batch on hand
  from the receipt movement that created it (party equals document), in chunks of 1 000 batches (`GetBatchExpiry`).
- **Money** (`Cash.cs`, `CashDashboardBehavior.cs`): a Sankey of this month's money (transfers between own accounts left out), a time
  series of the balance built from today's balance and the daily net movements (never a replay of the history), bars per bank
  account and per cash point (not one axis: the large account would hide the tills).
- **Sales**/**Purchasing**: bars, donut by top-level item folder (`GetTopFolders`), a heat map of weekdays against weeks, a scatter
  of margin against volume with the item as label and key, tables that open the report for the clicked customer or supplier.

## 3. How a request runs

`GET New` builds the filters and runs `OnNewAsync`; `GET settings` returns the operator floor (only for a dashboard with
`[AutoRefresh]`); `POST Run` authorizes `O|{Name}|View`, validates, calls `GetDataAsync`. There is no result cache, so a refresh
never evicts a report the user will export. Route `/dashboard/{stem}` (`SalesOverviewDashboardDto` ->
`/dashboard/salesOverviewDashboard`). The page shows the filter bar collapsed to a chip row of text-like values; a validation
failure shows the messages instead of the widgets and pauses the timer.

## 4. Diagnostics

`KANDRAUI040` `[DrillDown]` names a method the UI behavior does not have (or the filters Dto has no `[UiBehavior]`); `041` the method has the
wrong parameters or return type; `042` the target is neither a report nor a dashboard filters Dto; `043` a method on a widget with no click
shape (heat map, time series, Sankey: drop the method). `KANDRAUI033` widget attribute on a property of the wrong shape; `034` several widget attributes on one property; `035` a shape
property with no widget attribute (warning); `036` default interval not in the list; `037` empty list, bad range, or list and
range together; `038` more than 10 intervals; `039` an interval under 5 s. `KANDRAGEN014`-style missing pieces: a missing
validator or Refit interface fails the build or the startup DI validation.

## 5. Traps

- Defaults that fail the validator: the page opens on the error state. Test with `DashboardDefaultsTests`.
- `ObjectName` different from `Name`: permission denied.
- Grouping by UTC date: a sale at 01:00 local time lands in the wrong bar.
- A pie with many slices: the legend wraps and takes the chart's room; group the tail under "Other".
- Charts of one row with different `Height`: the shorter card shows empty space.
- Repository registered under the root dictionary type: ask `GetDictionaryAsync<ItemNode>`, not the folder or leaf type.
- A report query per bucket (a loop over weeks): the statement count grows with the period. Group once, bucket in memory.
- A current balance rebuilt from every movement ever posted: read the balance table (`IBalanceReader`), or as-of with the readers.
- A count nobody can act on ("items with zero stock" when the balance cannot go negative): no widget without a list one click away.
- One bar of "received" stacked on "paid": its height means nothing. Two series, side by side.
- Two widgets with the same title on one dashboard: the page throws a duplicate-key error. Give each its own caption.
- A scatter point click needs two circles per point in MudBlazor 9.8 (a visible dot and a hover circle): handled in the engine, recheck on an upgrade.
- A seeded installation keeps its old role claims: re-seed or edit roles to grant the dashboard area.
