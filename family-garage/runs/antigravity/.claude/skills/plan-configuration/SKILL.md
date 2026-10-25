---
name: plan-configuration
description: Turn a business specification ("build me an app that tracks X") into a Kandra configuration - map the spec's concepts onto Kandra's entity kinds, fix the build order, and verify every stage. Use FIRST, before creating any entity, whenever the task is a whole application or module rather than one entity. Points at the create-* skills for each step.
---

# Planning and building a configuration from a specification

This is the front door. The other skills each build one kind of entity; this one decides **which** entities to
build, **in what order**, and **how to know each stage works**. Read it completely once, then follow the stages.
The skills in this folder plus `AGENTS.md` cover how to build; the documentation site (`docs.kandra.tech`) has the
conceptual guides behind them. You do not need the engine source.
Companion file: [`reference.md`](reference.md) - modeling patterns for the shapes most business specs contain
(balances, rates, plan-vs-fact, dashboards, statements, multi-currency).

## 0. Rules that save the most time

1. **Read the whole `SKILL.md` of a kind before writing the first file of that kind**, and start from its `data/`
   skeletons. The long `reference.md` next to it is for looking things up, not for reading front to back.
2. **Build what the spec lists, nothing more.** No extra currencies, languages, screens, roles or documentation
   files. Where the spec says "labels easy to localize", English resx entries are enough (see
   [`localize-configuration`](../localize-configuration/SKILL.md)).
3. **Use the engine's mechanism, not a custom layer.** Pages, controllers, EF configuration, DI wiring and the
   client services are generated from the Dto/entity attributes - never hand-write them and never edit
   `Generated.Net/`, `*.Designer.cs` or migration snapshots. If a generated page looks wrong, the fix is in the
   Dto's attributes. A need the vocabulary cannot express is a reason to stop and ask, not to write a Razor page.
4. **"The backend builds" proves little.** A missing resx key renders the raw key, a missing client DI entry
   crashes the form in the browser, a wrong register write only shows when a document is submitted. Every stage
   below ends with a check that exercises the feature (a test, or a look in the browser), not only `dotnet build`.
5. **Registers are written by documents**: from a document behavior's `OnSubmitAsync`/`OnUnsubmitAsync`/`OnDeleteAsync`
   through the `ISubmitScope` parameter, the only way to reach the balance, turnover and document-bound info writers.
   (The independent info register has a plain injectable writer, but no form and no consumer; it is not a way to feed a
   register from a dictionary.) Balances are never edited directly.
6. **Document lifecycle is fixed by the engine:** Save stores a draft; Submit posts (runs `OnSubmitAsync`); saving
   an already-posted document re-posts it with the new values (the record set is diffed in place); saving it as
   a draft unposts it; Unsubmit reverses; Delete is refused while posted. "Edit or unpost reverses and reposts" in a
   spec is already true, provided `OnSubmitAsync` writes the *complete* record set for the document every time.
7. **Do not touch the scaffolded plumbing you do not need to change** (`Program.cs`, the host,
   Dockerfile, compose files): the scaffold already runs. That includes the empty chart of accounts: without
   double-entry accounting it stays empty (Kandra Core 0.5.21+; see `post-to-accounting` for older cores). When something fails, read the real exception
   before changing anything; [`troubleshoot-configuration`](../troubleshoot-configuration/SKILL.md) lists the failures agents actually hit, and
   [`look-up-engine-api`](../look-up-engine-api/SKILL.md) says where to find a signature.
8. **Describe the model for the AI agent that will use it.** The built-in MCP server shows an agent the plain-English
   `[Description]` of every Dto class and its non-obvious fields, of every register class and of **every register
   Dimensions/Resources/Details property** (registers have no Dto, so those are the only explanation an agent gets: unit,
   currency, sign, sentinels). Write them as you create each entity; the `data/` skeletons carry the placeholders.

## 1. Map the specification onto entity kinds

Write the mapping down (a short table in your plan) before any code. Use the engine's vocabulary; 1C and Odoo
terms are given because specs are often written in them.

