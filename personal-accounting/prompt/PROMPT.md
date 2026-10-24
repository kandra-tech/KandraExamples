# Personal Accounting & Budgets - build brief (v4)

## First

1. Read the platform's getting-started page, `https://docs.kandra.tech/getting-started`, and follow it to create the
   project in this folder: configuration name `PersonalAccounting`, SQLite only. Build and run the empty project once
   and sign in, so you know the starting point is healthy.
2. In the new project, read `AGENTS.md`, then the skill it names for building a whole application from a
   specification. The project's `AGENTS.md` and its skills folder are the rest of the documentation.

## What to build

A single-user, local application to record personal money movements, see balances, and compare spending with monthly
budgets. Build exactly what is listed here. Anything not listed is out of scope.

## How the application should be modelled

The framework uses the classic 1C:Enterprise-style metadata model; model the domain in its terms rather than as tables
and screens:

- **Catalogs (dictionaries)** hold the *things* people refer to.
- **Documents** record *events* on a date. A document **posts** to registers when it is posted and takes its postings
  back when it is un-posted or changed.
- **Registers** hold everything derived. An **accumulation register with balances** keeps running totals; an
  **accumulation register of turnovers** keeps period totals; a **periodic information register** keeps a value that is
  valid from a date (exchange rates). Registers are never edited by hand and have no form of their own: users change
  them only by posting documents.
- **Reports** read registers (not the documents' tables) and compute their rows on request.
- A **dashboard** is its own kind, not a report: a page of figures and charts computed from the registers, opened
  without filling anything in and refreshed on a timer.
- Grouping inside a catalog (a category under a parent category) is the catalog's own hierarchy of groups and items,
  not a "parent" field.
- Fixed lists of choices are enumerations; system-wide settings are constants.

Where this brief says "balance", "turnover", "rate" or "planned amount", that is a register. In particular: **exchange rates are a
periodic information register entered through a "Set exchange rates" document (a date plus one line per currency) and
shown by an "Exchange rates" report; there is no rates catalog.** **Opening balances are entered through an "Opening
balances" document (a date plus one line per account) that posts to the account balance register; the account itself
stores no balance.**

## Reference data (catalogs)

- **Account**: name, type (cash / bank / card / savings), currency.
- **Category**: name, kind (income / expense), optional parent (one level).
- **Currency**: code, name. Seed UAH, USD, EUR, PLN. The base currency (UAH) is a setting.

## Documents

All documents have a printable PDF form with the company letterhead, document number, date, details, and totals.

- **Opening balances**: date; lines of (account, amount).
- **Set exchange rates**: date; lines of (currency, rate to the base currency).
- **Income**: date, account, category, amount, comment.
- **Expense**: date, account, category, amount, comment.
- **Transfer**: date, from account, to account, amount out, amount in (they differ when the currencies differ), comment.
- **Budget** (one per month): month; lines of (expense category, planned amount).

## Registers and posting

| Register | Kind | Written by | Meaning |
|---|---|---|---|
| Account balances | accumulation, balances | Opening balances (in), Income (in), Expense (out), Transfer (out of the source account, in to the target account) | amount per account in the account's own currency |
| Category turnover | accumulation, turnovers | Income, Expense | amount per category and account per day, in the account currency **and** in the base currency at the rate valid on the document's date |
| Exchange rates | periodic information | Set exchange rates | rate of each currency valid from the document's date |
| Budget plan | periodic information, bound to its document | Budget | planned amount per expense category for the budget's month |

Posting rules: saving a **posted** document re-posts it with the new values; un-posting or deleting it takes its
entries back; a document can be deleted only when not posted. Balances are never edited by hand.

## Screens

1. **Dashboard** (the home page; opens without parameters and refreshes itself every minute): tiles for the total
   balance in the base currency (rates as of today), this month's income and this month's expense (each against last
   month), and the share of this month's budget already spent; income and expense per month for the last six months (bar
   chart); this month's expenses by category (donut, the five largest and the rest as "Other"); the balance of each
   account in its own currency and in the base currency (table).
2. **Transaction journal** (a report over the account balance register's movements): the operations in a date range, filterable
   by account and category, one row per movement with its document (a transfer appears once for each account).
3. **Budget vs. actual** (a report): choose a month; per budget category: planned (from the budget plan register), actual
   (from the category turnover register), remaining, % used, and an overspent flag.
4. **Reports**: spending by category for a period; account statement (opening balance, entries, closing balance for a
   period).
5. Forms for the catalogs and documents above; quick-add entries for Income / Expense / Transfer in the navigation.

Reports are tables (no charts; charts belong on the dashboard) and export to CSV, Excel, and PDF.

## Non-functional

Local database, no cloud services. Amounts are decimals. UI languages: English and Ukrainian (labels as resource strings in both English and Ukrainian resource files). Login: the framework's default.

## Seed data (the demo data of the project)

Opening balances for three accounts (cash, bank card, savings); about 15 categories (Salary, Groceries, Rent, Transport,
Utilities, Health, Entertainment, ...); rates for USD, EUR, PLN; about 30 income, expense and transfer documents over two
months (posted); one budget.

## Acceptance checks (write each as an automated test)

1. Posting an Expense reduces the account balance and increases the category turnover.
2. A Transfer between accounts in different currencies changes both balances correctly.
3. Changing a posted document, un-posting it, or deleting it updates balances and turnover correctly.
4. Budget vs. actual equals the sum of posted expenses per category for the month.
5. An account statement's closing balance equals the account balance on that date.
6. The dashboard's figures for this month equal the reports for the same month: expense as in spending by category,
   each account's balance as in its account statement.

## How to work

Work in stages and check each before the next: reference data, then registers, then documents (with their tests), then
reports, the dashboard, navigation, one database migration, demo data. At the end run the whole test suite, start the application and
open every form and report and the dashboard once in a browser. Do not ask questions unless something blocks you.

## Deliver

The running project with a short `README.md` (how to build, run, sign in) and the tests. Do not write other documents.
Finish with a reply of at most fifteen lines: what was built, how to run it, and any place where you had to deviate from
this brief and why.
