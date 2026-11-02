---
name: test-configuration
description: Write tests that prove a configuration works end to end - document posting and register effects, edit/unsubmit/delete, reports compared with hand-computed numbers, acceptance checks of a spec - using the shared PersonalAccountingTestHost (real services on SQLite in memory, no web host). Use when adding a document, a register, a report, or when a spec lists acceptance checks.
---

# Testing a configuration

`src/PersonalAccounting.Tests/Support/PersonalAccountingTestHost.cs` builds the configuration's real services on a
SQLite in-memory database with the checked-in **migrations applied**: the engine's CRUD services, behaviors, mappers,
validators, register writers/readers and report behaviors, exactly as the running app composes them, minus HTTP and
authentication (every caller is allowed and runs as one test user, `TestUserId`, which is who `NotifyCallerAsync`
notifies; time zone UTC unless you pass another one). That is the right level for nearly every test of a
configuration: it proves the posting rules, the register effects and the report numbers, which is what breaks.
**Do not write your own provider/DI plumbing** (forty lines of `ServiceCollection` per test class was the first thing
the first trial wrote); use the host. Add the migrations first (`plan-configuration` stage 7): the host applies them.

## The shape of a test

Copy [`data/PostingTest.cs`](data/PostingTest.cs) and rename. **Run every step in its own scope** with
`_host.InScopeAsync(sp => ...)`: a scope is one unit of work with its own DbContext, and updating a document in the scope
that inserted or read it fails with "another instance with the same key value is already being tracked" (verified). In
short:

```csharp
public sealed class ExpenseTests : IDisposable
{
    private readonly PersonalAccountingTestHost _host = new();
    public void Dispose() => _host.Dispose();

    private static IDocumentCrudService<ExpenseDto, ExpenseQueryDto> Expenses(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<ExpenseDto, ExpenseQueryDto>>();

    [Fact]
    public async Task Submitting_an_expense_reduces_the_account_balance()
    {
        var ct = CancellationToken.None;
        var account = await _host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<AccountDto, AccountQueryDto>>()
            .InsertAsync(new AccountDto { Code = "CARD", Name = "Card", /* ... */ }, ct));
        var expense = await _host.InScopeAsync(sp => Expenses(sp)
            .InsertAsync(new ExpenseDto { Code = "E1", Date = Day(2026, 10, 5), AccountId = account.Id, /* ... */ Amount = 1500m }, ct));

        await _host.InScopeAsync(sp => Expenses(sp).SubmitAsync(expense.Id, ct));

        var balance = await _host.InScopeAsync(async sp => (await sp
            .GetRequiredService<IBalanceReader<AccountBalanceMovement, AccountBalance>>()
            .Balances(q => q.Where(b => b.Dimensions.AccountId == account.Id), ct)).Sum(b => b.Resources.Amount));
        Assert.Equal(-1500m, balance);
    }
}
```

The services you resolve (all generated from your Dtos, nothing to register):

| Need | Service |
|---|---|
| Create/read/update/delete a dictionary | `IDictionaryCrudService<TDto, TQueryDto>`: `NewAsync`, `InsertAsync` (returns `CreatedResultDto` with `.Id`), `UpdateAsync(dto)`, `GetByIdAAsync(id)` (sic), `QueryAsync`, `DeleteAsync` |
| A **hierarchical** dictionary | `IHierarchicalDictionaryCrudService<TRootDto, TQueryDto>` (the abstract root Dto, e.g. `CategoryNodeDto`), not `IDictionaryCrudService`; insert a folder or a leaf Dto through it |
| Documents | `IDocumentCrudService<TDto, TQueryDto>`: the same plus `SubmitAsync(id)`, `UnsubmitAsync(id)`, `GetRegisterTransactionsAsync(id)` |
| Run a report | `IReportService<TFilters, TResult>`: `NewAsync`, `RunAsync(filters)` |
| Registers | `IBalanceReader<TMovement, TBalance>` (`Balances(q => ...)`, `Balances(asOf, ...)`), `ITurnoverReader<TMovement>` (`Movements(from, to, q => ...)`), `IInfoRegisterReader<TRecord>` (`SliceOfLast(at, ...)`) |
| Constants | `IConstantsManageService.SetAsync<T>(name, value, from, ct)`, `IConstantsService.GetAsync<T>(asOf, ct)` |

