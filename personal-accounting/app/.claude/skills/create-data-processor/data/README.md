# Data processor code templates

Three `.cs` skeletons — one per plane (Forms/Application/ClientLib) — plus this README. Copy the set you need,
then replace every `{{Placeholder}}` and delete anything commented `// OPTIONAL — delete if unused`. These are
not compiled as part of any project (nothing under `.claude/skills/` is referenced from `PersonalAccounting.slnx`) — they're
copy-paste source, not real code. See [`../reference.md`](../reference.md) for what each attribute/base type
actually means; see [`../SKILL.md`](../SKILL.md) for the full step order (localization, nav, verify) and the
full list of other hand-maintained files these three don't cover.

## Every data processor — use all three

- `Forms.cs` → `{{Config}}.Forms/DataProcessors/{{Name}}.cs` — input Dto, result Dto, validator, all in one
  file, matching the live `ImportNbuRates.cs`.
- `Application.cs` → `{{Config}}.Application/DataProcessors/{{Name}}.cs` — the Behavior. The file has two
  mutually-exclusive shapes in it: the common one (create/submit an ordinary document via `I{Kind}CrudService<,>`,
  uncommented by default) and the rare direct-register-write one (commented out, `../reference.md` §6) — pick
  one, delete the other.
- `ClientLib.cs` → `{{Config}}.ClientLib.Common/ApiClients/I{{Name}}ApiClient.cs`.

There is no Domain-plane file and no Mapper for a data processor — unlike every other entity kind, it has
nothing persisted to map to/from (`../reference.md`'s intro explains why).

## Placeholder legend

| Token | Meaning | Example |
|---|---|---|
| `{{Config}}` | Your configuration's root namespace — the project-name prefix every plane project shares (`{{Config}}.Forms`, `{{Config}}.Application`, `{{Config}}.ClientLib.Common`). In this repo that's `PersonalAccounting`. | `PersonalAccounting` |
| `{{Name}}` | PascalCase processor name — becomes `{{Name}}Dto`/`{{Name}}Result`/`{{Name}}Validator`/`{{Name}}Behavior`, the route `/dataprocessor/{{name}}`/`/api/v1/processors/{{Name}}`, and `[KandraDataProcessorForm(Name = "{{Name}}")]`'s value. Must equal the Behavior's own `ObjectName` string exactly (`../reference.md` §8) — this is enforced at runtime, not just style. | `ImportNbuRates` |
| `{{InputField}}` / `{{InputFieldCaption}}` | One input parameter with an editor attribute. | `Date` / `Date` |
| `{{BoolField}}` / `{{BoolFieldCaption}}` | OPTIONAL — a plain bool flag needing no editor attribute. | `Submit` / `Button_Submit` |
| `{{ResultField}}` / `{{ResultFieldCaption}}` | One result property that should render on the result panel — needs `[Caption]` or it's silently skipped (`../reference.md` §1). | `Code` / `RateImport` |
| `{{CreatedEntityIdField}}` | A result property deliberately left **without** `[Caption]` — a technical id (e.g. the created document's own id) the result panel shouldn't show. | `RateImportId` |
| `{{TargetDocumentDto}}` / `{{TargetDocumentQueryDto}}` | The Dto/QueryDto pair of whichever document (or dictionary) this processor creates via the ordinary CRUD service. | `RateImportDto` / `RateImportQueryDto` |

If you're working in this repo's own `PersonalAccounting` configuration, `{{Config}}` = `PersonalAccounting` — a real, existing
prefix, not one you create. A different configuration substitutes its own equivalent project names.
