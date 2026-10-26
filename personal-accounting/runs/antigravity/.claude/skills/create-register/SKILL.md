---
name: create-register
description: Add a new register (analytical storage — inventory balances, exchange rates, and similar accumulation/reference data written by document posting or independently) to a Kandra configuration. Covers all four register families — Balance, Turnover, document-bound Info, and independent/periodic Info. Use when the user asks to add a new register, ledger, balance tracking, or accumulation/analytical storage.
---

# Creating a register

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

A register is analytical storage with **no form, no grid, no `Code`/`Name`, no numbering, and no type identity**
— see [`reference.md`](reference.md) for the full conceptual explanation (why registers don't fit the
Dictionary/Document three-project pattern, every base class/attribute's real signature, the `ISubmitScope`
keyed-DI guarantee and why it exists, and two fully worked examples, all self-contained so you don't need to go
read engine source to use this skill). This recipe is written against **this repo's one reference
configuration, `PersonalAccounting`**; substitute your own configuration's root project names if you're working in a
different one.

Unlike a Dictionary/Document, a register is **one class-library project, one file**: everything lives in
`{Config}.Domain/Entities/Registers/{Name}.cs`. There's no Forms/Application-plane split to make, because a
register never has its own `*Dto`/`*Behavior`/`*Validator` — the code that writes it lives in whichever
Document's Behavior posts to it (`create-document`'s territory, not this skill's).

Copy-paste `.cs` skeletons for every family live in [`data/`](data/) (`data/README.md` explains the
`{{Placeholder}}` tokens and which files a given family needs) — start from those rather than a blank file.

**`[Description]` on every register class and on every Dimensions/Resources/Details property.** The MCP tools `list_registers`/`describe_register`
(and the agent-facing `query_register`) surface `System.ComponentModel.Description` from the register's *handle class*
(`{{Name}}Register`, the one deriving `BalanceRegister<>`/`TurnoverRegister<>`/`DocumentInfoRegister<>`/
`PeriodicInfoRegister<>`) and from each Dimensions/Resources/Details **property** — registers have no Dto, so these are
the only place an agent can learn what the register means. Say what it measures, the sign and unit of each resource, and any
sentinel value (e.g. `PartyId = Guid.Empty` = "no party"); on resources and details always state the unit/currency and how
to read the value (a caption like "Amount" says nothing about which currency or sign). The `data/` skeletons carry a
`[Description]` placeholder on every property: fill each one.
`[Caption]`/`[Format]` stay on the same properties. Keep the `[Description]` text English, plain and short.

## 1. Pick a family

Read `reference.md` §1 for the full comparison and default guidance. Short version: **Balance** for a running
total that must never go negative (stock, credit) — the only family with a maintained totals table. **Document-
bound Info** if a document *sets* a value (a rate, a price, a period setting) rather than accumulating one —
this is the most common shape in this repo (`ExchangeRatesRegister`/`PricesRegister`/
`CostingMethodSettingsRegister` are all this family). **Turnover** (period totals without a running balance) has two live examples in the reference configuration (`Sales`,
`Purchases`; the one hand-registered DI line is in step 5). **Independent/periodic Info** has no form and no live
consumer, so a value users must *enter* (a rate, a price) is a **document-bound Info register written by a "set values"
document**, not an independent one. Which register for which spec wording: [`plan-configuration`](../plan-configuration/reference.md).

## 2. Dimensions class — every family needs one

Copy `data/Dimensions.cs` into the register's own file (§4 below — all classes for one register share a single
file). `[KandraGeneratedEquality]` (mandatory, `sealed partial`) fills in `Equals`/`GetHashCode`/`ToString` and
the EF-translatable `BuildEqualityPredicate()` — never hand-write any of those four. `reference.md` §2 covers
the FK-pairing convention (Guid-Id + `[JsonIgnore]` nav) versus a deliberately bare scalar dimension, and the
nullable-dimension hazard on a Balance register specifically.

## 3. Resources (Balance/Turnover only) and/or Details (any family)

**Resources** (Balance/Turnover): reuse `Kandra.Domain.Registers.QuantityResources`/`NonNegativeQuantityResources`
if a single guarded-or-not quantity is all you need — most registers do. Only copy `data/Resources.cs` and write
your own if you need more than one accumulated field; `reference.md` §3a has the generic-math operator contract
the split between the movement check (`EnsureNonNegative()`) and the balance guard (`EnsureValidBalance()`),
and why neither can be merged into a throwing arithmetic operator.

**Details** (any family, optional): copy `data/Details.cs` for the non-aggregated payload, or use the shipped
`NoDetails` if there's nothing beyond dimensions (+ resources). `reference.md` §3b explains the movement-scoped-
attribute-vs-stored-value distinction between the accumulation and Info families.

## 4. Register + row entities

