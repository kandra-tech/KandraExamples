---
name: post-to-accounting
description: Wire a document's OnSubmitAsync to post debit/credit ledger transactions to the chart-of-accounts/posting subsystem (Acc_ tables) — account pairs with subconto (analytical dimension) values. Use when a document needs to move value through the accounting ledger (not just registers like inventory/exchange rates), when declaring a new account or a new subconto-postable entity, or when adding the "Account Transactions" posted-ledger view tab to a document.
---

# Posting a document to the chart-of-accounts subsystem

> **Not posting to accounting? Leave the chart empty.** A configuration that only uses registers keeps the scaffold's
> empty `ChartOfAccountsBuilder` (`new ChartOfAccounts()`).

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

The chart-of-accounts/posting subsystem is a **second, deliberately independent** accounting engine — not
built on the Balance/Movement register engine `create-register`/`create-document` cover (see
[`src/PersonalAccounting/CLAUDE.md`](../../CLAUDE.md) "Chart-of-accounts / posting subsystem" — don't try to unify the
two). A document commonly writes to **both**: e.g. `GoodsReceiptBehavior.OnSubmitAsync` writes an
`InventoryMovement` (register) *and* a payables→inventory ledger posting (accounting) in the same method, via
two different accessors on the same `ISubmitScope registers` parameter. This recipe covers only the accounting
half; see [`create-document`](../create-document/SKILL.md) for the document itself and
[`create-register`](../create-register/SKILL.md) if it also needs register writes.

Full design doc: `docs/chart-of-accounts-posting-subsystem.md`. Session history if you want the "why" behind a
design choice below: `HANDOFF_055.md` (subsystem built), `HANDOFF_061.md` (universal `AccountTransaction`
document + UI selectors), `HANDOFF_062.md` (the `EntityRef`/builder API this recipe teaches — read this one
first if anything below looks unfamiliar next to older code you find in the repo).

## 1. The chart is a compile-time, in-memory tree — not a persisted entity

There is no `Account` table. `IChartOfAccountsBuilder.BuildChart()` (one implementation per configuration,
`{Config}.Application/Accounting/{Config}ChartOfAccountsBuilder.cs`) returns a `ChartOfAccounts` built from
`AccountLeaf`/`AccountLeaf<T1>`/.../`AccountLeaf<T1,T2,T3,T4,T5>`/`AccountLeafGeneral` nodes (up to 5 subconto
slots per account, `AccountLeafBase.MaxSubconto`). Real example, `WmsChartOfAccountsBuilder.BuildChart()`
(`PersonalAccounting.Application/Accounting/WmsChartOfAccountsBuilder.cs`):

```csharp
public ChartOfAccounts BuildChart() => new(
    new AccountLeaf<Item, Warehouse>(28, "Account_28", AccountType.Asset),           // 2 typed slots
    new AccountLeaf(30, "Account_30", AccountType.Asset),                             // 0 slots (e.g. Cash)
    new AccountLeaf<Counterparty, GoodsReceipt>(63, "Account_63", AccountType.Liability), // slot 2 is a Document type, not a Dictionary
    // A composite ("OneOf") slot - several distinct CLR types allowed at the same position:
    new AccountLeafGeneral(68, "Account_68", AccountType.Liability,
        [Subconto.Of<Counterparty>(), Subconto.OneOf<Waybill, GoodsReceipt>()],
        normalBalance: NormalBalance.Debit | NormalBalance.Credit, balanceSplitSlots: [0]),
    new AccountLeaf<Item>(70, "Account_70", AccountType.Income)
);
```

Account "names" are localization resource keys (`Account_{number}`) resolved via
`AccountNodeLocalizationExtensions.GetDisplayName` — add the matching resx entries (English required, uk/ru optional), same convention
as every other captioned thing in this repo. To add a new account: add one more `AccountLeaf...` line here — no
migration, no DI registration, nothing else to touch (the chart is a plain object graph built fresh at startup).