| The spec says | Kandra kind | Skill | Notes |
|---|---|---|---|
| catalog, directory, master data, reference list | **Dictionary** | `create-dictionary` | Flat for plain lists. Anything with a parent ("category with a parent", "group of items") is a **hierarchical dictionary** (folders and leaves, with path and cycle checks, tree selectors and tree list generated); never fake a tree with a self-reference. Selectors in documents pick leaves by default; `NodeSelection = HierarchicalNodeSelection.Any` (or `Folders`) lets a field pick a folder. |
| transaction, operation, order, receipt, act, voucher | **Document** | `create-document` | Has `Date`, `Code` (auto-number), optional lines, Submit/Unsubmit. |
| running balance, stock, amount owed (accumulation register *with* balances) | **Balance register** | `create-register` | Written by documents only. Resources are never negative; direction is the movement's `IsExpense` flag. |
| totals per period/category, no running balance needed (turnover register) | **Turnover register** | `create-register` | Event log with period queries; no balance table and no balance guard, but movements are never negative either: a decrease (a refund, a sale that reduces a cost total) is `IsExpense = true`, read signed. |
| a value valid *from a date* (exchange rate, price, setting per period) - information register | **Info register** | `create-register` | A register has no form. Users enter it through a **document** (a "set rates" document with a line per currency); a report shows it. |
| plan, budget, target, schedule | **Document** + **info register** | `create-document`, `create-register` | A plan is a value per period and key, so a plan document posts a document-bound info register (period = the plan month); plan-vs-fact is a report joining it with a turnover register (see `reference.md`). |
| dashboard, statement, journal, analytical screen | **Report** | `create-report` | Parameters Dto + computed result lines; the query lives in `IReportsRepository`. A dashboard is a report with no (or few) parameters and one grouped grid. |
| wizard, import, batch action, period close | **Data processor** | `create-data-processor` | Not persisted. |
| company setting, rate, system-wide parameter | **Constant** | `create-constant` | Dated values, read as of a date. |
| fixed list of choices (kind, status, type) | **Enum** | `create-enum` | Plain C# enum with localized captions. |
| a field that recomputes from the server while editing | **Handler** + **UI behavior** | `create-handler`, `create-ui-behavior` | Row sums and totals need only a UI behavior. |
| printable form / export to CSV, PDF, Excel | **Print form** | `create-print-form` | Reports export through their print form; the formats are a flag list (`ExportFormat.Csv | Pdf | Excel`). |
| debit/credit ledger with chart of accounts | **Posting subsystem** | `post-to-accounting` | A separate engine from registers. Only when the spec asks for double-entry accounting. |

Things the spec may ask for that the engine has **no** feature for - say so in your report instead of building a
custom layer: charts and graphs (reports are tables and pivot tables), custom pages outside the generated forms,
multi-user permissions beyond the built-in roles (out of scope for most single-user specs), several files on one
record (a `[FilePicker]` field holds one file; use one field per file the spec names, or say so), and a **schedule that
ships with the configuration**: every data processor is schedulable with no code (`create-data-processor` step 4), but
the schedule itself (daily at 07:00, run as whom) is created by an admin under Admin > Scheduler (`/admin/scheduler/jobs`). For "a daily job",
build the processor, test it, and write in the README how to schedule it; never claim a job runs that nobody created.

Already provided, do not build: users, login, roles and claims, numbering (`ISimpleNumberingSystem`), list
filtering/sorting/paging, change history, soft delete, import packages, constants admin page, notifications.

## 2. Plan before coding (ten lines is enough), then review it

- Entities: name, kind, key fields, which dictionaries they reference.
- **Posting table**: for each document, which register it writes, the dimensions, the resource and the direction.
  Every acceptance check in the spec should be traceable to a row of this table.
- Reports: what each reads (which register or entity), its parameters, its result columns.
- Navigation: the sidebar tree (folders for Documents / Reports / Reference data).
- Seed data: what the demo package contains. Nothing the code depends on may exist *only* there: a real installation
  never applies the demo package, so the application must work on an empty database (reference.md section 1).

