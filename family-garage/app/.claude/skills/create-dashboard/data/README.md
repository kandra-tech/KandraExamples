# Dashboard code templates

Three `.cs` skeletons, one per plane, plus this README. Copy them, replace every `{{Placeholder}}`, delete anything
commented `OPTIONAL`. They are not compiled as part of any project (nothing under `.claude/skills/` is referenced
from a solution). The minimal dashboard they describe is one KPI tile plus one bar chart over a month range; the
behavior's repository call is the one thing you write yourself (the reference example reads through
`ITradingReportsRepository`). See [`../SKILL.md`](../SKILL.md) for the other steps (navigation, permissions,
localization) and [`../reference.md`](../reference.md) for what each type means.

- `Forms.cs` -> `{{Config}}.Forms/Dashboards/{{Name}}.cs`: filters Dto, result Dto, validator.
- `Application.cs` -> `{{Config}}.Application/Dashboards/{{Name}}DashboardBehavior.cs`.
- `ClientLib.cs` -> `{{Config}}.ClientLib.Common/ApiClients/I{{Name}}DashboardApiClient.cs`.

| Token | Meaning | Example |
|---|---|---|
| `{{Config}}` | Root namespace shared by the plane projects. | `KandraWms` |
| `{{Name}}` | PascalCase dashboard name: `{{Name}}DashboardDto`, `[KandraDashboardForm(Name = "{{Name}}Dashboard")]`, route `/dashboard/{{name}}Dashboard`. | `SalesOverview` |
| `{{Measure}}` | The one measure the two widgets show. | `Sales` |
| `I{{Name}}DashboardRepository` / `Get{{Measure}}` | Your own read-only query over a register reader or `ITradingReportsRepository`-style repository: returns rows with a UTC `Date` and a signed `Amount`. | `ITradingReportsRepository.GetSales` |
