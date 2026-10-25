# Dictionary code templates

Ten `.cs` skeletons — four flat-vs-hierarchical pairs, one per plane (Domain/Forms/Application/ClientLib), plus
two single-constant templates shared by both shapes (`TypeIdConstant.cs`/`NumberingBucketConstant.cs`) — copy
the set you need, then replace every `{{Placeholder}}` and delete anything commented `// OPTIONAL — delete if
unused`. These are not compiled as part of any project (nothing under `.claude/skills/` is referenced from
`FamilyGarage.slnx`) — they're copy-paste source, not real code. See [`../reference.md`](../reference.md) for what
each attribute/base type actually means; see [`../SKILL.md`](../SKILL.md) for the full step order (migrations,
resx, UI, verify) and the full list of other hand-maintained files (resx keys, nav entry) these ten don't
cover.

## Flat dictionary — use all five

- `Domain.Flat.cs` → `{{Config}}.Domain/Entities/Dictionaries/{{Name}}.cs`
- `Forms.Flat.cs` → `{{Config}}.Forms/Dictionaries/{{Name}}.cs`
- `Application.Flat.cs` → `{{Config}}.Application/Dictionaries/{{Name}}.cs`
- `ClientLib.Flat.cs` → `{{Config}}.ClientLib.Common/ApiClients/I{{PluralName}}ApiClient.cs`
- `TypeIdConstant.cs` → `{{Config}}.Enums/{{TypeIdsClass}}/{{TypeIdsClass}}.{{Name}}.cs` (skip
  `NumberingBucketConstant.cs` only if this dictionary hand-types its `Code`, see below)
- `NumberingBucketConstant.cs` → `{{Config}}.Domain/Numbering/{{NumberingBucketsClass}}.{{Name}}.cs`

## Hierarchical dictionary — use all six (numbering bucket template copied twice, once per kind)

- `Domain.Hierarchical.cs` → `{{Config}}.Domain/Entities/Dictionaries/{{Name}}.cs`
- `Forms.Hierarchical.cs` → `{{Config}}.Forms/Dictionaries/{{Name}}.cs`
- `Application.Hierarchical.cs` → `{{Config}}.Application/Dictionaries/{{Name}}.cs`
- `ClientLib.Hierarchical.cs` → `{{Config}}.ClientLib.Common/ApiClients/I{{PluralName}}ApiClient.cs`
- `TypeIdConstant.cs` → **one** file: `{{TypeIdsClass}}.{{Name}}.cs` — the dictionary's single TypeId, declared on `{{Name}}Node`/`{{Name}}NodeDto` and shared by folder and leaf
- `NumberingBucketConstant.cs` → **two** files: `{{NumberingBucketsClass}}.{{Name}}.cs` (leaf) and `{{NumberingBucketsClass}}.{{Name}}Folder.cs` (folder)

## Placeholder legend

| Token | Meaning | Example |
|---|---|---|
| `{{Config}}` | Your configuration's root namespace — the project-name prefix every plane project shares (`{{Config}}.Domain`, `{{Config}}.Forms`, `{{Config}}.Application`, `{{Config}}.Enums`). In this repo that's `FamilyGarage`. | `FamilyGarage` |
| `{{Name}}` | Singular PascalCase entity name. For hierarchical, this is the **leaf** type name — the root becomes `{{Name}}Node`, the folder `{{Name}}Folder`, automatically. | `Supplier` |
| `{{PluralName}}` | Plural form — table name segment and the `[Kandra*Form(Name = ...)]` route/client-name value. | `Suppliers` |
| `{{TypeIdsClass}}` | Your configuration's centralized type-id constants class (reference.md §6) — `partial`, one constant per sibling file. In this repo that's `FamilyGarageTypeIds`, in `{{Config}}.Enums/{{TypeIdsClass}}/`. | `FamilyGarageTypeIds` |
| `{{NumberingBucketsClass}}` | Your configuration's numbering-bucket constants class (reference.md §7) — same `partial`-per-file split. In this repo that's `NumberingBuckets`, in `{{Config}}.Domain.Numbering`. | `NumberingBuckets` |
| `{{FreshGuid}}` | A **freshly generated** GUID literal (e.g. `Guid.NewGuid()` once, or any GUID generator) — never reuse an existing one. Goes into its own new file, `{{TypeIdsClass}}.{{Name}}.cs` (via `TypeIdConstant.cs`), never into `{{TypeIdsClass}}.cs` itself — see reference.md §6. | `3f9c2e11-...` |
| `{{NumberingPrefix}}` | Short human-readable code prefix. | `SUP` |
| `{{NumberingDigits}}` | Zero-padding width for the numeric part (`D`-format digit count). | `6` |
| `{{FolderNumberingPrefix}}` / `{{FolderNumberingDigits}}` | Hierarchical only — the folder kind usually wants its own, shorter prefix/width. | `SUPF` / `3` |

Every plane template (`Application.*.cs`) assumes a numbering-based `Code` (the common case). If this
dictionary hand-types its `Code` instead (see `Currency` in reference.md §9), replace the
`OnNewAsync`/`OnFolderNewAsync`/`OnLeafNewAsync` body with `ValueTask.CompletedTask`, drop the
`ISimpleNumberingSystem` constructor parameter, and skip `NumberingBucketConstant.cs` entirely for this
dictionary — `TypeIdConstant.cs` is still required regardless (every dictionary needs a type identity; only
numbering is optional).

If you're working in this repo's own `FamilyGarage` configuration, `{{Config}}` = `FamilyGarage`,
`{{TypeIdsClass}}` = `FamilyGarageTypeIds`, `{{NumberingBucketsClass}}` = `NumberingBuckets` — those are real, existing
classes, not ones you create. A different configuration substitutes its own equivalents (creating them on
first use if they don't exist yet).
