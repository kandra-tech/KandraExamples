# Constant code templates

Two `.cs` skeletons — a constant has no flat-vs-hierarchical or with-vs-without-lines split, so there's exactly
one shape per file, unlike `create-dictionary`/`create-document`'s multi-variant `data/` directories. Copy both,
replace every `{{Placeholder}}`. Not compiled as part of any project (nothing under `.claude/skills/` is
referenced from `FamilyGarage.slnx`) — this is copy-paste source, not real code. See
[`../reference.md`](../reference.md) for what `Constant<T>`/`IConstant`/`[KandraConstant]`/`{{TypeIdsClass}}`
actually mean; see [`../SKILL.md`](../SKILL.md) for the full step order and the "Hand-maintained files" audit
(short: these two files are the whole addition).

## Use — both, in this order

- `TypeIdConstant.cs` → `{{Config}}.Enums/{{TypeIdsClass}}/{{TypeIdsClass}}.{{Name}}.cs` (do this first — the
  constant class references it by name)
- `Constant.cs` → `{{Config}}.Application/Constants/{{Name}}Constant.cs`

## Placeholder legend

| Token | Meaning | Example |
|---|---|---|
| `{{Config}}` | Your configuration's root namespace — the project-name prefix (`{{Config}}.Application`, `{{Config}}.Enums`). In this repo that's `FamilyGarage`. | `FamilyGarage` |
| `{{Name}}` | Singular PascalCase constant name — also becomes the stored lookup key (`Name => "{{Name}}"`), the `{{TypeIdsClass}}` field name, and the label an admin sees verbatim (unlocalized) in the Constants admin list, so pick something you're fine showing raw. | `VatRate` |
| `{{TypeIdsClass}}` | Your configuration's centralized type-id constants class (reference.md §2) — the same `partial`, one-constant-per-sibling-file class Document/Dictionary entities use for their own `TypeId` (see `create-dictionary`'s `data/README.md`, which owns the canonical explanation of this convention since it's shared across every entity kind, not specific to constants). In this repo that's `FamilyGarageTypeIds`, in `{{Config}}.Enums/{{TypeIdsClass}}/`. | `FamilyGarageTypeIds` |
| `{{T}}` | The value's CLR type — drives which admin-UI input control renders automatically (`ConstantValueKind`: `string`→text, `decimal`/`int`→numeric, `bool`→switch, `DateTime`→date+time picker; anything else falls back to a raw-JSON text box). | `decimal` |
| `{{CompiledDefault}}` | The fallback value `GetAsync<C>()` returns when nothing has ever been written to the database for this constant — pick a real, safe default, not a placeholder value. Must be a valid `{{T}}` literal/expression for the primary-constructor base-call argument. | `0.20m` |
| `{{FreshGuid}}` | A **freshly generated** GUID literal (e.g. `Guid.NewGuid()` once, or any GUID generator) — never reuse an existing one. Goes into its own new file, `{{TypeIdsClass}}.{{Name}}.cs` (via `TypeIdConstant.cs`), never into `{{TypeIdsClass}}.cs` itself — see `reference.md` §2. | `3f9c2e11-...` |

If you're working in this repo's own `FamilyGarage` configuration, `{{Config}}` = `FamilyGarage` and
`{{TypeIdsClass}}` = `FamilyGarageTypeIds` — both real, existing (`{{TypeIdsClass}}`) or existing-project
(`{{Config}}`) names, not ones you create. A different configuration substitutes its own equivalent
`{{Config}}.Application`/`{{Config}}.Enums` projects and its own `{{TypeIdsClass}}`-equivalent class (creating
it on first use if it doesn't exist yet — same convention `create-dictionary` and `create-document` follow).