**Then check the plan against the red-flag table in [`reference.md`](reference.md) section 1.** Two mistakes are
common and expensive: modeling a value that holds from a date (an exchange rate, a price) as a *dictionary* row, and
modeling a balance (an account's opening balance) as a *field on a dictionary*. In Kandra the first is a register
written by a "set values" document and shown by a report; the second is an opening-balance document posting to the
balance register. If the spec's wording suggests the literal reading, build the right model and make the screens match
the spec (reference.md section 10).

## 3. Build order and gates

Dependencies decide the order: a register's dimensions reference dictionaries, a document's behavior needs the
registers it writes, a report reads both. Build the **whole stage, then run its gate**.

| Stage | Build | Gate |
|---|---|---|
| 0. Baseline | Nothing yet: `dotnet build`, `dotnet test`, run the app and sign in (section 4). | All green; login works. You now know the scaffold is healthy, so any later failure is yours. |
| 1. Foundations | Enums (`create-enum`), constants (`create-constant`, e.g. the base currency). | Build. |
| 2. Dictionaries | `create-dictionary` for each (referenced ones first). | Build; `dotnet test --filter LocalizationKeysTests` clean. |
| 3. Registers | `create-register` for each. | Build. |
| 4. Documents | `create-document` for each, with `OnSubmitAsync` writing the registers; add the **acceptance tests** now (`test-configuration` where the configuration has that skill, otherwise follow the existing tests in `*.Tests`) - one per posting rule, including edit/unsubmit. | Tests pass. |
| 5. Reports | `create-report`; query methods on `IReportsRepository`; tests that compare report output with posted documents. | Tests pass. |
| 6. Navigation | `add-interface-node`: every entity and report reachable from the sidebar; check claims for new entities in the claims seeder. | Build. |
| 7. Migrations | **One** migration for the whole model (`dotnet kandra-migrate add <Name>`; see `AGENTS.md`). While the model is still moving, `dotnet kandra-migrate remove` the unreleased one and add it again; never hand-edit one. | `dotnet kandra-migrate status` clean; tests (they apply the migrations) pass. |
| 8. Demo data | `author-import-package`: the demo package, then `UPDATE_IMPORT_SCHEMA=1 dotnet test`. | Package validates; a demo run starts with the data. |
| 9. Acceptance | Every acceptance check of the spec as a test; then walk the UI (section 4). | See below. |

Migrations are needed before tests that touch the database (the test host applies them), so do stage 7 as soon
as stages 2-4 compile, then regenerate it as the model changes. Order matters less than not forgetting: the
migration, the demo package's schema snapshot and the nav entries are the three things most often left stale.

## 4. Run it and look at it

```bash
dotnet run --project src/{Config}.WebApi --launch-profile {Config}.UI     # https://localhost:7220 (http://localhost:5220 redirects to it)
```

- **First start** shows the first-run wizard (sets the admin password). To skip it set `Kandra__AdminPassword`,
  or run the demo installation: `Kandra__Setup__Mode=Demo` together with `ACCEPT_EULA=Y` (the licence must be accepted
  before any setup runs; without it the demo data is never applied) applies the demo package and creates the sample users.
  In demo mode sign in as `admin` / `Admin!1` (sample users `user0`/`User!0`, `user1`/`User!1` ...).
- Call the API/UI over **https** and accept the development certificate: a redirect from http drops the
  `Authorization` header in many HTTP clients.
- Scripting against the API: every generated endpoint is listed at `/swagger` (`/api/v1/{documents|dictionaries|reports|...}/...`).
  Request bodies are the Dtos themselves, with no wrapper object: a report's run takes the filters Dto at the root of the
  JSON, a document's print takes `{"format": "Pdf"}`. Read `.../new` first to see a Dto's shape.
- Data lives in the SQLite file under the app folder (`Kandra:DataPath`); delete the file (or set
  `Kandra__Database__ResetToken`) to start clean after a model change that is not yet migrated.
- **UI walk, per entity kind** (do this at stage 4 and again at the end; it takes minutes and catches the failures a
  build cannot): open the list; click Add; the form must render without a console error; save an invalid form
  (messages appear in English, not as raw keys); save a valid one; for a document Submit it, then check the register
  effect (a report, or the document's View page tabs), edit it, Unsubmit it. A new document must open with its number
  and date filled in (a missing one is a wrong `OnNewAsync` signature, `create-document` step 4). Open every report and
  data processor page too: "An error occurred deserializing the response" there is a Refit `[PathPrefix]` that matches no
  route (`troubleshoot-configuration`).
- A form that crashes on open, or a list with raw upper-case labels, is nearly always a missing client validator
  registration (`AddValidatorsFromAssembly` in the client services - the template already has it) or a missing resx
  key (`localize-configuration`).

## 5. Done means

- `dotnet build` and `dotnet test` pass; every acceptance check of the spec is a test or was demonstrated in the UI.
- Every Dto class and every register (class and properties) has a real `[Description]`, not a placeholder.
- A short `README.md`: prerequisites, build, run, login, tests. Update the existing README instead of adding files;
  do not write handover or report documents the task did not ask for.
- No leftover scratch files, test databases, screenshots or scripts in the repository.
- Say plainly what you did not do and why (a spec item with no engine feature, a choice you made where the spec was open).
  Describe only what exists: before writing "a daily job sends reminders", find the job.