`new PersonalAccountingTestHost(services => ...)` lets a test replace or add a registration (a fake clock, a stub).

## What to test (and what not)

Per document type, one test for **each of**:

1. **Submit** writes the expected register rows (dimensions and amounts, direction).
2. **Edit while posted**: change the amount, `UpdateAsync` the posted document, assert the register shows the new numbers
   and no leftovers from the old ones (the engine re-posts a posted document on save).
3. **Unsubmit** returns the register to its previous state; saving a posted document back as a draft does the same.
4. **Delete** is refused while posted (`InvalidStateException`) and works after unsubmit.
5. Any **validation** rule of the behavior or validator (amount must be positive, a missing rate refuses the post).

Per report, one test that posts a few documents on known dates, runs the report and compares the **result rows with
numbers computed by hand in the test** (never "not empty"). Every acceptance check in the spec is one of these tests,
named after the check.

Do not test generated code (controllers, mappers' trivial members, EF configuration) and do not assert on UI.

## Rules about days and months: test east of UTC

In UTC a local date and a UTC date are the same day, so a test host in UTC cannot see a time-zone bug: a dashboard tile
that reads the month of `StartUtc` (the last day of the previous month for a user east of UTC) disagrees with the report,
and a test of "the dashboard equals the report" in UTC still passes. For every rule that turns an instant into a calendar day or month (a monthly budget or limit, "this month" on a dashboard,
a report's month or day filter, a due date in days), add one test with the host in `EastOfUtc`, the dates the client
would send, and an assertion on the **calendar** month, not only that two figures agree (two readers that shift alike
agree with each other):

```csharp
private readonly PersonalAccountingTestHost _host = new(timeZone: PersonalAccountingTestHost.EastOfUtc);

// 1 November picked in Kyiv arrives as 31 October 22:00 UTC.
var november = PersonalAccountingTestHost.LocalMidnight(PersonalAccountingTestHost.EastOfUtc, 2026, 11, 1);
// ... post a budget and an expense dated `november`, run the report for a day in November and for one in October:
// both belong to November, nothing to October.
```

The host sets the zone that `ICurrentTimeZone` reports, which is what behaviors, reports and dashboards read. Import
package date tokens follow the caller's own zone instead; test those through the running app.

## Gotchas

- One scope per step (above): **never two mutating calls (insert, submit, update, unsubmit, delete) in the same
  `InScopeAsync`**, and read registers in a scope of their own. The exact reader types and their date bounds are in
  `create-register` section 8a; the same bounds apply to what a test asserts. Reading the register in its own scope also guarantees you see what was committed, not
  entities still sitting in a change tracker.
- Inserting a document with `IsActive = true` posts it immediately; the skeleton inserts a draft and calls `SubmitAsync`,
  which tests the same path the Submit button takes.
- Dates are UTC: build them with `new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc)`. A calendar-day field that is
  `DateOnly` takes `DateOnly`. For a rule about days or months, add a test in another time zone (next section).
- Numbering and the document `Date` are set by the behavior's `OnNewAsync` when you call `NewAsync`; a test that skips
  `NewAsync` must set `Code` and `Date` itself.
- A behavior that needs a constant (base currency) reads it as of the document's date: set the constant with a `from`
  date on or before the earliest document of the test.
- `UPDATE_IMPORT_SCHEMA=1 dotnet test` rewrites the import schema snapshot when the model changed; the demo package test
  fails when it is stale.
- Tests that need a real HTTP pipeline (authorization, the generated controllers, JWT) are the exception; they need a
  `WebApplicationFactory` and are not worth it for posting logic.
- Run only yours while iterating: `dotnet test src/PersonalAccounting.Tests --filter FullyQualifiedName~ExpenseTests`.
