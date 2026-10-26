---
name: create-dashboard
description: Add a dashboard (charts, KPI tiles and small tables computed on the server, opened without filling anything in, optionally auto-refreshing) to a Kandra configuration, optionally as the Interface's home page. Use when the user asks for a dashboard, an overview or home screen, KPIs, or "charts of the business".
---

# Creating a dashboard

A dashboard is a read-only page of **widgets** (bar/line/pie charts, KPI tiles, a small table) that the server computes from
existing data. It opens and runs at once with default filters, and can refresh itself on a timer. It is not a report: no
export, no print form, no "fill in the filters and press Run". If the user wants rows to read, filter and export, that is
[`create-report`](../create-report/SKILL.md). [`reference.md`](reference.md) has every type's real signature and the full
worked example; copy-paste skeletons for the minimal dashboard (one KPI tile + one bar chart) are in [`data/`](data/)
(`data/README.md` explains the `{{Placeholder}}` tokens).

This configuration has no dashboard of its own yet. The real examples to read end to end are in the `KandraWms` reference
configuration: the **business overview** home page (`KandraWms.Forms/Dashboards/BusinessOverview.cs`, `KandraWms.Application/Dashboards/
BusinessOverviewDashboardBehavior.cs`, its Refit interface, and `HomeDashboard` in `KandraWmsInterfaces.cs`: KPI tiles that drill to
the area dashboards, a chart with a header link) and the **stock** dashboard (table row drills, the batch expiry read); the bounded
reads they share are `IDashboardsRepository`.

## The four pieces plus the Refit interface

Same shape as a report, minus the mapper: a dashboard spans **Forms** and **Application** (nothing to persist).

1. **Filters Dto + result Dto + validator** in `{Config}.Forms/Dashboards/{Name}.cs` (copy `data/Forms.cs`).
2. **Behavior** in `{Config}.Application/Dashboards/{Name}DashboardBehavior.cs` (copy `data/Application.cs`).
3. **Refit interface** in `{Config}.ClientLib.Common/ApiClients/I{Name}DashboardApiClient.cs` (copy `data/ClientLib.cs`).
   Hand-written and empty, like every other kind; a missing one is a `CS0246` in the generated client registration.
4. Nothing else is written by hand: the page, controller, DI registration, claims and policy are generated from the attributes.
   Never hand-edit `Generated.Net/`, never add an `AddDashboard<>()` call yourself.

`[KandraDashboardForm(Name = "{Name}Dashboard", ResultDtoType = ..., ValidatorType = ...)]` goes on the filters Dto. `Name`
is the permission object, the caption key and the policy stem, and the behavior's `ObjectName` **must equal it exactly**
(otherwise the page answers "permission denied"; `PermissionCatalogTests` checks it). The filters Dto implements
`IDashboardFiltersDto`; the result Dto derives `DashboardResult<TFilters>`, which echoes the filters.

## 1. Choose a data shape and a widget per number

One property on the result Dto per widget, typed with an engine shape and carrying one widget attribute (a mismatch is a
build diagnostic):

| What you show | Shape | Attribute |
|---|---|---|
| One number, optionally against a previous value, optionally a sparkline | `KpiData` | `[KpiTile]` |
| Values per category or period, one or more series | `CategoryChartData` | `[BarChart]`, `[BarChart(Stacked = true)]`, `[LineChart]`, `[LineChart(Area = true)]` |
| Shares of a whole (one series) | `CategoryChartData` | `[PieChart]`, `[PieChart(Donut = true)]` |
| A few rows (top N) | `TableData<TRow>` | `[TableWidget]` |
| A daily trend over weeks or months | `TimeChartData` | `[TimeSeriesChart]`, `[TimeSeriesChart(Area = true)]` |
| Which days or hours are busy (rows x columns) | `HeatMapData` | `[HeatMapChart]` |
| Where money or goods came from and went | `FlowData` | `[SankeyChart]` |
| Which items earn and which only turn over (X against Y, a label and a key per point) | `PointChartData` | `[ScatterChart]` |
| Several measures of one thing, or shares as petals | `CategoryChartData` | `[RadarChart]`, `[RoseChart]` |