Copy the block matching your family from `data/Balance.cs` / `data/Turnover.cs` / `data/DocumentInfo.cs` /
`data/PeriodicInfo.cs` into `{{Config}}.Domain/Entities/Registers/{{Name}}.cs` (one new file, components +
row entity + register class together, top to bottom — mirror `Inventory.cs`/`ExchangeRates.cs`).
`[KandraRegisterEntity(TableName = "Reg_...")]` is mandatory on every Balance/Movement/Record class;
`[RegisterCaption("{{Name}}")]` goes on the Movement/DocumentInfoRecord row only (not Balance) — technically
optional, add it anyway (§7 below, `reference.md` §4/§9 has the exact shapes and the one field you assign
yourself, `LineId`). The text of `EnsureValidBalance()` reaches the user as it is, in English and with the row's dimension
ids appended by the engine: for a rule a user can break (un-posting a purchase whose debt has been paid off in part),
check it in the document behavior first and throw an `InvalidStateException` with a localized message; the guard stays
as the last line of defence.

## 5. DI wiring — mostly generated, Turnover is the exception

Balance, document-bound Info, and independent/Periodic Info are discovered from the `[KandraRegisterEntity]`-
attributed classes and wired automatically by `Kandra.Generators.Di.Persistence` — confirm after building that
`AddBalanceRegister<...>()`/`AddDocumentInfoRegister<...>()`/`AddPeriodicInfoRegister<...>()` appears in the
generated `Generated{Config}PersistenceDiExtensions.g.cs`. **Turnover is not wired by that generator** — a
Movement with no matching Balance is silently skipped (`reference.md` §6 explains exactly why, and links the
generator source to re-check this claim against). Register it by hand instead, one line in
`{{Config}}.Persistence/ConfigureServices.cs`'s `AddPersonalAccountingPersistence()`:

```csharp
services.AddTurnoverRegister<{{Name}}Movement, {{Name}}Dimensions, {{Name}}Resources, {{Name}}Details /* or NoDetails */>();
```

EF configuration needs **no manual step for any family** — `Kandra.Generators.EfConfig` emits the concrete
`IEntityTypeConfiguration<T>` (table name, owned-type mapping, FK relationships, `[Precision]` columns) for
every `[KandraRegisterEntity]`-attributed class automatically. Never hand-write an `EntityConfigurations/*.cs`
file for a register.

## 6. Wire the writing document's Behavior

The register-write code itself lives in the Document's own Behavior file
(`{{Config}}.Application/Documents/{{Doc}}.cs`), not a new file — see `create-document` for everything else
about that class. Add `[RegisterTransactions(typeof({{Name}}{{RowType}}))]` on the Behavior class (repeatable
if the document touches more than one register), and call `ISubmitScope.GetWriter<...>()` inside
`OnSubmitAsync`/`OnUnsubmitAsync`/`OnDeleteAsync` — this is the **only** legal way to write a register anywhere
in the codebase, enforced by keyed DI, not just convention (`reference.md` §5 has the full contract, why it's
keyed, and the exact writer interface per family — `IBalanceRegisterWriter<,>`/`ITurnoverRegisterWriter<>`/
`IDocumentInfoRegisterWriter<>`/plain-injectable `IInfoRegisterWriter<,,>` for the independent tier). Follow
`GoodsReceiptBehavior`'s `OnSubmitAsync` (`reference.md` §8) for the simple single-write case, or
`RateImportBehavior`'s for the Info-family equivalent.

## 7. Localization

Every `[Caption]`/`[RegisterCaption]` key referenced anywhere in the new register's classes needs an entry in
`{{Config}}.Localization` resx files (English required, uk/ru optional) — same rule and same "missing key silently renders
raw" failure mode as a Dictionary Dto's `[Caption]` keys. `reference.md` §10 has the full explanation; this is
the one real cross-cutting shared-file edit a new register needs.

## 8. Reading from the register

Constructor-inject the matching reader (`IBalanceReader<,>`/`ITurnoverReader<>`/`IInfoRegisterReader<>`) into a
repository, report, or service — readers are plain, ungated services, not gated behind `ISubmitScope` the way
writers are. Use `DimensionPredicateBuilder.BuildOr`/`.BuildOrBatches` to filter by a set of dimension keys
rather than hand-rolling reflection or loading everything client-side — `reference.md` §7 has the full pattern
and the live `ReportsRepository` example.

## 8a. Writer and reader types at a glance (copy these, do not guess generics)

| Family | Writer (in a document behavior, `registers.GetWriter<...>()`) | Reader (constructor-inject anywhere) |
|---|---|---|
| Balance | `IBalanceRegisterWriter<{{Name}}Movement, {{Name}}Balance>` | `IBalanceReader<{{Name}}Movement, {{Name}}Balance>` (also has every `ITurnoverReader` method) |
| Turnover | `ITurnoverRegisterWriter<{{Name}}Movement>` | `ITurnoverReader<{{Name}}Movement>` |
| Document-bound Info | `IDocumentInfoRegisterWriter<{{Name}}Record>` | `IInfoRegisterReader<{{Name}}Record>` |
| Independent Info | `IInfoRegisterWriter<{{Name}}Record, {{Name}}Dimensions, {{Name}}Details>` (plain DI) | `IInfoRegisterReader<{{Name}}Record>` |