Every leaf states its `AccountType` (`Asset`/`Liability`/`Equity`/`Income`/`Expense`); its `NormalBalance`
(the side that is its positive balance) is derived from that — Asset/Expense → `Debit`, the rest → `Credit`.
For an active-passive account that can carry either side on the same subconto (68 above), pass
`normalBalance: NormalBalance.Debit | NormalBalance.Credit` as an override; accounting reports read
`NormalBalance`, never `AccountType`, for sign logic. Reports split such an account's balance into debit and
credit per subconto group instead of netting it - by every slot unless `balanceSplitSlots` says otherwise (68
passes `[0]`: split by Counterparty only, so an advance received under one document and cleared under another
nets to zero rather than showing on both sides). `IAccountQueryService.GetExpandedBalanceAsOfAsync` reads it.

## 2. Make an entity postable: implement `ISubconto`

Any type you want usable as a subconto (analytical-dimension) value — the "who/what" a ledger line is about —
must implement `Kandra.Domain.Commons.ISubconto` (`: IRootEntity, IEntityWithTypeId`), i.e. have a `Guid Id`
and a `static Guid TypeId`. Both dictionaries and documents can be `ISubconto` — `Counterparty`/`Item`/
`Warehouse` (dictionaries) and `Waybill`/`GoodsReceipt` (documents) all are in this repo today (grep `,
ISubconto` to see the current, narrow set — it's easy to trust a stale summary here, always grep fresh).
**Only implement it if the type genuinely needs to be referenced as a posting dimension** — a folder/grouping
type like `CounterpartyFolder` doesn't implement it — it only inherits the dictionary's `TypeId` from its Node
(a hierarchical leaf like `Counterparty` inherits its `TypeId` the same way; only the leaf adds `ISubconto`).

```csharp
public sealed partial class GoodsReceipt : DocumentBase, ISubconto, /* ... */
{
    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.GoodsReceipt);
    // Id comes from DocumentBase/DictionaryBase already - nothing else to add for ISubconto itself.
}
```

## 3. Give the document the free posted-ledger view tab

```csharp
[KandraDocumentBehavior(typeof(GoodsReceiptDto))]
[RegisterTransactions(typeof(InventoryMovement))]  // if it also writes registers - see create-register
[AccountTransactions]                              // <- this one
public class GoodsReceiptBehavior(/* ... */) : IDocumentBehavior<GoodsReceipt>
```

`[AccountTransactions]` (`Kandra.Attributes.Application`) is a plain marker, discovered by
`Kandra.Generators.Shared.BehaviorDiscovery` and consumed by `Kandra.Generators.Ui.Blazor.DocumentUiEmitter` to
add a 5th "Account Transactions" tab to the generated View page (`BlazorUiRenderer` opens
`AccountTransactionsView`, backed by `IAccountTransactionsService` — one universal instance covers every
document type, since a posted-ledger row's shape doesn't vary by document). Zero extra registration needed —
same "just add the attribute" posture as `[RegisterTransactions]`.

## 4. Post: `IPostingService` via `ISubmitScope`, inside `OnSubmitAsync`

`IPostingService` is a **keyed DI service**, resolvable only via `ISubmitScope.GetPostingService()` — never by
plain constructor injection (same gating as register writers; see
[`src/KandraCore/CLAUDE.md`](../../../../KandraCore/CLAUDE.md) "Register engine internals"). Real, current example —
`GoodsReceiptBehavior.OnSubmitAsync` (`PersonalAccounting.Application/Documents/GoodsReceipt.cs`):

```csharp
public async ValueTask OnSubmitAsync(GoodsReceipt entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
{
    // ... register-write code omitted, see create-register ...

    var postingService = registers.GetPostingService();

    var transactions = entity.Lines.Select(line => new PostingTransaction(
        LineId: line.Id,
        Source: postingService.AsOf(63).With<Counterparty, GoodsReceipt>(entity.CounterpartyId, entity.Id).Build(),
        Destination: postingService.AsOf(28).With<Item, Warehouse>(line.ItemId, entity.ToWarehouseId).Build(),
        Amount: line.Quantity * line.Price)).ToList();

    await postingService.PostAsync(entity.Id, transactions, entity.Date, ct: cancellationToken);
}

public async ValueTask OnUnsubmitAsync(GoodsReceipt entity, ISubmitScope registers, CancellationToken cancellationToken) =>
    // ... plus register UnpostAsync if this document also writes registers ...
    await registers.GetPostingService().UnpostAsync(entity.Id, cancellationToken);

public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken) =>
    await registers.GetPostingService().UnpostAsync(id, cancellationToken);
```

`PostAsync`/`UnpostAsync` never call `SaveChangesAsync` themselves — same convention as register writers, the
document behavior's own save pipeline does that. `PostAsync` is **upsert**: re-posting an already-posted
document (edit + re-submit) merges line-by-line (added/changed/removed) rather than reverse-then-reinsert; the
caller never needs to know whether this is the first post or a re-post.

### Identity of a posting: `LineId` + `Leg`

A posting is identified within its document by the pair `(LineId, Leg)`, which must be unique in one `PostAsync` call:

- `LineId` is the id of the document line the posting comes from, or `null` for a posting of the document as a whole
  (an advance, a fee, a lineless document). Never invent a Guid for it (no hashing, no `Guid.NewGuid()`: a new id on
  every post turns each re-post into a full deprecate-and-reinsert).
- `Leg` names the posting when one line (or the document) posts more than once: leave the default `""` on the main
  posting and give each further one a fixed name, kept as a constant in one static class of your Application project
  (e.g. `Accounting/PostingLegs.cs` with `public const string Vat = "Vat";`, `Cogs`, ...). Use a name, never a running
  number: a leg that is only sometimes there (VAT for a non-payer, an advance) must not shift the identity of the others.
  At most 32 characters, stored in the ledger, so never rename a value once documents have posted it.

```csharp
transactions.Add(new PostingTransaction(
    LineId: line.Id,
    Source: postingService.AsOf(70).With<Item>(line.ItemId).Build(),
    Destination: receivable,
    Amount: net));

if (vat > 0)
{
    transactions.Add(new PostingTransaction(
        LineId: line.Id,
        Source: postingService.AsOf(64, 1).With<Counterparty, Waybill>(entity.CounterpartyId, entity.Id).Build(),
        Destination: receivable,
        Amount: vat,
        Leg: PostingLegs.Vat));
}
```

## 5. The `AsOf(...).With(...).Build()` builder — cheat sheet

`AsOf` lives on both `IChartOfAccountsService` and `IPostingService` (the latter just proxies to the former, so
a document behavior needs only `IPostingService` — don't also inject `IChartOfAccountsService` for this).
Three overloads: `AsOf(AccountCode)`, `AsOf(params int[])` (e.g. `AsOf(63)`, `AsOf(28, 1)` for a real
multi-segment code like "28.1"), `AsOf(string)` (parses a dotted code string — what you'd use for a
user-picked account, e.g. `AsOf(entity.SourceAccountCode)`).

`With(...)` populates the subconto values for *all* of that account's declared slots in one call (arity =
declared slot count, matching `AccountLeaf<T1,...>`'s own arity):

| Overload | When | Notes |
|---|---|---|
| `With<T1..T5>(Guid id1, ...)` | You know the CLR types at compile time (the common case) | Zero reflection — TypeId comes from `T.TypeId` |
| `With<T1..T5>(T1 v1, ...)` | You already have loaded instances | Thin `.Id`-extracting wrapper over the Guid overload — no separate logic path |
| `With(params Guid?[] ids)` | Types aren't known at compile time (a user picked the account at runtime) | Infers each slot's TypeId from the account's own single-member declaration; **throws if a slot with a real value is composite** (`OneOf`, 2+ member types) — a bare Guid can't say which one |
| `With(params SubcontoSlot?[] slots)` | Same as above, but you *do* know which member type applies for a composite slot | Pass `new SubcontoSlot(typeId, id)` explicitly; validated against the slot's declared `Accepts(Guid)` |
| `With(ISubcontoSet s)` | You already have a pre-built set to pass through | Rare |

Both untyped overloads (`Guid?[]`/`SubcontoSlot?[]`) **must cover every declared slot** — passing fewer values
than the account declares throws (`EnsureCoversDeclaredSlots`), so a bug like "forgot slot 2" fails loudly, not
silently. Passing **more** than declared is fine and the extras are simply ignored — this is what lets a
document with a fixed number of flat subconto fields (see `add-account-subconto-field` skill) call `With(...)`
identically regardless of which account the user actually picked.

A slot value can be `null` (no subconto in that position) even for a required-count call — only a genuinely
composite slot with an actual non-null value is rejected by the untyped path.

## 6. Validation `PostAsync` enforces (read this before writing a test that expects something else)

`PostingService.Validate` runs entirely before touching the database — throws before the repository/balance
updater are ever called:

- Batch non-empty; every `(LineId, Leg)` pair unique within the batch; `Leg` at most 32 characters.
- `Amount > 0`.
- `CurrencyId`/`CurrencyAmount` both set or both null.
- `Source.Account`/`Destination.Account` must resolve to a real leaf (not a folder, not unknown).
- Each side's subconto count must **exactly** match the account's declared slot count, and each non-null
  slot's TypeId must be one of that slot's declared member types (positional, not a set-match).
- **Same account posted to itself is rejected only if the subconto is also identical** — an internal transfer
  within one account (e.g. moving inventory between two warehouses, both posting to account 28 with a
  different `Warehouse` subconto) is explicitly allowed.

## 7. Testing pattern

Model new accounting tests on `PersonalAccounting.Tests/Accounting/`:
- `PostingServiceValidationTests.cs` — proves `Validate()` throws for each rule above *before* touching
  persistence, using throwing `IPostingRepository`/`IAccountBalanceUpdater` stubs (a test that reaches them
  fails loudly for the wrong reason instead of silently passing).
- `PostingServiceSqliteTests.cs` — real SQLite round-trip (insert, re-post with a changed line, deprecate,
  balance turnover) — this is the one that actually exercises the EF `ComplexProperty`-mapped `SubcontoSlot`
  columns, not just that the code compiles.
- `ChartOfAccountsServiceTests.cs` — chart declaration/lookup invariants (`GetLeaf`, `GetSubcontoInfo`,
  `FindAccountsBySubcontoType`, the `AsOf(...).With(...)` builder itself, composite-slot rejection).

## 8. Gotchas — read before "fixing" something that isn't broken

- **Don't build a shared Type↔TypeId registry for this.** `ChartOfAccountsService` deliberately keeps its own
  flat `SubcontoInfo` dictionaries, derived purely from the chart itself — not merged with the client-visible
  `Kandra.ClientLib.Common.TypeRegistry.ITypeRegistry` (a different concern — UI polymorphic-selector dispatch) or any
  new registry. If you need "what CLR type/kind is this subconto TypeId," call
  `IChartOfAccountsService.GetSubcontoInfo(Guid)` (returns `SubcontoInfo?` — `TypeId`, `Type`, `Kind`, and,
  kind-dependent, `EnumType`/`RootType`) — don't reflect it yourself.
- **`GetSubcontoInfo` returns `null`, it doesn't throw**, for a type/TypeId not declared anywhere in the chart.
  Handle the null case (skip, "no results", whatever fits) rather than assuming it's always found.
- **No EF migration is needed just to change posting call sites.** `SubcontoSlot(Guid TypeId, Guid EntityId)`
  is the one physical shape (`Acc_PostingTransactions`/`Acc_AccountBalances`'s `src{1-5}_*`/`dst{1-5}_*`/
  `slot{1-5}_*` complex-mapped columns) — it hasn't changed since the subsystem was built. You only need a
  migration if you add a genuinely new persisted entity (a new dictionary/document/register).
- **A composite (`OneOf`) slot can't be posted through a user-facing "raw Guid" UI field today** — see
  `add-account-subconto-field` skill §6 for the exact gap and where the fix would go if you're the one picking
  it up.
- Don't resolve `IPostingService`/`IChartOfAccountsService.AsOf(...)` outside `ISubmitScope` — there is no
  other legal path to `IPostingService`, it will fail DI resolution.

## 9. Verify

`dotnet build PersonalAccounting.slnx`, `dotnet test src/PersonalAccounting.Tests --filter "FullyQualifiedName~Accounting"`,
then live in the browser: submit the document, open its View page's **Account Transactions** tab, confirm the
posted row shows the expected accounts/amount/subconto captions; unsubmit and confirm the row goes inactive
(stays visible, `Active = false`) rather than disappearing.