Values are `decimal`. Labels are **already localized and formatted** by the behavior (`IStringLocalizer`, the caller's culture).
An empty shape renders "no data", never an error. Keep a pie/donut to about six slices (group the tail under "Other"), and give a
KPI a `Trend` (`HigherIsBetter`, `LowerIsBetter`, `Neutral`): overdue debts are `LowerIsBetter`, a balance is `Neutral`.
There is no horizontal bar chart. **Every widget answers a question someone acts on**: a count gets a widget only if the list behind
it is one click away. Use a time series, heat map, Sankey or scatter only where a bar cannot say it. Never stack "received" on top
of "paid" in one bar (its height means nothing): draw two series side by side. Per-account bars in one chart need similar
magnitudes, or the small ones vanish: split them (bank accounts, cash points).

### Drill-down: every figure leads somewhere

`[DrillDown(typeof(Target))]` on a widget opens a report (a `[KandraReportForm]` Dto) or another dashboard (a `[KandraDashboardForm]`
filters Dto) with the target's own defaults: a KPI tile is clickable as a whole, every other widget kind gets an "open" button in its
header (so a phone user does not have to hit a small bar). `[DrillDown(typeof(Target), nameof(MyUiBehavior.Method))]` also makes the
elements clickable: the UI behavior method (a public instance method of the filters Dto's `[UiBehavior]` class) takes `(ChartClick click)`
on a bar, line, pie, radar, rose or scatter chart, `()` or `(KpiData tile)` on a KPI tile, `(TRow row)` on a table row, and returns the
target's filters Dto (or a `Task`/`ValueTask` of it), or `null` to do nothing. Rules:

- Map the click's `Key`, never its label: fill `CategoryChartData.Keys` (a month `yyyy-MM`, a week's Monday `yyyy-MM-dd`, an id), a
  scatter point's `Key`, a table row's id property (a property with no `[Caption]` is not a column), or `KpiData.Key` (for example
  the item ids behind a figure, comma separated, capped).
- The target shows the figure that was clicked: use the same rule as the report (net of returns, home currency, local calendar
  period) and the same period, in the user's **local** time (`DateTimeKind.Local`; the wire converter makes it UTC).
- Heat maps, time series and Sankey diagrams have no click shape: give them the method-less header button.
- Element clicks need MudBlazor's chart markup; they are resolved in `DashboardChartWidget.razor.js` (bars, line points, slices,
  scatter circles).

## 2. Layout

`[Widget(Order = 10, Width = 4, Height = WidgetHeight.Small)]` places a widget in a 12-column grid from the `md` breakpoint up;
below it every widget is full width and stacks in `Order`, so the phone layout needs nothing from you. Put KPI tiles first at
`Width = 3` or `4` and `Small`; charts `Medium`; give the charts that sit side by side the **same height** (a short neighbour
leaves empty space), and use `Large` for a table or a chart with many categories. `[Caption]` is the widget title,
`[Hint("key")]` its tooltip (a resource key, so it is translated), `[Format("N0")]` formats values in tooltips, axes and tiles.

## 3. Behavior: defaults, data, time zone

- **Defaults must pass the validator.** The page calls `OnNewAsync`, then `GetDataAsync` with the result, without waiting for
  anyone. Set a value for every filter in `OnNewAsync` and write the validator's rules so those values pass. Add a configuration test like the reference configuration's `Dashboards/DashboardDefaultsTests` (one theory row per
  `DashboardDefaults.FindFiltersDtos(assembly)`, each run through `DashboardDefaults.CheckAsync`), which runs every dashboard's defaults through its validator; it picks up a
  new dashboard by itself, and must stay green. A dashboard with no filter properties is fine.
- **Bound the cost: a run is a few statements whatever the data.** Group a register **in SQL by UTC date and hour** inside the
  reader's projection (`ITurnoverReader<T>.Movements(from, to, q => q.GroupBy(m => new { m.Period.Date, m.Period.Hour, ... }).Select(...))`)
  and place the few resulting rows into the caller's local buckets in memory (`DashboardPeriods.IndexOf(buckets, date.AddHours(hour))`).
  A current balance comes from `IBalanceReader<,>.Balances` (the balance table) or `IAccountQueryService.GetBalancesAsync`, never
  rebuilt from all movements. Never run a report query once per bucket, never query `Reg_*` directly, and look names up only for the
  rows shown. A test pins it: see `DashboardStatementBudgetTests` (statements per run, and the same count for a short and a long period).
- **Group by day, week or month with `DashboardPeriods`** in the caller's time zone, never by UTC date or `DateTime.Date`:
  `DashboardPeriods.Last(12, DashboardBucketSize.Month, timeZone.TimeZone, now)` gives buckets with `StartUtc`/`EndUtc`
  (end exclusive, so a repository `to` is `EndUtc.AddTicks(-1)`), `DashboardPeriods.IndexOf(buckets, utc)` finds a row's bucket,
  `DashboardPeriods.Today(...)` the local day. Inject `ICurrentTimeZone`, and `TimeProvider? time = null` so a test can fix "now".
- **A bucket's `Start` is the calendar date; `StartUtc`/`EndUtc` only bound the movements you read.** Anything keyed by a
  calendar month or day (a budget plan stored per month, a report's month filter, a method that rebuilds a month from
  `.Year`/`.Month`) takes the local `Start`. `StartUtc` of a month is the last day of the previous month for every user
  east of UTC, so a query keyed by month reads the previous month's data and the tile disagrees with the report.
  Put movements into buckets with `DashboardPeriods.IndexOf`, never with `Period.Month`. The test for such a figure runs
  east of UTC (`test-configuration`, "Rules about days and months").
