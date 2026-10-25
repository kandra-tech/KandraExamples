# Register engine — reference

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

Self-contained explanation of every engine type/attribute `SKILL.md` walks through — written so you don't need
to go read `src/KandraCore/*` source to use it. Exact signatures as of the last time this was verified against the
engine; if a build error contradicts something here, the code wins (see "If this reference goes stale" at the
bottom). Ready-to-copy `.cs` skeletons matching everything below live in [`data/`](data/) — pull the pair for
whichever family you picked in `SKILL.md` step 1.

## What a Register actually is

Analytical/accumulation storage — inventory balances, exchange rates, price lists, settings-by-period — with
**no form, no grid, no `Code`/`Name`, no numbering, and (with one exception below) no type identity at all**.
This is the real dividing line against a Dictionary or Document: a register is never edited through a CRUD
screen, it's only ever written by document-posting code (or, for one specific tier, by plain application code)
and read back through typed query services. Concretely, a register never has a `*Dto`/`*Behavior`/`*Validator`
— there is no Forms/Application plane for it the way Dictionary/Document/Report/DataProcessor each have. Every
class you write for a register lives in `{Config}.Domain/Entities/Registers/`, on the same plane as the plain
EF entities.

One consequence worth calling out explicitly, because it looks like it contradicts the root `CLAUDE.md`'s "*Base
entity classes carry zero UI/authorization/navigation attributes" rule: register component classes (Dimensions/
Resources/Details) **do** carry `[Caption]`/`[Format]`/`[Precision]` directly, right there on the Domain-plane
class. That rule is about Dictionary/Document `*Base` entities specifically — it exists because those entities
have a separate `*Dto` to carry UI metadata instead. Registers have no `*Dto`, so their one and only class *is*
where display metadata for the register-transactions viewer (§9) has to live. Not a violation, just a different
entity kind with a different plane shape.

