# Document code templates

Eight `.cs` skeletons — three with-lines-vs-lineless pairs (Domain/Forms/Application), one shared ClientLib
template (identical either way), plus two single-constant templates shared with `create-dictionary`
(`TypeIdConstant.cs`/`NumberingBucketConstant.cs`) — copy the set you need, then replace every
`{{Placeholder}}` and delete anything commented `// OPTIONAL — delete if unused`. These are not compiled as
part of any project (nothing under `.claude/skills/` is referenced from `FamilyGarage.slnx`) — they're copy-paste
source, not real code. See [`../reference.md`](../reference.md) for what each attribute/base type actually
means; see [`../SKILL.md`](../SKILL.md) for the full step order (migrations, resx, nav, verify) and the full
list of other hand-maintained files these eight don't cover.

## Line-bearing document (the common case) — use five

- `Domain.WithLines.cs` → `{{Config}}.Domain/Entities/Documents/{{Name}}.cs`
- `Forms.WithLines.cs` → `{{Config}}.Forms/Documents/{{Name}}.cs`
- `Application.WithLines.cs` → `{{Config}}.Application/Documents/{{Name}}.cs`
- `ClientLib.cs` → `{{Config}}.ClientLib.Common/ApiClients/I{{PluralName}}ApiClient.cs`
- `TypeIdConstant.cs` → `{{Config}}.Enums/{{TypeIdsClass}}/{{TypeIdsClass}}.{{Name}}.cs`
- `NumberingBucketConstant.cs` → `{{Config}}.Domain/Numbering/{{NumberingBucketsClass}}.{{Name}}.cs` (skip
  only if this document hand-types its `Code` instead of auto-numbering — rare)

## Lineless (header-only) document — same five, swap the Domain/Forms/Application trio

- `Domain.Lineless.cs` → `{{Config}}.Domain/Entities/Documents/{{Name}}.cs`
- `Forms.Lineless.cs` → `{{Config}}.Forms/Documents/{{Name}}.cs`
- `Application.Lineless.cs` → `{{Config}}.Application/Documents/{{Name}}.cs`
- `ClientLib.cs` / `TypeIdConstant.cs` / `NumberingBucketConstant.cs` — same three files, same targets, no
  lineless variant needed (`IDocumentApiClient<>`/the identity+numbering conventions don't vary by shape).

Decide with-lines vs. lineless the same way you'd decide it for real: does this document need a tabular
child table (order lines, movement lines)? If yes, with-lines. A lineless document is the exception — reach
for it only when the document is genuinely header-only, the way `AccountTransaction` (a single manual
posting entry) is — see `../reference.md` §10 for that worked example.

## Placeholder legend

| Token | Meaning | Example |
|---|---|---|
| `{{Config}}` | Your configuration's root namespace — the project-name prefix every plane project shares (`{{Config}}.Domain`, `{{Config}}.Forms`, `{{Config}}.Application`, `{{Config}}.Enums`). In this repo that's `FamilyGarage`. | `FamilyGarage` |
| `{{Name}}` | Singular PascalCase document name — also the `[KandraDocumentEntity(TableName = "Doc_{{Name}}")]` table-name segment. | `PurchaseOrder` |
| `{{PluralName}}` | Plural form — table name segment for the lines table, and the `[KandraDocumentForm(Name = ...)]` route/client-name value. | `PurchaseOrders` |
| `{{TypeIdsClass}}` | Your configuration's centralized type-id constants class (reference.md §7) — `partial`, one constant per sibling file, shared with dictionaries. In this repo that's `FamilyGarageTypeIds`, in `{{Config}}.Enums/{{TypeIdsClass}}/`. | `FamilyGarageTypeIds` |
| `{{NumberingBucketsClass}}` | Your configuration's numbering-bucket constants class (reference.md §8) — same `partial`-per-file split, shared with dictionaries. In this repo that's `NumberingBuckets`, in `{{Config}}.Domain.Numbering`. | `NumberingBuckets` |
| `{{FreshGuid}}` | A **freshly generated** GUID literal — never reuse an existing one. Goes into its own new file, `{{TypeIdsClass}}.{{Name}}.cs`, never into `{{TypeIdsClass}}.cs` itself — reference.md §7. | `3f9c2e11-...` |
| `{{NumberingPrefix}}` | Short human-readable code prefix embedded in the formatted `Code` (e.g. `entity.Code = "PO.2612345"`). Purely cosmetic, 2-4 letters is typical. | `PO` |
| `{{NumberingDigits}}` | Zero-padding width for the numeric part (`D`-format digit count) in the formatted `Code`. | `5` |

If you're working in this repo's own `FamilyGarage` configuration, `{{Config}}` = `FamilyGarage`,
`{{TypeIdsClass}}` = `FamilyGarageTypeIds`, `{{NumberingBucketsClass}}` = `NumberingBuckets` — those are real, existing
classes (already holding one entry per existing document), not ones you create. A different configuration
substitutes its own equivalents (creating them on first use if they don't exist yet).
