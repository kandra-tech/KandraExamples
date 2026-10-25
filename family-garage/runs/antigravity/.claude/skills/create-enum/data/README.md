# Enum code templates

Four small skeletons. An enum has no Dto/Behavior/Validator, so there's no per-plane split — pick the enum
template that matches the role you chose in `../SKILL.md` step 1, add the type-identity file only for the
subconto role, and paste the resx snippet. Not compiled as part of any project (nothing under `.claude/skills/`
is referenced from `FamilyGarage.slnx`) — copy-paste source. See [`../reference.md`](../reference.md) for what each
attribute means and [`../SKILL.md`](../SKILL.md) for the full step order.

## Use

- **Plain enum:** `Enum.Plain.cs` → `{{Config}}.Enums/{{Name}}.cs`
- **Subconto-backing enum:** `TypeIdConstant.cs` → `{{Config}}.Enums/{{TypeIdsClass}}/{{TypeIdsClass}}.{{Name}}.cs`
  **first** (the enum references it by name), then `Enum.Subconto.cs` → `{{Config}}.Enums/{{Name}}.cs`
- **Either:** `Localization.resx.xml` — paste its `<data>` entries into the English (and, when translating, the uk/ru) `{{Config}}.Localization`
  resx files (one entry per `[EnumCaption]` key per file).

## Placeholder legend

| Token | Meaning | Example |
|---|---|---|
| `{{Config}}` | Your configuration's root namespace — the project-name prefix (`{{Config}}.Enums`, `{{Config}}.Localization`). In this repo that's `FamilyGarage`. | `FamilyGarage` |
| `{{Name}}` | Singular PascalCase enum name — also the middle segment of every `Enum_{{Name}}_{{Member}}` resx key and, for the subconto role, the `{{TypeIdsClass}}` field name. | `PaymentKind` |
| `{{MemberA}}`, `{{MemberB}}` | Member names. Add more by repeating the block; each needs its own explicit numeric value and `[EnumCaption]`. | `Cash`, `BankTransfer` |
| `{{MemberA label}}`, `{{MemberB label}}` | The translated display text for that member, per resx file language. | `Cash` / `Готівка` |
| `{{TypeIdsClass}}` | Your configuration's centralized type-id constants class — `partial`, one constant per sibling file, shared with dictionaries/documents/constants (`create-dictionary`'s `data/README.md` owns the canonical explanation). In this repo that's `FamilyGarageTypeIds`, in `{{Config}}.Enums/{{TypeIdsClass}}/`. | `FamilyGarageTypeIds` |
| `{{FreshGuid}}` | A **freshly generated** GUID for the enum's `[TypeId]` — never reuse an existing one. Goes into its own new file, never into `{{TypeIdsClass}}.cs` itself. | `3f9c2e11-...` |
| `{{FreshGuidA}}`, `{{FreshGuidB}}` | One **fresh, unique** GUID per member's `[SubcontoId]` — unrelated to `{{FreshGuid}}`, and permanent once posted against. | `4ef39b2a-...` |

If you're working in this repo's own `FamilyGarage` configuration, `{{Config}}` = `FamilyGarage` and
`{{TypeIdsClass}}` = `FamilyGarageTypeIds` (which `dotnet new` rewrites to your scaffolded name). The
`{{Config}}.Enums` project always exists; the `{{TypeIdsClass}}` class is created on first use if no earlier
dictionary/document/constant has minted a type identity yet — the same convention `create-dictionary` and
`create-document` follow.