**No type identity.** Unlike every Dictionary/Document, a register never implements `IEntityWithTypeId`, never
gets a `[TypeId(...)]`, and never mints a `FamilyGarageTypeIds.{Name}` constant (root `CLAUDE.md` §6's dictionary
convention doesn't apply here at all — skip that step entirely). The one place a *reference to* a document's
type id shows up near a register is `[ReferenceOf(TypeIdProperty = ..., PossibleTypes = [...])]` (§9) — that's
resolving a *sibling* Guid field against another entity's `TypeId`, not declaring one for the register itself.

**Written only from document posting, except one tier.** Balance, Turnover, and document-bound Info registers
are writable only via `ISubmitScope.GetWriter<TWriter>()`, called from inside a document Behavior's
`OnSubmitAsync`/`OnUnsubmitAsync`/`OnDeleteAsync` (§5). The one exception is the **independent/periodic Info**
tier (§4), whose writer is a plain, ordinary DI service — by design, since that tier models reference-style
state (imports, admin UI, dictionary behaviors), not a posting consequence.

**`[Description]` goes on the register handle class and on every Dimensions/Resources/Details property.** `describe_register` (and
`list_registers`) reflect `System.ComponentModel.DescriptionAttribute` off the `{{Name}}Register` class
(`Kandra.Generators.Shared`'s `EntityDiscovery` -> the generated `IRegisterMetadataProvider`) and off each
Dimensions/Resources/Details property (`RegisterFieldDiscovery`) - not off a Dto, since a register has none, and not off
the Balance/Movement/Record row classes. State what the register means, what each resource measures (unit, sign, whether
it can go negative), and every sentinel (`InventoryDimensions.PartyId = Guid.Empty` = no party / moving-average item).
`using System.ComponentModel;` is needed. A document that writes the register still documents its own side through its
Dto `[Description]` and `[RegisterTransactions(typeof(...))]`; the register's `[Description]` is the register's own contract.

## 1. Pick a family

| Family | Base class | Balance table? | Write path | Live consumer(s) in `FamilyGarage` |
|---|---|---|---|---|
| **Balance** | `BalanceRegister<TMovement,TBalance>` | Yes — maintained running totals, atomic delta upserts | Document posting only, keyed `IBalanceRegisterWriter<,>` | `InventoryRegister` (§8) |
| **Turnover** | `TurnoverRegister<TMovement>` | No — the movement log *is* the register | Document posting only, keyed `ITurnoverRegisterWriter<>` | **None.** Writer + DI extension (`AddTurnoverRegister<>()`) exist and compile, but no consumer and no test in this repo exercises them — treat as validated-by-design, not validated-by-use (`src/KandraCore/CLAUDE.md`'s own words: "no live consumer"), not equal footing with Balance/Info |
| **Document-bound Info** | `DocumentInfoRegister<TRecord>` | No — a replace-by-recorder log, no accumulation | Document posting only, keyed `IDocumentInfoRegisterWriter<>` | `ExchangeRatesRegister`, `PricesRegister`, `CostingMethodSettingsRegister` (§8) |
| **Independent/Periodic Info** | `PeriodicInfoRegister<TRecord>` | No | **Plain injectable** `IInfoRegisterWriter<,,>` — not keyed | **None yet.** DI-wired (`AddPeriodicInfoRegister<...>()` is generated the moment such a record type exists) but nothing in this repo uses it today |

Default to **Balance** if you need a running total that must never go negative (stock, available credit, ...).
Default to **document-bound Info** if a document *sets* a value (a rate, a price, a setting effective from a
date) rather than *accumulating* one — three of the four live registers in this repo are exactly this shape.
Reach for **Turnover** when you need period totals and not a maintained balance (`Sales`/`Purchases` are the examples);
reach for **independent Info** only if the data must be settable outside document posting entirely - it has no form of its
own and nothing uses it yet, so a value users enter is almost always a document-bound Info register fed by a "set values"
document (`RateImport` is the example), shown by a report.

There's also a bare `InfoRegister<TRecord>` base one level above Periodic/Document-bound (`Kandra.Domain.
Registers.InfoRegister.cs`) — it exists purely as the shared ancestor those two derive from; nothing in the
engine discovers or wires a register declared directly against it, so don't use it as a family on its own.

## 2. Dimensions class — every family needs exactly one

```csharp
[KandraGeneratedEquality]
public sealed partial class {Name}Dimensions : IRegisterDimensions<{Name}Dimensions>
{
    [Caption("Warehouse")]
    public Guid WarehouseId { get; set; }
    [JsonIgnore]
    public Warehouse Warehouse { get; set; } = null!;
    // more Guid-Id + [JsonIgnore] nav pairs — this is the register's analytical key
}
```

`src/KandraCore/Kandra.Domain/Registers/IRegisterDimensions.cs`:

```csharp
public interface IRegisterDimensions<TSelf> : IEquatable<TSelf>
    where TSelf : class, IRegisterDimensions<TSelf>
{
    Expression<Func<TSelf, bool>> BuildEqualityPredicate();
    static abstract void VisitKeys(IDimensionKeyVisitor<TSelf> visitor);
    TSelf Clone();
}
```

`[KandraGeneratedEquality]` (must be `sealed partial`) fills in `Equals`/`GetHashCode`/`ToString`,
`BuildEqualityPredicate()` — an EF-translatable equality expression against the instance's own values, used by
`DimensionPredicateBuilder` (§7) to build a targeted `WHERE` clause instead of loading a whole balance table
client-side — `VisitKeys(...)` (each key member as a typed `d => d.X` expression, which the engine uses to order and join by dimensions, e.g. in `IRegisterMaintenance<,>`'s streamed rebuild) and `Clone()` (a copy of the key values, navigations left unset). Don't hand-write any of these
members. Keep the class to genuinely analytical keys — don't
fold in non-key attributes (those belong in Resources/Details, §3).

A `Guid`/`Guid?` dimension property is either FK-paired with a `[JsonIgnore]` navigation (the normal case,
resolved by dictionary FK convention) or a deliberately bare scalar with no navigation at all — see
`InventoryDimensions.PartyId` in §8 for why a register sometimes wants the latter (no honest row exists for a
sentinel value, or the target type varies per row). A bare scalar dimension still needs `[ReferenceOf(...)]`
(§9) if you want the register-transactions viewer to resolve it to something better than a raw Guid.

**Nullable dimension columns are a real cross-provider hazard on a Balance register** — a nullable FK becomes
part of the balance table's unique upsert-target index, and NULL-in-unique-index semantics differ between SQL
Server and PostgreSQL. `CostingMethodSettingRecord` (a document-bound Info register, no balance/upsert target)
gets away with nullable `WarehouseId`/`ItemId` as an intentional "unrestricted" wildcard — a Balance register's
dimensions should stay non-nullable.

## 3. Resources / Details — the payload, per family

### 3a. Balance or Turnover: Resources class (the accumulated value)

Reuse a shipped one if it fits — `Kandra.Domain.Registers.QuantityResources` (balance may go negative) or
`NonNegativeQuantityResources` (balance may not go negative — the stock-style guard). Both reject a negative
*movement*. Only write a new one if you need more than a single quantity:

```csharp
[KandraGeneratedEquality]
public sealed partial class {Name}Resources : IRegisterResources<{Name}Resources>
{
    [Precision(14, 3)] public decimal Quantity { get; init; }
    // more decimal members, each [Precision(...)]

    public static {Name}Resources AdditiveIdentity => new();

    public static {Name}Resources operator +({Name}Resources a, {Name}Resources b)
        => new() { Quantity = a.Quantity + b.Quantity /* + every other member */ };

    public static {Name}Resources operator -({Name}Resources a)
        => new() { Quantity = -a.Quantity /* + every other member, negated */ };

    public static {Name}Resources operator -({Name}Resources a, {Name}Resources b) => a + (-b);

    // Movement check — throw for EVERY member: a movement's values are never negative (direction is IsExpense).
    public void EnsureNonNegative()
    {
        if (Quantity < 0) throw new InsufficientBalanceException($"{nameof(Quantity)} cannot be negative ({Quantity}).");
        // + every other member
    }

    // Balance guard — optional. Omit it and the balance gets the same check as a movement (no member may go
    // negative). Declare it to relax that: empty body = balance may go negative; or check only some members.
    public void EnsureValidBalance()
    {
        if (Quantity < 0) throw new InsufficientBalanceException($"{nameof(Quantity)} cannot be negative ({Quantity}).");
    }

    public bool IsZero => Quantity == 0m; // && every other member == 0
}
```

`IRegisterResources<TSelf>` (`Kandra.Domain.Registers`) requires the four generic-math operators
(`IAdditionOperators`/`ISubtractionOperators`/`IUnaryNegationOperators`/`IAdditiveIdentity`) plus
`EnsureNonNegative()`/`IsZero`, plus `IEquatable<TSelf>`, `Clone()` and the static `VisitMembers(...)`, and offers `EnsureValidBalance()` with a
default implementation. `[KandraGeneratedEquality]` (must be `sealed partial`) generates `Equals`/`GetHashCode`/
`ToString`/`Clone()`/`VisitMembers` over the stored properties (the computed `IsZero` is skipped) — don't hand-write them. The
register writers compare with `Equals` to leave unchanged movements untouched when a document is re-posted, and
attach a `Clone()` to every row they write, so two movements may safely share one instance. The operators
are **static abstract members, closed per concrete type** — they don't inherit the way a virtual instance method
would, which is why `NonNegativeQuantityResources` redeclares all of them instead of just flipping a flag on
`QuantityResources`. Operators must stay **total** — never throw, never clamp — the engine relies on
`x + (-x) == AdditiveIdentity` holding for delta math. Only the two checks may throw, and only the engine calls
them: `EnsureNonNegative()` on every movement at `PostAsync` time (so it must cover **every** member), and
`EnsureValidBalance()` on each touched balance row after the delta is applied (defaults to `EnsureNonNegative()`;
declare it only to let the balance, or some of its members, go negative). They are separate on purpose: relaxing
the balance policy must never let a negative movement through.

`IsZero` also decides whether a balance row survives: when every resource of a row reaches zero after a post, the
writer deletes the row, so "no row" means zero (deliberate: batch-keyed balances would otherwise pile up dead rows
forever). The comparison is exact, so a posting that empties a balance must write that balance's exact remaining
value. Cost a partial draw as `quantity * (amount / balanceQuantity)`, but let a draw that takes the whole balance take
its whole `Amount`: in decimal arithmetic `q * (a / q)` is not always `a` (`1.91m / 3 * 3` is
`1.9100000000000000000000000001`), and that leftover keeps the row at zero quantity forever. The inventory allocation service of the worked example (`InventoryAllocationService.CostOf`) does this.

Note the design doc's `MoneyResources`/`QuantityAmountResources` never actually shipped — only `QuantityResources`/
`NonNegativeQuantityResources` exist in the engine today. If you need multiple accumulated fields (the design
doc's `QuantityAmountResources` example), write your own — `InventoryResources` (§8, `Quantity` + `Amount`) is
the real, current example to copy.

### 3b. Any family: Details class (the non-aggregated payload)

```csharp
[KandraGeneratedEquality]
public sealed partial class {Name}Details : IRegisterDetails<{Name}Details>
{
    [Precision(18, 6)] [Caption("Rate")] [Format("N6")]
    public decimal Rate { get; set; }
    // any stored, non-key, non-accumulated fields
}
```

`IRegisterDetails<TSelf>` requires `IEquatable<TSelf>` and `Clone()` for the same reason as Resources (§3a) —
`[KandraGeneratedEquality]` (must be `sealed partial`) generates both; don't hand-write them.

Use the shipped `NoDetails` (`Kandra.Domain.Registers`) if the register needs no extra payload — a Balance/
Turnover register with no movement-scoped attributes, or a pure existence/presence Info register. The entity
configuration `Ignore()`s the `Details` navigation entirely when `TDetails == NoDetails`, so there's no unmapped
column either way. On a Movement, Details is *movement-scoped* (rides along with each posted row, e.g.
`InventoryDetails.SerialNumber`); on an Info record family, Details **is** the stored value itself (there's no
separate "Resources" concept for Info registers — see §4).

## 4. Register + row entities, per family

All four shapes live together in `{Config}.Domain/Entities/Registers/{Name}.cs` — one file per register,
mirroring `Inventory.cs`/`ExchangeRates.cs` (components + row entity + register class, top to bottom).

**Balance:**
```csharp
[KandraRegisterEntity(TableName = "Reg_{Name}_Balance")]
public sealed class {Name}Balance : Balance<{Name}Dimensions, {Name}Resources> { }

[KandraRegisterEntity(TableName = "Reg_{Name}")]
[RegisterCaption("{Name}")]
public sealed class {Name}Movement : Movement<{Name}Dimensions, {Name}Resources, {Name}Details /* or NoDetails */> { }

public sealed partial class {Name}Register : BalanceRegister<{Name}Movement, {Name}Balance> { }
```

**Turnover:**
```csharp
[KandraRegisterEntity(TableName = "Reg_{Name}")]
[RegisterCaption("{Name}")]
public sealed class {Name}Movement : Movement<{Name}Dimensions, {Name}Resources, {Name}Details /* or NoDetails */> { }

public sealed partial class {Name}Register : TurnoverRegister<{Name}Movement> { }
```

**Document-bound Info:**
```csharp
[KandraRegisterEntity(TableName = "Reg_{Name}")]
[RegisterCaption("{Name}")]
public sealed class {Name}Record : DocumentInfoRecord<{Name}Dimensions, {Name}Details> { }

public sealed partial class {Name}Register : DocumentInfoRegister<{Name}Record> { }
```

**Independent/Periodic Info:** identical shape to document-bound, swap `DocumentInfoRecord<...>`/
`DocumentInfoRegister<...>` for `PeriodicInfoRecord<...>`/`PeriodicInfoRegister<...>`.

### Entity base shapes (what you get for free)

```csharp
// Movement<TDimensions,TResources,TDetails> — src/KandraCore/Kandra.Domain/Registers/Movement.cs
public abstract class Movement<TDimensions, TResources, TDetails> : IMovement
{
    public Guid Id { get; set; }             // UUIDv7, engine-assigned
    public DateTime Period { get; set; }     // stamped from the document's Date
    public Guid DocumentId { get; set; }     // recorder
    public DocumentBase Document { get; set; }
    public Guid LineId { get; set; }         // set by YOUR posting code from the document line — see §5
    public bool Active { get; set; } = true; // false = excluded from every reader, survives in the log
    public bool IsExpense { get; set; }      // false = receipt, true = expense — direction, not sign
    public TDimensions Dimensions { get; set; }
    public TResources Resources { get; set; } // every member must be >= 0 — the engine enforces this at Write
    public TDetails Details { get; set; }
}

// Balance<TDimensions,TResources> — Balance.cs
public abstract class Balance<TDimensions, TResources> : IBalance
{
    public Guid Id { get; set; }             // surrogate PK — EF can't key on owned-type (Dimensions) properties
    public TDimensions Dimensions { get; set; }
    public TResources Resources { get; set; } // current balance
    public int Version { get; set; }         // optimistic-concurrency counter, engine-managed — a manually-
                                              // incremented column with bounded (3-attempt) retry on conflict,
                                              // not a per-provider atomic upsert; deliberate scope-down, see
                                              // techdebt/TECH_DEBT.md TD-004 if you're tempted to "fix" it
}

// DocumentInfoRecord<TDimensions,TDetails> — DocumentInfoRecord.cs (extends PeriodicInfoRecord)
public abstract class DocumentInfoRecord<TDimensions, TDetails> : PeriodicInfoRecord<TDimensions, TDetails>, IDocumentInfoRecord
{
    public DateTime Period { get; set; }     // inherited from PeriodicInfoRecord
    public Guid DocumentId { get; set; }     // recorder
    public DocumentBase Document { get; set; }
    public Guid LineId { get; set; }         // set by your posting code
    public bool Active { get; set; } = true;
}

// PeriodicInfoRecord<TDimensions,TDetails> — PeriodicInfoRecord.cs (independent mode — no DocumentId/LineId/Active)
public abstract class PeriodicInfoRecord<TDimensions, TDetails> : InfoRecord<TDimensions, TDetails>, IPeriodicInfoRecord
{
    public DateTime Period { get; set; }
}
```

Every one of these members is stamped or managed by the engine — you never assign `Id`/`Period`/`DocumentId`/
`Version`/`Active` yourself in posting code. The one field *you* do assign is `LineId` (Movement/
DocumentInfoRecord only) — the engine doesn't know how your movements correspond to document lines, so your
posting code (§5) sets it from the source line's own `Id`. Two rows may legitimately share a `LineId` when they
differ by `IsExpense` (a transfer's two legs) or when one line fans out into several rows (e.g. FIFO splitting
one issue line across several batches) — the record-set identity the engine relies on is `DocumentId` alone,
not `(DocumentId, LineId)`.

## 5. Writing to the register — `ISubmitScope`, and why it's keyed DI

`src/KandraCore/Kandra.Application.Abstractions/Services/ISubmitScope.cs`:

```csharp
public interface ISubmitScope
{
    TWriter GetWriter<TWriter>() where TWriter : class, IRegisterWriter;
    TReader GetReader<TReader>() where TReader : class, IRegisterReader;
    IPostingService GetPostingService(); // chart-of-accounts posting, unrelated to registers — see post-to-accounting
}
```

`IRegisterWriter`/`IRegisterReader` (`Kandra.Persistence.Abstractions.Registers`) are empty root marker
interfaces — every real writer/reader interface (`IBalanceRegisterWriter<,>`, `IBalanceReader<,>`, ...) extends
one of them purely so these two generic methods can constrain `TWriter`/`TReader` to "an actual register
writer/reader," not any `class`.

This is the **only** legal way to obtain a Balance/Turnover/document-bound-Info writer anywhere in the
codebase. The implementation (`Kandra.Application.Services.SubmitScope`) resolves writers via
`IServiceProvider.GetRequiredKeyedService<TWriter>(SubmitScopeKeys.ScopeOnly)` — a keyed DI registration whose
key is an internal constant, `InternalsVisibleTo`-granted only to `Kandra.Persistence`/`Kandra.Application`/
`FamilyGarage.Tests`. A keyed service **cannot be resolved by plain constructor injection** — there is no
`IBalanceRegisterWriter<InventoryMovement, InventoryBalance>` you could ask for in an ordinary service
constructor and get; the DI container simply has no unkeyed registration for it. `ISubmitScope` itself is
handed to you as a parameter **only** on a document Behavior's `OnSubmitAsync`/`OnUnsubmitAsync`/`OnDeleteAsync`
(and to opt-in register-writing data processors) — see `create-document`'s reference.md §6 for the hook
signatures. This isn't a naming convention the engine hopes you follow; it's a compile-time/DI-time guarantee
that a register can't be written from a report, a dictionary behavior, a random service, or a stray
`OnSaveAsync` — only from inside the posting pipeline.

`GetReader<TReader>()` is pure convenience — register readers are **not** keyed (§7), so this just calls the
ordinary `GetRequiredService<TReader>()` without a separate constructor injection. Use it when posting code
needs to read the register it's about to write to (e.g. `TransferBehavior` reads its own prior movements via
`registers.GetReader<ITurnoverReader<InventoryMovement>>()` before recomputing them, §8).

The independent/periodic Info tier is the deliberate exception: `IInfoRegisterWriter<TRecord,TDimensions,
TDetails>` is registered as a **plain** `AddScoped` (§6's DI table), constructor-injectable from anywhere — an
import job, an admin UI service, a dictionary behavior. That tier models reference-style state, not a posting
consequence, so the document-only restriction doesn't apply to it.

### Writer contracts

```csharp
// ITurnoverRegisterWriter<TMovement> — the base accumulation writer
public interface ITurnoverRegisterWriter<TMovement> : IRegisterWriter
{
    Task PostAsync(Guid documentId, DateTime period, IReadOnlyCollection<TMovement> movements, CancellationToken ct);
    Task UnpostAsync(Guid documentId, CancellationToken ct);
    Task DeleteAsync(Guid documentId, CancellationToken ct);
}

// IBalanceRegisterWriter<TMovement,TBalance> : ITurnoverRegisterWriter<TMovement> — same three members;
// PostAsync additionally maintains the balance table (delta upsert + EnsureNonNegative guard) under the hood.
public interface IBalanceRegisterWriter<TMovement, TBalance> : ITurnoverRegisterWriter<TMovement> { }

// IDocumentInfoRegisterWriter<TRecord> — same three-member shape, no balance to maintain
public interface IDocumentInfoRegisterWriter<TRecord> : IRegisterWriter
{
    Task PostAsync(Guid documentId, DateTime period, IReadOnlyCollection<TRecord> records, CancellationToken ct);
    Task UnpostAsync(Guid documentId, CancellationToken ct);
    Task DeleteAsync(Guid documentId, CancellationToken ct);
}

// IInfoRegisterWriter<TRecord,TDimensions,TDetails> — independent mode only, NOT keyed
public interface IInfoRegisterWriter<TRecord, TDimensions, TDetails> : IRegisterWriter
{
    Task SetAsync(TDimensions dimensions, DateTime period, TDetails details, CancellationToken ct);
    Task DeleteAsync(TDimensions dimensions, DateTime period, CancellationToken ct);
}
```

`PostAsync` **replaces the whole record set** for `documentId` — build every movement/record the document
should currently have (all lines, both legs of a transfer, ...) and pass them together in one call; don't call
it once per line. `UnpostAsync` deactivates (`Active = false`) rather than deletes by default — rows stay in
the log, readers stop seeing them, a repost (`PostAsync` again) reverses that. `DeleteAsync` unposts if needed
and then physically removes the rows — the register-side half of document deletion.

The typical Behavior shape (see §8 for the full real examples):

```csharp
public async ValueTask OnSubmitAsync({Doc} entity, bool isInsert, ISubmitScope registers, CancellationToken ct)
{
    var movements = entity.Lines.Select(line => new {Name}Movement
    {
        LineId = line.Id,
        IsExpense = /* true for the expense leg, false for receipt */,
        Dimensions = new {Name}Dimensions { /* ... */ },
        Resources = new {Name}Resources { /* ... */ },   // Balance/Turnover only
        Details = new {Name}Details { /* ... */ },       // if not NoDetails
    }).ToList();

    var writer = registers.GetWriter<IBalanceRegisterWriter<{Name}Movement, {Name}Balance>>();
    await writer.PostAsync(entity.Id, entity.Date, movements, ct);
}

public async ValueTask OnUnsubmitAsync({Doc} entity, ISubmitScope registers, CancellationToken ct) =>
    await registers.GetWriter<IBalanceRegisterWriter<{Name}Movement, {Name}Balance>>().UnpostAsync(entity.Id, ct);

public ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken ct) =>
    new(registers.GetWriter<IBalanceRegisterWriter<{Name}Movement, {Name}Balance>>().DeleteAsync(id, ct));
```

This code lives in the **document's own Behavior file** (`{Config}.Application/Documents/{Doc}.cs`), not a new
file for the register — see `create-document` for everything else about that class. Also add
`[RegisterTransactions(typeof({Name}Movement))]` on the Behavior class itself (§9) — repeatable if one document
touches several registers (e.g. `GoodsReceiptBehavior` also carries `[AccountTransactions]` for the unrelated
chart-of-accounts subsystem).

## 6. DI wiring — mostly generated, Turnover is the real exception

`Kandra.Generators.Di.Persistence` discovers every `[KandraRegisterEntity]`-attributed class by its **direct**
base type (`Balance<,>`/`Movement<,,>`/`DocumentInfoRecord<,>`/`PeriodicInfoRecord<,>`) and, after building,
`AddGenerated{Config}PersistenceEntities()` (called by `AddFamilyGaragePersistence()`) contains:

- One `services.AddBalanceRegister<TMovement,TDimensions,TResources,TDetails,TBalance>()` per Movement whose
  Dimensions+Resources match some discovered Balance — registers the reader, the **keyed** writer, and
  `IRegisterMaintenance<,>`.
- One `services.AddDocumentInfoRegister<TRecord,TDimensions,TDetails>()` per discovered `DocumentInfoRecord`
  subclass — reader + keyed writer.
- One `services.AddPeriodicInfoRegister<TRecord,TDimensions,TDetails>()` per discovered `PeriodicInfoRecord`
  subclass — reader + **plain** writer (not keyed — §5).
- One `IRegisterTransactionsReader<TRow>` registration per Movement/DocumentInfoRecord row (§9), always,
  regardless of `[RegisterCaption]`.

**A Movement type with no matching Balance (i.e. a Turnover register) gets none of this.** The generator only
ever emits `AddBalanceRegister<...>()` by walking discovered *Balance* entities and looking up their matching
Movement — a bare Movement nobody paired with a Balance is silently skipped by that loop, on purpose (there is
no separate "is this Turnover?" case; `EntityKind` doesn't even have a distinct Turnover value). Register it by
hand instead, one line in `{Config}.Persistence/ConfigureServices.cs`'s `AddFamilyGaragePersistence()`:

```csharp
services.AddTurnoverRegister<{Name}Movement, {Name}Dimensions, {Name}Resources, {Name}Details /* or NoDetails */>();
```

This is a real edit to an existing shared file — the one hand-maintained DI line this whole feature needs (see
the "Hand-maintained files" table at the bottom). Re-check this against
`Kandra.Generators.Di.Persistence/PersistenceDiEmitter.cs` before relying on it being permanent — if the
generator ever grows a Turnover case, the manual call becomes redundant and should be removed, not left as a
duplicate registration.

**EF configuration is fully generated, for every family, no exceptions.** `Kandra.Generators.EfConfig` emits a
concrete `{Name}{Row}Configuration : {Row}EntityConfiguration<...>` into `{Config}.Persistence/Generated.Net/`
for every `[KandraRegisterEntity]`-attributed class — table name (from `[KandraRegisterEntity(TableName = ...)]`),
owned-type mapping of Dimensions/Resources/Details, `[Precision]` columns, and FK relationships for every
nav-paired dimension property, all reflected straight off what you declared in step 2/3/4. Never hand-write an
`EntityConfigurations/*.cs` file for a register — there's nothing left to add by hand there.

## 7. Reading from the register

Readers are **plain, ungated, ordinary scoped services** — unlike writers, any class can constructor-inject
one directly, no `ISubmitScope` needed (see `ReportsRepository`'s own constructor for the live pattern: it
injects `ITurnoverReader<InventoryMovement>`, `IBalanceReader<InventoryMovement,InventoryBalance>`,
`IInfoRegisterReader<ExchangeRateRecord>`, and `IInfoRegisterReader<PriceRecord>` side by side).

```csharp
public interface ITurnoverReader<TMovement> : IRegisterReader
{
    Task<List<TMovement>> Movements(DateTime from, DateTime to, Func<IQueryable<TMovement>, IQueryable<TMovement>>? adjust, CancellationToken ct);
    Task<List<TResult>> Movements<TResult>(DateTime from, DateTime to, Func<IQueryable<TMovement>, IQueryable<TResult>> project, CancellationToken ct);
}

public interface IBalanceReader<TMovement, TBalance> : ITurnoverReader<TMovement>
{
    Task<List<TBalance>> Balances(Func<IQueryable<TBalance>, IQueryable<TBalance>>? adjust, CancellationToken ct);
    Task<List<TResult>> Balances<TResult>(Func<IQueryable<TBalance>, IQueryable<TResult>> project, CancellationToken ct);
    // as-of-date: current balance minus the effective sum of movements with Period > at ("current - tail")
    Task<List<TBalance>> Balances(DateTime at,
        Func<IQueryable<TBalance>, IQueryable<TBalance>>? adjustBalances,
        Func<IQueryable<TMovement>, IQueryable<TMovement>>? adjustMovements, CancellationToken ct);
}

public interface IInfoRegisterReader<TRecord> : IRegisterReader // both Periodic and DocumentInfo share this
{
    Task<List<TRecord>> Records(DateTime from, DateTime to, Func<IQueryable<TRecord>, IQueryable<TRecord>>? adjust, CancellationToken ct);
    Task<List<TResult>> Records<TResult>(DateTime from, DateTime to, Func<IQueryable<TRecord>, IQueryable<TResult>> project, CancellationToken ct);
    Task<List<TRecord>> SliceOfLast(DateTime at, Func<IQueryable<TRecord>, IQueryable<TRecord>>? adjust, CancellationToken ct);
}
```

Pass an `adjust: q => ...`/`project: q => ...` transform rather than materializing then filtering client-side —
navigation dereferenced *inside* the projection (e.g. `m.Dimensions.Warehouse.Name`) gets pulled into the SQL
automatically, no `Include()` needed, except on the as-of-date `Balances(at, ...)` overload, which needs a real
`TBalance` shape (`Include()`) because its current-minus-tail math needs the actual navigation objects, not a
projection. Returned rows from every overload are already `AsNoTracking()`'d — terminal reads, not further-
composable `IQueryable`.

**Filtering by a set of dimension keys** (e.g. "give me balances for exactly these (Warehouse, Item) pairs"):
use `DimensionPredicateBuilder` (`Kandra.Persistence.Abstractions.Registers`) rather than hand-rolling
`Expression.Property` reflection or loading everything client-side:

```csharp
var predicate = DimensionPredicateBuilder.BuildOr(b => b.Dimensions, keys); // one OR-of-equality expression
// or, for large key sets (SQL Server's ~2100 parameter cap):
foreach (var batch in DimensionPredicateBuilder.BuildOrBatches(b => b.Dimensions, keys)) { /* one query per batch, concat results */ }
```

Each term comes from the target dimensions type's own `BuildEqualityPredicate()` (§2, generated) rebound onto
your selector — no reflection anywhere in this path. This is the established, only-sanctioned way to turn a
dimension key set into an EF-translatable filter in this codebase; reuse it for any new reader that needs the
same shape rather than inventing another one.

## 8. Worked examples

Full, current, working source — copy the matching skeleton from [`data/`](data/), then adapt field-by-field
against whichever of these is structurally closer to what you're building. Both live in
`FamilyGarage.Domain/Entities/Registers/`.

### Balance: `InventoryRegister`

`Inventory.cs` — `InventoryDimensions` keys on `(WarehouseId, ItemId, PartyId)`. `WarehouseId`/`ItemId` are
ordinary nav-paired dimensions; `PartyId` is a deliberately bare `Guid` (no navigation) — `Guid.Empty` means
"no party, moving-average item" by application convention, and for a real batch it's set to the *creating*
document's own `Id`, so there's no honest dictionary/document row a navigation could always point at (documents
are soft-deletable, a register must stay queryable forever). It carries `[ReferenceOf(TypeIdProperty =
nameof(InventoryDetails.PartyCreatorDocumentTypeId), PossibleTypes = [typeof(GoodsReceipt)])]` — the dynamic
mode (§ attribute vocabulary below): the viewer resolves `PartyId` by reading the sibling
`PartyCreatorDocumentTypeId` field (which itself carries `[RegisterHidden]`, so it's a real stored column but
never shown as its own raw-Guid viewer field) to know which candidate type it's actually pointing at.

`InventoryResources` has two members: `Quantity` (`[Precision(14,3)]`) and `Amount` (`[Precision(14,5)]`).
`EnsureNonNegative()` rejects a movement where either is negative; `EnsureValidBalance()` guards only `Quantity`
on the balance (throws `InsufficientBalanceException`) — `Amount` is left unguarded there (a zero-cost receipt is
legitimate). `InventoryDetails` carries `SupplierId` (`[ReferenceOf(typeof(Counterparty))]` — static mode,
no nav property to pair against), the `PartyCreatorDocumentTypeId` mentioned above, `SerialNumber`,
`ExpirationDate`. `InventoryBalance : Balance<InventoryDimensions, InventoryResources>` and
`InventoryMovement : Movement<InventoryDimensions, InventoryResources, InventoryDetails>` each carry
`[KandraRegisterEntity(TableName = "Reg_Inventory"/"Reg_Inventory_Balance")]`; the Movement additionally carries
`[RegisterCaption("Inventory")]`. `InventoryRegister : BalanceRegister<InventoryMovement, InventoryBalance>`.

Posted from two document Behaviors, both `[RegisterTransactions(typeof(InventoryMovement))]`:
- **`GoodsReceiptBehavior`** — the simple, single-leg case: one `IsExpense = false` movement per line, straight
  from the line's own `Quantity`/`Price`, `PartyId` chosen per the item's resolved costing method. This is the
  one to copy first.
- **`TransferBehavior`** — the complex, two-leg case: reads its own prior expense movements via
  `registers.GetReader<ITurnoverReader<InventoryMovement>>()`, runs them through a FIFO/FEFO allocation service
  (`IInventoryAllocationService` — `docs/kandra-wms-inventory-costing-design.md`, out of scope for this skill),
  then mirrors each resulting expense leg into a receipt leg at the destination warehouse. Read it only once
  the simple case makes sense — its complexity is inventory-costing-specific, not a register-engine requirement
  (the design doc's own two-row-transfer example, design §1.3, is far simpler than what FamilyGarage actually
  needed once costing entered the picture).

Both call `registers.GetWriter<IBalanceRegisterWriter<InventoryMovement, InventoryBalance>>()` and
`.PostAsync(entity.Id, entity.Date, movements, ct)`/`.UnpostAsync(entity.Id, ct)`/`.DeleteAsync(id, ct)`.

### Document-bound Info: `ExchangeRatesRegister`

`ExchangeRates.cs` — the simplest live register in the repo, good as a first copy target for this family.
`ExchangeRateDimensions` keys on `(FromCurrencyId, ToCurrencyId)`, both ordinary nav-paired dimensions.
`ExchangeRateDetails` carries one member, `Rate` (`[Precision(18,6)]`). `ExchangeRateRecord :
DocumentInfoRecord<ExchangeRateDimensions, ExchangeRateDetails>` carries `[KandraRegisterEntity(TableName =
"Reg_ExchangeRates")] [RegisterCaption("ExchangeRates")]`. `ExchangeRatesRegister :
DocumentInfoRegister<ExchangeRateRecord>`.

Posted from `RateImportBehavior` (`[RegisterTransactions(typeof(ExchangeRateRecord))]`):

```csharp
var records = entity.Lines.Select(line => new ExchangeRateRecord
{
    LineId = line.Id,
    Dimensions = new ExchangeRateDimensions { FromCurrencyId = line.CurrencyId, ToCurrencyId = homeCurrency.Id },
    Details = new ExchangeRateDetails { Rate = line.Rate },
}).ToList();

var ratesWriter = registers.GetWriter<IDocumentInfoRegisterWriter<ExchangeRateRecord>>();
await ratesWriter.PostAsync(entity.Id, entity.Date, records, cancellationToken);
```

`OnUnsubmitAsync`/`OnDeleteAsync` are one-liners calling `.UnpostAsync(entity.Id, ct)`/`.DeleteAsync(id, ct)` on
the same writer — no balance to maintain, so there's nothing else to do.

**Two more live examples of the exact same family**, worth a glance if `ExchangeRatesRegister` alone doesn't
answer your shape question: `PricesRegister` (`Prices.cs`, posted by `PriceListBehavior`) — dimensions
`(PriceTypeId, ItemId)`; `CostingMethodSettingsRegister` (`CostingMethodSettings.cs`, posted by
`CostingMethodSettingBehavior`) — dimensions `(WarehouseId?, ItemId?)` **nullable** (an intentional wildcard
convention, safe here specifically because a document-bound Info register has no balance/upsert unique index to
collide on — see the nullable-dimensions caveat in §2).

### Independent/periodic Info - unproven, use with eyes open

Turnover has live consumers (`Sales`, `Purchases`, `VatEvents`). The independent/periodic Info family is real, DI-wired
and documented above (§1/§4/§6), but nothing uses it, so there is no worked example. It has no form: you would have to
write the data's editor yourself. Before committing to it, check whether the shape you need is a document-bound Info
register fed by a "set values" document (§1's guidance).

## 9. The register-transactions viewer — fully generated, two attributes are all you write

Every Document's View page gets a uniform "Register Transactions" tab showing every register that document's
Behavior is declared to touch — with **zero hand-written reader/service/UI code** for any of it:

- `Kandra.Generators.RegisterTransactions.Persistence` emits one `IRegisterTransactionsReader<TRow>` per
  discovered Movement/DocumentInfoRecord row type — always, for every such row, regardless of
  `[RegisterCaption]`. (Balance and plain-`PeriodicInfoRecord` rows are excluded: a Balance row is a
  current-state snapshot with no `DocumentId` of its own to attribute rows to; an independent Info record isn't
  document-bound either.)
- `Kandra.Generators.RegisterTransactions.Application` emits one `IDocumentRegisterTransactionsService<TEntity>`
  per Document type, combining every register its Behavior declares via `[RegisterTransactions(...)]` — a
  Document with zero such attributes still gets a trivial, empty-result implementation, so nothing downstream
  needs a nullable/optional dance.
- The client endpoint (`GET .../registertransactions/{id}` → `DocumentRegisterTransactionsDto`) is already on
  the **shared** `IDocumentApiClient` base every document's Refit interface inherits — no new Refit method, no
  new controller action, for any register.

What you actually write, both already covered above:

| Attribute | Namespace | Target | Meaning |
|---|---|---|---|
| `[KandraRegisterEntity(TableName = "...")]` | `Kandra.Attributes.Entities` | class (Balance/Movement/Record) | Marks the class for `Kandra.Generators.EfConfig`/`.Di.Persistence` discovery — mandatory on every register row/balance entity. |
| `[RegisterCaption(value, isLiteral = false)]` | `Kandra.Attributes.Naming` | class (Movement/DocumentInfoRecord only — not Balance) | Display name for the register in the viewer. Technically optional (falls back to no caption client-side if omitted) — add it anyway, every live register does. `value` is a resx key by default, same convention as `[Caption]` (§10) — don't pass `isLiteral: true` to dodge the resx edit. |
| `[RegisterTransactions(typeof(RowType))]` | `Kandra.Attributes.Application` | class, **repeatable** — the document's Behavior, not the register | Declares one register row type this Behavior's `OnSubmitAsync`/`OnUnsubmitAsync`/`OnDeleteAsync` writes to. Purely declarative — the engine never checks it actually matches your `GetWriter<>()` calls, so keep it in sync by hand when you add/remove a register write. |

Field-level display metadata comes from the same `[Caption]`/`[Format]` attributes you already put on
Dimensions/Resources/Details properties in §2/§3 — nothing extra to add there. Two more attributes only needed
for reference-shaped fields:

| Attribute | Target | Meaning |
|---|---|---|
| `[ReferenceOf(typeof(TargetType))]` | a bare `Guid`/`Guid?` Dimensions/Resources/Details property with no paired navigation | Static mode: resolves the field to `TargetType`'s Code/Name (Dictionary) or Code/Date (Document) in the viewer, exactly like a nav-paired dimension would. `SupplierId` in §8. |
| `[ReferenceOf(TypeIdProperty = nameof(Sibling), PossibleTypes = [typeof(A), typeof(B), ...])]` | same, when the target type varies per row | Dynamic mode: resolves per-row by reading a sibling Guid property (anywhere on the same row — Dimensions/Resources/Details) holding the target's own static `TypeId`. `PartyId`/`PartyCreatorDocumentTypeId` in §8. |
| `[RegisterHidden]` | any Dimensions/Resources/Details property | Excludes the property from the viewer's column list while keeping it a real, stored, queryable column — for exactly the case above: a discriminator field another field's dynamic `[ReferenceOf]` needs but that shouldn't itself render as a raw Guid. |

## 10. Localization

Every `[Caption]`/`[RegisterCaption]` key referenced anywhere in a register's Dimensions/Resources/Details/row
classes must exist in the English `{Config}.Localization` file (`{Config}.resx`; `.uk.resx`/`.ru.resx` optional)
— exactly the same rule as a Dictionary Dto's `[Caption]` keys (`create-dictionary`'s reference.md §8), and for
the same reason: a missing key just renders the raw key name at runtime, no build error, no exception. This is
the one real cross-cutting shared-file edit a new register needs — see the table below.

## 11. EF migration

Same three-provider command as `create-dictionary`'s reference.md/`SKILL.md` — nothing register-specific about
it, since EF configuration is fully generated (§6):

```bash
cd src/FamilyGarage.Persistence.Databases
dotnet ef migrations add Add{Name} --project . --startup-project ../FamilyGarage.WebApi/FamilyGarage.WebApi.csproj --context FamilyGarageDbContextSqlite     --output-dir Migrations/Sqlite
dotnet ef migrations add Add{Name} --project . --startup-project ../FamilyGarage.WebApi/FamilyGarage.WebApi.csproj --context FamilyGarageDbContextSqlServer  --output-dir Migrations/SqlServer
dotnet ef migrations add Add{Name} --project . --startup-project ../FamilyGarage.WebApi/FamilyGarage.WebApi.csproj --context FamilyGarageDbContextPostgreSql --output-dir Migrations/PostgreSql
```

All three providers together, even if only SQLite is exercised locally. Besides the new migration file, this
rewrites each provider's `Migrations/{Provider}/FamilyGarageDbContext{Provider}ModelSnapshot.cs` — expect that
diff, it's `dotnet ef`'s own output, never hand-edit it (same rule as `Designer.cs`).

## Hand-maintained files

A register is a **much shorter** hand-maintained list than a Dictionary/Document, precisely because it has no
Forms/Application plane and no type identity to mint. Everything below is either a genuinely new fact
(localization) or a documented, narrow generator gap (Turnover DI):

| What | File | Why it's manual |
|---|---|---|
| Localization keys | Edit to `{Config}.Localization/*.resx` (English required, uk/ru optional) | Every `[Caption]`/`[RegisterCaption]` key referenced by the register's classes must exist in all three locale files (§10) — no generator populates resx. |
| Turnover DI registration (**Turnover family only**) | Edit to `{Config}.Persistence/ConfigureServices.cs`'s `AddFamilyGaragePersistence()` | `Kandra.Generators.Di.Persistence` only emits `AddBalanceRegister<>()` by walking discovered Balance entities and matching their Movement — a Movement with no paired Balance (Turnover) is never wired automatically (§6). Not needed for Balance/document-bound-Info/independent-Info — those are 100% generator-wired. |
| Register-write code + `[RegisterTransactions(...)]` | Edit to the **posting document's own** `{Config}.Application/Documents/{Doc}.cs` Behavior file | Business logic (which movements/records to build) is inherently hand-written — but this is a `create-document`-scope file you're already touching, not a new shared file the register itself owns. |

Everything a Dictionary/Document needs that a register explicitly **doesn't**: no type-id constant (no
`FamilyGarageTypeIds.{Name}.cs` — registers don't implement `IEntityWithTypeId`), no numbering bucket, no Refit
client interface (the register-transactions endpoint is already on the shared `IDocumentApiClient` base, §9),
no Blazor UI page, no nav entry (a register has no `[Kandra*Form]` Dto, so it was never eligible for any of
those in the first place — this isn't a coincidental gap, it follows directly from "What a Register actually
is" at the top of this document). EF configuration (§6) and the register-transactions reader/service (§9) are
both 100% generated for every family, Turnover included.

## 12. Verify

`dotnet build FamilyGarage.slnx`, then check the generated (or, Turnover only, hand-registered) DI call is present in
`Generated.Net/Kandra.Generators.Di.Persistence/.../Generated{Config}PersistenceDiExtensions.g.cs`, and that
`Generated.Net/Kandra.Generators.EfConfig/.../{Name}{Row}Configuration.g.cs` exists with the table/FK/precision
you expect. `dotnet test FamilyGarage.slnx` — `FamilyGarage.Tests/Registers/` has the closest existing coverage shape to
model new tests on: `{Row}DimensionsPredicateTests` (`DimensionPredicateBuilder` over the new dimensions type),
`InventoryBalancePredicateSqliteTests` (SQLite round-trip proving the predicate actually translates to SQL, not
falling back to client-side eval), `RegisterMaintenanceTests` (`IRegisterMaintenance<,>.RebuildAsync`/
`VerifyAsync` — Balance family only), `{Row}TransactionsReaderSqliteTests` (the generated
`IRegisterTransactionsReader<TRow>`, resolved through the real DI wiring end to end). Then exercise it live:
post a document that writes the register, confirm the balance/record changed via a report or the register's own
reader, check the new register-transactions tab on the document's View page, unsubmit, confirm it reverted.

## If this reference goes stale

This document is a snapshot, not a live view. If the code disagrees with something here (a compile error citing
a signature that doesn't match, a missing member, a generator emitting something different from §6/§9's
description), trust the code — then update this file so the next person doesn't hit the same surprise. The
fastest way to re-verify a specific claim: grep the type/attribute name under `src/KandraCore/Kandra.Domain/
Registers/`, `Kandra.Persistence.Abstractions/Registers/`, `Kandra.Persistence/Registers/`,
`Kandra.Attributes/Entities/` or `Kandra.Attributes/Naming/`/`.Application/`, or
`Kandra.Generators.Shared/RegisterFieldDiscovery.cs`/`EntityDiscovery.cs` (register-model discovery) and
`Kandra.Generators.Di.Persistence/PersistenceDiEmitter.cs` (DI wiring) — every type named above lives in exactly
one of those. `docs/kandra-register-engine-design.md` is the design rationale (note it predates some naming —
it says `Kandra.Core`/`Kandra.Data`, the real code is `Kandra.Domain`/`Kandra.Persistence`, and its
`IRegisterDimensions`/dimension-comparer story is the pre-`BuildEqualityPredicate()` version — trust this
reference and the code over that doc's declaration-layer specifics, its behavioral/conceptual sections still
hold). `docs/1c-register-architecture.md` is background only (the 1C behavioral contract this design reconciles
against) — its Part IV declaration blueprint was explicitly rejected, don't copy shapes from it.
