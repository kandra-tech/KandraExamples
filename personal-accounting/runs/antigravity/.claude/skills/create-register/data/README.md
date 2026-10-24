# Register code templates

Seven `.cs` skeletons — two shared components (`Dimensions.cs`, mandatory for every family) plus optional
`Resources.cs`/`Details.cs`, and one row+register pair per family (`Balance.cs`, `Turnover.cs`,
`DocumentInfo.cs`, `PeriodicInfo.cs`) — copy the pieces you need, then replace every `{{Placeholder}}` and
delete anything commented `// OPTIONAL — delete if unused`. Unlike `create-dictionary`'s templates, every piece
here lands in **the same single file**: `{{Config}}.Domain/Entities/Registers/{{Name}}.cs`. These are not
compiled as part of any project (nothing under `.claude/skills/` is referenced from `PersonalAccounting.slnx`) — they're
copy-paste source, not real code. See [`../reference.md`](../reference.md) for what each attribute/base type
actually means; see [`../SKILL.md`](../SKILL.md) for the full step order (DI wiring, localization, migrations,
verify) and what's hand-maintained vs. generated.

## Every family — always start here

- `Dimensions.cs` → paste into `{{Config}}.Domain/Entities/Registers/{{Name}}.cs`. Mandatory for all four
  families.

## Then add, if needed

- `Resources.cs` → same file. **Balance/Turnover only**, and only if the shipped `QuantityResources`/
  `NonNegativeQuantityResources` (`Kandra.Domain.Registers`) don't fit — most registers reuse one of those and
  skip this file entirely.
- `Details.cs` → same file. Any family, only if the register needs a non-aggregated payload beyond dimensions
  (+ resources) — skip and use the shipped `NoDetails` (`Kandra.Domain.Registers`) otherwise.

## Then pick exactly one family

- `Balance.cs` → same file. Running-total register with a maintained balance table (e.g. `InventoryRegister`).
- `Turnover.cs` → same file. Movement log only, no balance table — **no live consumer in this repo**, read
  `../reference.md` §1/§8 before picking this over Balance/document-bound Info.
- `DocumentInfo.cs` → same file. A document *sets* a value (rate/price/setting) — the most common family in
  this repo (`ExchangeRatesRegister`/`PricesRegister`/`CostingMethodSettingsRegister`).
- `PeriodicInfo.cs` → same file. Independent write mode (imports, admin UI, dictionary behaviors — not document
  posting) — **no live consumer in this repo**, same caveat as Turnover.

## Placeholder legend

| Token | Meaning | Example |
|---|---|---|
| `{{Config}}` | Your configuration's root namespace (`{{Config}}.Domain`, ...). In this repo that's `PersonalAccounting`. | `PersonalAccounting` |
| `{{Name}}` | Singular PascalCase register name — becomes the row type's prefix (`{{Name}}Movement`/`{{Name}}Record`), the balance type's prefix (`{{Name}}Balance`), and the register class name (`{{Name}}Register`). | `Inventory` |
| `{{TableName}}` | The `Reg_{{Name}}` table-name convention every live register follows — set explicitly via `[KandraRegisterEntity(TableName = "...")]`, never left to infer. Balance family needs two: `Reg_{{Name}}` (movement log) and `Reg_{{Name}}_Balance`. | `Reg_Inventory` / `Reg_Inventory_Balance` |
| `{{RegisterCaptionKey}}` | The resx key `[RegisterCaption(...)]` resolves — by convention just `{{Name}}` itself, matching every live register. Must exist in the `{{Config}}.Localization` resx (English required) (`../reference.md` §10). | `Inventory` |
| `{{RegisterDescription}}` | Plain-English `[Description]` of the register class: what it measures or stores, what a receipt vs an expense movement (`IsExpense`) means, and how to read it (balance, turnover, latest value as of a date). | `Money per account in the account's own currency; Income/Opening balances add, Expense subtracts, Transfer moves between accounts.` |
| `{{DimensionDescription}}` | `[Description]` of a dimension: what it identifies, and any sentinel value. | `The account whose money this is.` |
| `{{ResourceDescription}}` | `[Description]` of a resource: unit and currency (and the date of any conversion), that a movement value is never negative (direction is `IsExpense`), whether the balance may go negative. | `Amount in the account's currency; the balance may be negative for a credit card.` |
| `{{DetailDescription}}` | `[Description]` of a detail: what the value is, its unit, what null means; on an Info record, that it is valid from the record's Period until the next one. | `Units of the base currency per one unit of the currency, valid from this date until the next rate.` |
| `{{DimensionField}}` | One dimension property — either a `Guid` FK paired with a `[JsonIgnore]` nav, or a bare scalar (see `../reference.md` §2 for when to use which). | `WarehouseId` / `Warehouse` |
| `{{ResourceField}}` | One accumulated numeric member of the Resources class, with its own `[Precision(p, s)]`. | `Quantity` |
| `{{DetailField}}` | One non-aggregated stored/movement-scoped member of the Details class. | `SerialNumber` |
| `{{RowType}}` | `Movement` (Balance/Turnover) or `Record` (document-bound/independent Info) — the suffix on `{{Name}}{{RowType}}`, used when writing the register from a document Behavior (`../reference.md` §5/§9). | `Movement` |

Every template repeats its own `using`/`namespace` block since each is written to stand alone — once you've
pasted two or more into the same `{{Name}}.cs` file, keep only one `namespace` declaration and merge the
`using` lists (dedupe, drop what ends up unused — e.g. `Kandra.Attributes.Printable` if you never use
`[Format]`). `Inventory.cs`/`ExchangeRates.cs` (mentioned below) show what the merged result looks like in a
real file.

If you're working in this repo's own `PersonalAccounting` configuration, `{{Config}}` = `PersonalAccounting` and the register's
file target is `PersonalAccounting.Domain/Entities/Registers/{{Name}}.cs`, alongside `Inventory.cs`/`ExchangeRates.cs`/
`Prices.cs`/`CostingMethodSettings.cs` — real, existing sibling files worth opening side by side while filling
in placeholders. A different configuration substitutes its own equivalent Domain project.
