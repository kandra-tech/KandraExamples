# Modeling patterns for `plan-configuration`

How the usual business shapes are modeled on Kandra, with the reasoning, so the right structure is chosen the first
time. The model is the 1C:Enterprise / Odoo one: **dictionaries hold things, documents record events, registers hold
the derived numbers, reports read registers.** When a spec's wording suggests a different structure, translate it
here first. Skill names point at where the mechanics live.

## 0. The three questions that place any piece of data

1. **Is it a thing people refer to** (account, category, product, currency)? -> **Dictionary**. It has an identity and
   a name; its fields describe the thing and rarely change.
2. **Is it an event that happened on a date** (an expense, a receipt, a rate set, an opening entry)? -> **Document**.
   Events are never overwritten: you correct, re-post or reverse them.
3. **Is it a number that results from events, or a value that holds for a period or from a date** (a balance, a total,
   a rate, a budget line)? -> **Register**, written by documents and read by reports and other documents. Never typed in
   directly.

## 1. Red flags - a design review before you build

If your plan contains one of these, stop and re-model. The first trial of this template made four of them: the rates
dictionary, the opening-balance field, the dictionary writing a register and the self-referencing tree.

| Red flag | Why it is wrong | Do this instead |
|---|---|---|
| A dictionary with a **date and a value** (`ExchangeRate`, `Price`, `Tariff`, `Salary`) | A value that holds from a date is history, and history belongs to a register. A dictionary row has no "as of". Two copies of it (row + register) drift apart on edit and delete. | **Info register** written by a "set values" **document** (section 3). |
| An **amount or quantity field on a dictionary that behaves as a balance** (`Account.OpeningBalance`, `Item.Stock`) | A balance is a result of events. A field on the dictionary is a second source of truth next to the register, every report has to add the two together, and the field cannot be corrected without losing history. | An **opening-balance document** posting to the balance register on the start date (section 2). |
| A dictionary or any non-document **behavior writing a register** (`OnSaveAsync` of a dictionary calling a register writer) | Balance, turnover and document-bound info registers are writable only from a document's `OnSubmitAsync`/`OnUnsubmitAsync`/`OnDeleteAsync`. The independent info register's plain writer looks like a way around that, but then the dictionary row and the register record drift apart on every edit and delete (the trial's rates dictionary did exactly this). A dictionary that wants to feed a register is a document in disguise. | Make it a document. |
| A report that **recomputes balances by summing document tables** | Slower, and it silently disagrees with the register after a re-post. | Read the register (`IBalanceReader`, `ITurnoverReader`, `IInfoRegisterReader`). |
| A report result that is **stored** (a "totals" table, a cached dashboard) | Stale the moment a document changes. | A report computes on request. |
| An **enum-like dictionary** (account types) or a free-text field for a fixed list | Needless tables, no compile-time safety. | A plain `enum` (`create-enum`). |
| One document type with a `Kind` field that switches its whole behavior (Income/Expense/Transfer in one) | Three different posting rules and three different forms in one class. | One document per kind of event. A shared helper class for common code is fine. |
| A **tree faked with a self-reference** (`ParentId` on a flat dictionary) for "category with a parent", "group of items", "account plan" | No folder/leaf distinction, no materialized path, nothing stops cycles, no tree selectors or tree list; every report re-implements the walk. | A **hierarchical dictionary** (`create-dictionary`: folders and leaves). Documents pick leaves by default; set `NodeSelection = HierarchicalNodeSelection.Any` (or `Folders`) on the `[Search]`/`[Dialog]` field when a folder may be chosen (a budget line on a parent category). |
| **Raw SQL** anywhere (`SqlQueryRaw`, `FromSql`, ADO), above all against the engine's own tables (`Sys_Constants`) | The engine's tables are not your schema and change between versions; raw SQL skips the provider differences and every engine service built for the purpose. | EF Core queries and the register readers; constants through `IConstantsService` in the behavior, passed to the repository as a parameter (`create-report` section 2). |
| **UI text built in code that is not a behavior**: English literals in a report repository (`"Opening balance"`, `"Transfer to "`) or written into stored data (a register's `Comment = "Opening balance"`) | It can never be translated: stored text stays in the language it was written in, and a repository has no localizer. | Return or store a *kind* (an enum, a flag, an id); the report behavior or the UI turns it into words with the localizer. Stored text fields hold only what the user typed. |
| A **literal currency, company detail or fallback value** (`CurrencyCode = "UAH"`, a rate of `1` when none is found, `catch { return "UAH"; }`) | Silently wrong output (a USD account printed as UAH) instead of an error. | Read it from the document, the account or a constant; a missing value is an error or a visible gap. |
| Code that **finds a dictionary row by its name** (`c.Name == "Fuel"`, `switch (categoryName) { case "Repair": ... }`) | Breaks when a user renames the row or types it in another language, and on every installation without the demo data, where the row does not exist at all (the post then fails). | A field on the document the user fills (defaulted in `OnNewAsync`), a constant pointing at the entry, or an enum when the set is closed (`create-document` step 4). Reports group by the row's id and show its name. |
| Reference data the **code depends on only in the demo package** ("seed Purchase, Fuel, ..." put into `{name}-demo.json`) | A real installation never applies the demo package, so whatever the code expects is missing there. | Code must work on an empty database: the user creates the rows and picks them (a document field, a settings constant). The demo package only fills them in for the demo. |
| A **negative resource** on a movement, or an emptied `EnsureNonNegative()` to allow one (a sale "reducing" a cost total) | Every register rejects negative movements on purpose; direction is `IsExpense`. Readers and the MCP description then disagree with the data. | Post the positive amount with `IsExpense = true` and read it signed (`create-register` §8a). |
| A report that **queries register tables directly** (`db.Reg_X.Where(r => r.Active ...)`) or picks "the latest" with `OrderByDescending().First()` per key in a loop | Hand-written `Active`/`IsDeleted` filters get forgotten (un-posted and deleted rows count); one query per key per row. | The readers: `SliceOfLast` for the latest value per key, `Movements`/`Balances` for the log and totals (`create-register` §8a). |
| A **refused post that throws `InvalidOperationException`** with an English message | The API answers 500 (a stack trace in Development) and the user cannot read the reason in their language. | `InvalidStateException(localizer[key, ...].Value)`: HTTP 409, the form shows the message (`create-document` step 4). |
| Writing **extra pages/controllers** because a screen "needs custom layout" | Everything is generated from Dto attributes; a hand-made page is outside the engine's authorization, navigation and localization. | Express it as a report or a data processor; if it cannot be expressed, say so. |

## 2. Balances: accounts, stock, anything with a running total

- One **balance register**: dimension = what is held (`Account`), resource = the amount (always >= 0 on a movement),
  direction on the movement is its `IsExpense` flag (false = receipt/increase, true = expense/decrease). The register
  keeps the totals table and the movement log (`create-register`).
- If a balance may legitimately go negative (a credit card, an overdraft), the resources' `EnsureValidBalance()` stays
  empty; if it must not, throw there and the post is refused.
- Every change goes through a document. A document that moves money **out** posts a movement with `IsExpense = true`;
  one that moves it **in** posts `IsExpense = false`. A transfer is one document with two movements sharing a `LineId`
  and differing in `IsExpense`.
- **Opening balances are documents.** An `AccountOpeningBalance`-style document ("Opening balances") with a line per
  account (account, amount) dated at the start of the books; its `OnSubmitAsync` posts a receipt per line. The balance
  register is then the single source of truth: current balance = the register's balance, balance on a date = the
  register's as-of read (`IBalanceReader.Balances(at, ...)`), a statement's opening balance = the as-of read before the
  period. Nothing adds a dictionary field to a sum anywhere. Opening balances may be **seeded** in the demo package as
  documents of that type. If the spec lists "opening balance" under the account, it still means this document (section 10).
- The writer's `PostAsync(documentId, date, movements, ct)` replaces the document's whole record set (diffed in place), so
  `OnSubmitAsync` must always write **every** movement of the document, and `OnUnsubmitAsync`/`OnDeleteAsync` call
  `UnpostAsync`/`DeleteAsync` with the document id. That alone gives "edit = reverse and repost".

## 3. Values that hold from a date: exchange rates, prices, tariffs

- **Document-bound info register**: dimensions = the key (`FromCurrency`, `ToCurrency`), details = the value (`Rate`),
  the record's `Period` = the document's date. A rate is valid from its period until a newer record for the same key
  (`create-register`, `data/DocumentInfo.cs`).
- **A "set values" document** is how users enter it: `SetExchangeRates`/`RateImport` - a date plus lines (currency, rate).
  `OnSubmitAsync` builds one record per line and calls `IDocumentInfoRegisterWriter<TRecord>.PostAsync(...)`; unsubmit and
  delete call `UnpostAsync`/`DeleteAsync` (the worked example is `RateImportBehavior` in the reference configuration;
  `create-register/reference.md` section 8 has the walkthrough). The same document can later be filled by an importer
  (a data processor that creates documents) without changing the model.
- **Reading a rate "as of a date"**: `IInfoRegisterReader<TRecord>` slice-of-last at the date (`SliceOfLast(date, ...)`),
  filtered by the key with `DimensionPredicateBuilder`; never `OrderBy(date).First()` over a table you wrote yourself.
  Wrap it once in a small converter class (`ConvertToBase(currencyId, amount, date)`) and reuse it from documents and
  reports.
- A **report** ("Exchange rates") is how users *see* the register: parameters = date, result = latest rate per currency.
  Together: **register + document + report**. The base currency is a **constant** (`create-constant`), because it is one
  system-wide value read as of a date, not a thing with an identity.
- A dictionary of currencies (code, name) is fine: it is a thing people refer to. Only the *rate* moves out of it.

## 4. Event totals: expenses by category, sales by customer (turnover)

- A **turnover register** (no balance table): dimensions = the analysis keys (category, account, ...), resources = the
  amounts to sum, direction = `IsExpense`. Documents write it in the same `OnSubmitAsync` as the balance register;
  reports read period totals through `ITurnoverReader.Movements(from, to, ...)` or a grouped query.
- Keep the amounts the reports need **in the register at posting time** (amount in account currency *and* amount in the
  base currency converted at the rate of the document's date). Converting later with today's rate rewrites history.
- Make every key a report **groups or totals by** a dimension (category, account). Details are per-movement payload
  (comment, counter account): they can be filtered and shown, but they are not aggregation keys.

## 5. Plan versus fact (budgets, targets, forecasts)

- The **plan** is a **document-bound info register** written by a **plan document**. `Budget` is the document (a month and
  lines of category + planned amount); its register has the analysis key as dimensions (`Category`), the planned amount as
  a detail/value, and the record's `Period` = the first day of the month the plan is for. `OnSubmitAsync` posts one record
  per line (`IDocumentInfoRegisterWriter.PostAsync`); unsubmit and delete take them back; saving a posted budget re-posts it.
  The document says *who planned what and when*; the register makes the plan **queryable** next to the facts, by category
  and period, without opening documents.
- The **fact** is the turnover register. The **report** joins the two registers: for each category of the month, planned
  (the plan register's records of that month: `IInfoRegisterReader.Records(monthStart, monthEnd, ...)`, **not** `SliceOfLast`,
  which would carry last month's plan forward into a month that has none), actual (turnover sum for the month), remaining,
  percent used, an `IsOverspent` flag for highlighting.
- Use `SliceOfLast` instead when the plan really is "valid from a date until changed" (a standing monthly limit, a price
  list), the same shape as a rate (section 3).
- Do not version plan documents ("budget v2" copies): to change a plan, edit the posted document (it re-posts) or post a
  new document for the same period and decide in the report which wins (the later one; say so).
- Test it by posting documents and comparing the report rows with hand-computed sums (section 9).

## 6. Dashboards, journals, statements (reports)

- **Dashboard** = a report with no (or few) parameters showing several small tables (balances per account, month income
  vs expense, top categories). A report has **one** result grid (the generator takes the first `[ReportViewGrid]`), so
  make one long line class with a `Section` column grouped by default (`create-report` section 2a). There is no chart
  widget in the engine: say so and deliver tables.
- **Journal** (all operations in a period, with the document each came from) = a report over a **register's movement log**,
  not a union of document tables. A balance register (and a turnover register) keeps one movement per posted effect:
  period, the document that posted it (`Movement.Document`: code, date, type), dimensions, amount, direction (`IsExpense`)
  and details. Read it with `IBalanceReader`/`ITurnoverReader.Movements(from, to, q => q.Where(...).Select(...))` (the
  projection may dereference `m.Document.Code` and the dimension navigations) and filter by dimensions (account) and
  details (category) in SQL. Therefore **put what a journal row must show into the movement's Details when posting**
  (category, comment, counter account). One row per movement: a transfer appears once per account leg. The line class is a
  read model in `Domain/Entities/ReportingModels`, not a table. Several registers in one journal: read each and merge in
  memory by date.
- **Statement** (opening balance, entries, closing balance for a period) = a report: opening = register balance as of the
  period start, entries = the register's movements in the period (running balance computed in the report), closing = as of
  the period end. Never "opening field + sum". Mind the bounds (`create-register` section 8a): `Balances(at)` includes
  movements dated exactly `at` and `Movements(from, to)` is inclusive, so opening = `Balances(from.AddTicks(-1))`, entries =
  `Movements(from, endOfTo)`, closing = `Balances(endOfTo)` with `endOfTo = to.Date.AddDays(1).AddTicks(-1)`; a test that
  compares the closing balance with the register balance on that date catches an off-by-one.
- Report queries go in `IReportsRepository` (declared in `Domain`, implemented in `Persistence`), because the report's
  behavior in `Application` cannot reference `Persistence`. Query registers through their readers; keep each method
  returning plain lines; map to the result Dto with a static Mapperly mapper (`create-report`).
- **Export** (CSV, PDF, Excel and more) comes from the report's print form: list the formats in `[SupportedExportFormats]` (`create-print-form`); you do not write exporters. Excel gets real numbers and dates, filters, subtotal formulas and outline groups from the same attributes.

## 7. Multi-currency

- Every account has one currency; its balance register holds amounts in that currency (a card account in USD is USD).
- A transfer between currencies has two amounts (out in the source currency, in in the target currency); it posts two
  movements, one per account, each in its own currency. The rate is **not** needed for the posting, only for totals.
- Totals across accounts ("total in base currency") convert **at report time with the rate valid at the report date** (one
  `SliceOfLast` for all currencies, then multiply), while per-event conversions needed for history (turnover by category in
  base currency) are converted at posting time with the document's date rate and stored (section 4).
- A missing rate must not silently become 1: fail the post with a clear message, or fall back only for the base currency.

## 8. Who edits what - reference data, operational data, derived data

| Data | Edited through | Rules |
|---|---|---|
| Things (account, category, currency, counterparty) | Their dictionary form (generated). | A field that posted events depend on (an account's currency, a category's kind) must not change once movements exist: refuse it in the dictionary behavior/validator, or the history silently changes meaning. |
| **Operational data: operations on a date** (income, expense, transfer, receipt, order) | A **document** form (generated: list with filters, add, edit, view). | A document is saved either as a draft (no register effect) or posted (the form offers both); **Submit** posts a draft. **Correct a posted operation by editing it and saving** (it re-posts with the new values) or Unsubmit, edit, Submit. **Cancel** one by Unsubmit (kept as a draft) or Unsubmit then Delete (delete is refused while posted). Never "fix" a register: it has no editor. |
| Opening balances | An opening-balance document. | Same lifecycle as any operation. |
| Rates, prices, plans, targets (values valid from a date or for a period) | A "set values" / plan document + a report to view the register. | Same lifecycle; the register follows the document. |
| System parameters (base currency, company name) | A constant (constants admin page, or a settings data processor). | Dated: read as of a document's date. |
| Bulk entry of operations (a bank statement, a spreadsheet) | A data processor that creates documents through the document service (or an import package for seed data), never one that writes registers. | The documents are then edited, posted and reversed like hand-entered ones. |
| Anything derived (balance, turnover, journal, dashboard) | Nobody; registers are read-only and reports compute. | Users *see* it on a report, the document's "Register transactions" tab (what this document posted), and the document list. |

Every change to a document or dictionary row is kept in its change history (the History tab), so nothing needs an audit
table of your own. Who may do what is the built-in claims and roles; a single-user configuration needs nothing more.

## 9. Tests that prove the model (write them at stage 4/5)

One test per posting rule of your plan, through the real services (the `test-configuration` skill, where present, shows the host and a full
example): create reference data, create the document with the CRUD service, `SubmitAsync`, read the register, assert;
then edit the document and save, assert again; `UnsubmitAsync`, assert the register returned to its previous state;
delete, assert refused while posted. Then one test per report that posts a few documents and compares the report rows
with sums computed by hand. The spec's acceptance checks map one to one onto these tests.

## 10. When the spec and the model disagree

Specs are written by people thinking in screens. Where the spec's wording points at a structure from section 1's red-flag
list (a rates dictionary, an opening-balance field), build the correct model, make the user-visible result match the spec
(a "Rates" menu folder with the set-rates documents and the rates report; balances on a report or the dashboard), and note the translation in your final report in one line. Do not silently build the
literal reading, and do not ask - the mapping table in `SKILL.md` already says which way to go.
