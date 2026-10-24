# Personal Accounting & Budgets - Google Antigravity

| | |
|---|---|
| Harness / model | Google Antigravity / Gemini 3.8 Flash (medium) |
| Brief | [PROMPT.md](../../prompt/PROMPT.md) v4 |
| Template / Kandra Core | 1.3.5 / 0.5.25 |
| Date | 2026-10-24 |
| Time to first delivery | about 65 min, from scaffolding to the final report |
| Tests | 18 delivered (the six acceptance checks plus the template's own tests: localization keys, text in repositories, demo package, service resolution, migrations, letterhead); 21 after the fixes below |

## Verdict

The model is right. Balances, turnovers, rates and the budget plan are registers that only documents write. Rates and
opening balances are documents, not catalog rows. Categories are a catalog hierarchy, and the dashboard is a real
dashboard, not a report. The code is small and conventional: no hand-written pages or controllers, English and Ukrainian
resources complete, and demo data dated relative to today. The review found seven bugs, all fixed by hand
(see below). The most serious, un-posted documents still counted in the reports, comes from the reports querying the
register tables directly. That approach is the main thing left to improve.

## Deviations from the brief

- The brief asks for one document besides the code, a short `README.md`. The agent also wrote a handoff guide and a long
  implementation report. They are not published: their useful content is in this file, and parts of them were wrong (they
  name .NET 9 and Kandra Core 0.5.21; the solution is on .NET 10 and 0.5.25). The run's `README.md` was rewritten so its
  run and sign-in steps actually work (the agent's said the password comes from "default framework credentials").
- The launch ports were moved from the template's 7220/5220 to 7225/5225 because something else held 7220 on the build
  machine.

## Review notes

- **The reports read the register tables directly** (`db.Reg_AccountBalances`, `db.Reg_CategoryTurnover`, ...) through
  EF LINQ, instead of the engine's register readers (`Balances`, `Movements`, `SliceOfLast`). That is how bug 1 below got
  in. Even with the fix, the account balance is re-summed from every movement instead of read from the balance table, and
  the reports depend on how the engine stores registers. The next run should use the readers.
- The dashboard's six-month chart groups turnovers by the UTC month; an operation in the first hours of a month (local
  time) can land in the previous bar. With the demo data (documents at local midnight) it does not show.
- The transaction journal's Inflow and Outflow totals add amounts in different currencies (the USD leg of a transfer
  is summed with UAH). The brief does not ask for totals there.
- Not this configuration's doing: report grids show yes/no columns as `True`/`False` in every language, and the start
  page shows the engine's default description, which mentions a warehouse.

## Fixed after the run

1. **Un-posted documents still counted in every report and on the dashboard.** Un-posting a document keeps its register
   movements but marks them inactive. The readers skip inactive movements, but the reports' own queries did not filter
   them. Each query now reads active movements only. The acceptance test for check 3 checked balances only through the
   readers, so a Budget vs. actual assertion was added to it: without the fix it fails, with the fix it passes.
2. **The "budget spent" tile showed 0 %** while Budget vs. actual showed 50.7 % for the same month. The dashboard passed
   the month's start in UTC, which for a user east of UTC falls on the last day of the previous month, so it read that
   month's (empty) budget. The acceptance test for check 6 did not catch it because the test host runs in UTC.
   Fixed together with bug 5.
3. **A missing exchange rate counted as 1.** The dashboard's base-currency balance converted an account with no rate at
   1:1, and the Exchange rates report listed a currency with no rate as rate 1. The dashboard now fails with the
   configuration's own localized "missing exchange rate" message, as posting already did, and the report lists only the
   currencies that have a rate.
4. **The account type in the dashboard's table was the English enum name** (`Card`, `Savings`) in both languages. It now
   uses the enum's resource caption.

5. **A month picked as a date fell into the previous month** for a user east of UTC. The Budget document and the Budget
   vs. actual report took the month of a UTC instant: a budget dated 1 November (31 October 22:00 UTC in Kyiv) became
   October's, and so did the report for "1 November". Both now use the calendar month in the user's time zone, also for
   the bounds of the actual amounts (`LocalMonth` in the Domain project), and so does the dashboard tile.
6. **"One budget per month" was not enforced.** A second budget for the same month posted too, and the report added
   both plans together. Posting it is now refused with a localized message until the other one is un-posted.
7. **An expense could use an income category, and an income an expense category.** Such a document is now refused when
   it is posted (a draft can still be saved). The category selectors still list both kinds.

Bugs 1 to 4 came out of the first review, 5 to 7 out of a second one. The tests pass after the fixes: 21 of 21, the
agent's 18 (one with an added assertion, see bug 1) and three new ones in `Acceptance/MonthAndCategoryRulesTests.cs`,
which run as a user in Kyiv. Each new test was checked to fail with its fix taken out.

## Previous run

None.
