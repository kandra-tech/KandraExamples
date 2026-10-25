---
name: troubleshoot-configuration
description: Look up a build error, failing test, startup or runtime error, or wrong screen in a Kandra configuration and get its usual cause and fix - Mapperly RMG012/RMG020, KANDRAGEN/KANDRAUI diagnostics, "No service for type IValidator<...QueryDto>", "Cannot provide a value for property 'validator'", "List must not be empty", "already being tracked", permission denied on a report, raw keys on screen, empty document numbers in a report, demo data not applied, pending model changes. Use before debugging from scratch.
---

# Troubleshooting a configuration

Every row below is a failure an agent actually hit while building a configuration from the template. Find the symptom,
apply the fix, and only then start digging (with [`look-up-engine-api`](../look-up-engine-api/SKILL.md)).

## Build errors

| Symptom | Cause | Fix |
|---|---|---|
| `RMG012`/`RMG020` "property X on YLine has no matching property / is not mapped" | A line (or dictionary) entity has a navigation (`Account`, `Currency`) its Dto lacks, and the mapper method has no strategy | Declare the mapper methods with `[MapperRequiredMapping(RequiredMappingStrategy.Target)]` on `ToDto` and `Source` on `ToEntity`, the line pair included, as the `data/` skeletons do. Not `[MapperIgnore*]` per navigation. |
| `KANDRAGEN014` | A dictionary or document has no Mapperly mapper | Add the `{Name}Mapper` stub from the skeleton (`IEntityMapper<{Name}, {Name}Dto>`). |
| `CS0246` for `I{Plural}ApiClient` in generated code | The hand-written Refit interface is missing | Add it in `ClientLib.Common/ApiClients/` from the skeleton; `[PathPrefix]` = the form's `Name`. |
| `KANDRAUI011` warning, grid not shown | An `ICollection<TLine>` without `[TablePart]` and not named `Lines` | Add `[TablePart]`. |
| Other `KANDRAUI0xx` / `KANDRAPRINT0xx` | An attribute names a method or property that does not exist or has the wrong shape | The message names the member; fix the `nameof(...)`. |

## Test failures

| Symptom | Cause | Fix |
|---|---|---|
| "The instance of entity type X cannot be tracked because another instance with the same key value is already being tracked" | Two mutating calls in one scope | One `InScopeAsync` per step (`test-configuration`). |
| `ServiceResolutionTests`: "Unable to resolve service for type IValidator<{Name}QueryDto>" | The `{Name}QueryDtoValidator` is missing | Add `public class {Name}QueryDtoValidator : PagedQueryDtoValidator<{Name}QueryDto>;` next to the behavior (it is in the skeleton). |
| `DemoPackageTests` schema mismatch | The model changed after the snapshot was taken | `UPDATE_IMPORT_SCHEMA=1 dotnet test`. |
| `LocalizationKeysTests` lists keys | A resx entry is missing | Add the keys (`localize-configuration`). The generated-UI part reads the last build: `dotnet build` first. |
| Statement closing balance off by one day's movements | Inclusive bounds | `Balances(at)` includes `at`: opening = `Balances(from.AddTicks(-1))`, period end = `to.Date.AddDays(1).AddTicks(-1)` (`create-register` §8a). |
| Migrations test: pending model changes | A model change without a migration | `dotnet kandra-migrate add <Name>` (or remove and re-add the unreleased one). |

## Startup and runtime

| Symptom | Cause | Fix |
|---|---|---|
| `ArgumentException: List must not be empty` from `ChartOfAccounts` | Kandra Core before 0.5.21 refused an empty chart | Upgrade the core pin; on an older core return one placeholder leaf (`post-to-accounting`). |
| Browser: "Cannot provide a value for property 'validator'" when a form opens | The client container lacks the Forms validators | Keep `services.AddValidatorsFromAssembly(...)` in `Client{Config}ServicesExtensions` (the template has it). |
| Opening a list fails: "No service for type IValidator<{Name}QueryDto>" | Missing query validator | As above; `ServiceResolutionTests` catches it before the browser does. |
| Report answers permission denied / 403 for everybody | `ObjectName` differs from `[KandraReportForm(Name = ...)]` | Make them equal (`create-report` §3). |
| Report column with the document number is empty | Entity overload of `Movements(...)` without `Include` | `q.Include(m => m.Document)`, or project into the row (`create-register` §8a). |
| A button or label shows its raw key (`BUTTON_PRINT`, `ADDEXPENSE`) | Missing resx key (yours, or the engine's) | `LocalizationKeysTests`; for an engine key define it in your resx and report it (`localize-configuration`). |
| Demo start creates users but no data; `POST /api/v1/Setup/demo` returns 409 "license agreement must be accepted" | `Kandra__Setup__Mode=Demo` without `ACCEPT_EULA=Y` | Start with both. |
| Page fails with "An error occurred deserializing the response" (Refit) | The client's `[PathPrefix]` names a route that does not exist; the unknown `/api/...` path falls through to the WASM host's `index.html` (200, HTML) | Compare the prefix with `/swagger`: `/api/v1/dictionaries/`, `/documents/`, `/reports/`, **`/processors/`** (not `dataprocessors`), then the form's `Name`. |
| A new document opens without a number or date | `OnNewAsync` with the dictionary signature `(entity, ct)`: the document hook `(entity, date, sourceDocumentId, ct)` has a default body, so the other method is never called and nothing warns | Use the skeleton's signature (`create-document` step 4). |
| Submitting answers 500 where a rule should refuse the post | The behavior throws `InvalidOperationException` (any exception the engine does not map is a 500) | `throw new InvalidStateException(localizer[key, ...].Value)`: 409 with the message on the form. |
| `InsufficientBalanceException` "... cannot be negative" when posting a turnover or balance movement | A negative resource on a movement | Post the positive amount with `IsExpense = true`; never empty `EnsureNonNegative()` (`create-register` §8a). |
| A post fails with "... category not found" on a fresh installation (works in the demo) | Code looks a dictionary row up by name and the row exists only in the demo package | A document field, a constant or an enum instead (`create-document` step 4). |
| Startup: "The SQLite database was created by a different migration history" | The migrations were squashed or regenerated (one migration for the whole model) after the dev database was created | Delete the SQLite file, or start once with `Kandra__Database__IncompatibleHistory=Reset` (the old file is saved to `backups/`). |
| A report counts un-posted or deleted documents | It reads `db.Reg_X` (or a document table) without `Active` / `!IsDeleted` | Read through the register's reader (`create-register` §8a). |
| `PendingModelChangesWarning` at startup | Migration missing or stale | As in the test table. |
| Old UI after a client change (404s, SRI errors) | The host serves a stale static-assets manifest | Stop the host, build the WebApi project (not just the client), start again; hard-reload the browser. |
| HTTP client gets 401 after a redirect | `http://...:5220` redirects to https and drops `Authorization` | Call `https://localhost:7220` directly. |

## When the row does not fit

Read the exception and its whole stack trace (the inner exception is usually the real one), find the type it names with
`look-up-engine-api`, and fix the cause in your code. Do not patch generated files, and do not add placeholder data to make an
error disappear without knowing why it appeared.