- **A table column never shows an enum or a `bool` as is:** the table prints the member name (English in every
  language) and `True`/`False`. Type such a column `string` and fill it in the behavior with `value.GetCaption(localizer)`
  (`Kandra.Localization`, the member's `[EnumCaption]`) or `localizer["Common_Yes"]`/`["Common_No"]`, never `ToString()`.
- **Read data the way the equivalent report does**, so the two agree: register readers or the report repositories
  (`ITradingReportsRepository`, `IReportsRepository`), `IAccountQueryService` for the ledger. Never `Reg_*`/`Acc_*` tables,
  never raw SQL, no UI text in a repository. Read constants (home currency) in the behavior. Net a turnover register's
  `IsExpense` rows (returns) off instead of ignoring them.
- One `GetDataAsync` computes every widget; it runs on every refresh for every open tab. Read a wide range once and bucket in memory,
  as the dashboards repository (`IDashboardsRepository`) does.
- Lookups only return code and name; resolve a folder or parent through a query adjustment on the root dictionary type
  (`GetDictionaryAsync<ItemNode>(q => q.Where(...))`), the repository is registered under the root.
- Prove the figures with the same rule as a report: compare with the matching report for the same period (live), and add a test
  that posts documents when the arithmetic is non-trivial.

## 4. Auto-refresh

`[AutoRefresh(600, Intervals = [60, 300, 600, 1800])]` on the filters Dto: the default interval in seconds, then the choices (or
`Min/Max/Step` for a regular range; not both). At most 10 entries, none under 5 s; the operator floor
`Kandra:Dashboards:MinRefreshSeconds` (default 15) has the last word. Every open tab is a query, so **10 minutes** with choices from 1 minute to 30 is the
default (5 minutes for money, which moves through the day); use a quicker one only for a screen that is watched. No attribute means no timer (the manual
refresh button is always there). The user's choice is remembered per device. A bad attribute means no timer, not a failed page.

## 5. Navigation, home page and permissions

- **Node**: `InterfaceNodeDefinition.Entity(typeof({Name}DashboardDto))` in the business area it belongs to in
  `FamilyGarageInterfaces.cs` ([`add-interface-node`](../add-interface-node/SKILL.md)); dashboards go first in their area.
- **Home page**: `HomeDashboard: typeof({Name}DashboardDto)` on an `InterfaceDefinition` shows it instead of the brand panel for
  users who may view it (others keep the brand panel). Different Interfaces may have different home dashboards. A home dashboard
  has no filters and no node of its own (the "Home" link is it): every tile drills to the area dashboard or report behind it, and
  its totals use the same rules as those reports. A drill target the user may not view answers "access denied".
- **Permissions**: a dashboard is gated by `O|{Name}Dashboard|View`. The seeded user role gets the area-wide
  `A|Dashboard|View` in `FamilyGarageClaimsSeeder` (the same way reports get theirs), so a new dashboard needs no new claim for it.
  An installation seeded before the dashboard area existed has to have its roles re-seeded or edited to see any.

## 6. Localization

Per dashboard, en/uk/ru ([`localize-configuration`](../localize-configuration/SKILL.md)): the `Name` key (page title and node
caption), every `[Caption]` and `[Hint]` key (filters, widgets, table columns) and every label or series name the behavior takes from
`localizer[...]`. Use `Dashboard_*` engine keys as they are.

## 7. Hints and `[Description]`

Put `[Hint("{Caption}_Hint")]` on every widget whose meaning is not obvious from its caption: one sentence saying what the number is
and what it is compared with ("Revenue of the current month, net of returns, compared with the previous month"), with the text in
en/uk/ru. A hint is a resource key resolved like a caption (`[Hint("text", true)]` takes a literal); it wins over `[Description]`.
Dashboards are not exposed through MCP yet, so `[Description]` adds nothing on a widget; use it, as on any form, on the filters Dto
class and on a filter a reader could misread.

## 8. Verify

`dotnet build`, `dotnet test` (watch `DashboardDefaultsTests` and `PermissionCatalogTests`), then run the app: the node appears
in its area, the page opens with data and no filter input, the figures equal the matching reports for the same period, the
refresh button and interval choice work, an invalid filter shows the messages instead of the widgets, the phone width stacks the
widgets, and light and dark both read. Click one element of every drillable widget: the target opens, already run, showing the clicked
figure. Titles of the widgets of one dashboard must differ (they key the widgets: two equal titles crash the page). `reference.md`
lists the diagnostics (`KANDRAUI033`-`043`).