Writers: `PostAsync(documentId, period, rows, ct)`, `UnpostAsync(documentId, ct)`, `DeleteAsync(documentId, ct)` (the
independent one: `SetAsync(dimensions, period, details, ct)`, `DeleteAsync(dimensions, period, ct)`). Readers:

| Call | Returns | Bounds |
|---|---|---|
| `Balances(q => q.Where(...), ct)` | current balances | - |
| `Balances(at, adjustBalances, adjustMovements, ct)` | balances as of `at` | **includes** movements dated exactly `at` |
| `Movements(from, to, q => ..., ct)` (or a projection overload) | the movement log | **inclusive** at both ends; with the entity overload add `q.Include(m => m.Document)` (and any dimension navigation) or `m.Document` is null - a projection (`q => q.Select(m => new Row { Number = m.Document.Code })`) needs no Include |
| `SliceOfLast(at, q => ..., ct)` | latest record per key with `Period <= at` | inclusive |
| `Records(from, to, q => ..., ct)` | raw info records | inclusive at both ends |

Traps: a document's `Date` (the movement's `Period`) carries a time, so a period that ends "on the 31st" must end at
`to.Date.AddDays(1).AddTicks(-1)`, and an opening balance "at the start of the 1st" is `Balances(from.AddTicks(-1), ...)`.
The `Balances(dimensionKeys, adjust, ct)` **extension** cannot infer its four type arguments: either spell them all
(`reader.Balances<{{Name}}Movement, {{Name}}Balance, {{Name}}Dimensions, {{Name}}Resources>(keys, null, ct)`) or filter with
`Balances(q => q.Where(b => b.Dimensions.X == id), ct)`. When in doubt about what is registered, read the generated
`Generated.Net/.../Generated{Config}PersistenceDiExtensions.g.cs`: it lists every register's `Add...Register<...>()` call with its exact types.

**A decrease is a direction, never a negative number.** Every register family rejects a negative resource on a movement
(`EnsureNonNegative()` runs in both the Balance and the Turnover writer). A refund, a sale that reduces a cost total, a
return: post the positive amount with `IsExpense = true`, and read it back signed, `m.IsExpense ? -m.Resources.Amount :
m.Resources.Amount` (a Balance reader's `Balances` does this for you; `Movements` hands you the raw rows). Never empty
`EnsureNonNegative()` to let a minus through: the register's `[Description]` would then lie about its data, and every
reader that respects `IsExpense` disagrees with the reports that don't.

**Read through the readers, not the tables.** `db.Reg_X` sees un-posted rows (`UnpostAsync` sets `Active = false` and keeps
them for the log) and the movements of deleted documents; every reader filters `Active` for you. The same goes for
documents and dictionaries read straight from the `DbContext`: the model has no soft-delete filter, so `!IsDeleted` (and
`IsActive` for posted documents) is yours to write. "The latest value per key" is `SliceOfLast`, not an
`OrderByDescending(...).First()` per vehicle per item in a loop.

## 9. EF migration

Same three-provider command as [`create-dictionary`](../create-dictionary/SKILL.md) step 6/7 — nothing register-
specific, since EF configuration is fully generated (step 5).

## 10. Verify

`dotnet build PersonalAccounting.slnx`, check the generated (or, Turnover only, hand-registered) DI call and the generated
`{{Name}}{{Row}}Configuration.g.cs` both exist, `dotnet test PersonalAccounting.slnx` — `PersonalAccounting.Tests/Registers/` has the
closest existing coverage shape to model new tests on (`reference.md` §12 lists exactly which four test shapes
and what each one proves). Then exercise it live: post a document that writes the register, confirm the
balance/record changed via a report or the register's own reader, check the register-transactions tab on the
document's View page, unsubmit, confirm it reverted.

## If something here doesn't match the engine

`reference.md` is a point-in-time snapshot of engine source, not a live view — if a build error contradicts a
signature it or these templates show, the code wins. Re-verify by grepping the type/attribute name under
`src/KandraCore/Kandra.Domain/Registers/`, `Kandra.Persistence.Abstractions/Registers/`, `Kandra.Persistence/
Registers/`, or `Kandra.Attributes/Entities/`/`.Naming/`/`.Application/`, then fix `reference.md`/`data/` so the
next person doesn't hit the same surprise.

## Optional last step: demo objects

If you ship demo data (`ImportPackages/*-demo.json`), add documents that write to the new register (the register has no demo rows of its own)
to it: stable `$id`s, `$ref`/`$code` references, date tokens, a `version` bump, then a dry run. See
[`author-import-package`](../author-import-package/SKILL.md).
