# Family Garage - Google Antigravity

| | |
|---|---|
| Harness / model | Google Antigravity / Gemini 3.8 Flash (medium) |
| Brief | [PROMPT.md](../../prompt/PROMPT.md) v4 |
| Template / Kandra Core | 1.3.6 / 0.5.25 |
| Date | 2026-10-24 |
| Time to first delivery | about 2 h 50 min, from scaffolding to the final report |
| Tests | 23 delivered (eleven acceptance tests of the agent's own list plus the template's tests); 36 after the fixes below |

## Verdict

The model follows the brief. The car stores no mileage: the odometer is a periodic information register. Jobs such as
"Oil change" are work items with their interval in kilometres and months. The debt is a balance register, and the price
is counted once on purchase, so principal is not a cost. Every screen is generated, and English and Ukrainian resources
are complete. The code did not hold up under review, though. A service with two work items could not be posted. A posted
leasing buyout could not be saved again. Reminders never came back for the next oil change. For a user east of UTC, the
dashboard and the reports disagreed about the month. Five of the brief's eleven acceptance checks had no test, although
the agent's report said all eleven did. All of this was fixed by hand (see below).

## Deviations from the brief

- **The acceptance tests follow the agent's own list, not the brief's.** The brief's checks 3 (changing, un-posting and
  deleting a Service), 4 (km or months, whichever comes first), 7 (total cost of ownership), 9 (every document posts on
  an empty database) and 11 (the dashboard equals the reports) had no test. They are added in
  `Acceptance/BriefChecksTests.cs`. Checks 3, 4, 7 and 9 pass on the delivered code; check 11 failed (bug 8).
- **The demo data did not match the brief.** It had one bank-loan contract instead of a leasing contract and a loan,
  five payments instead of monthly payments up to last month, 8 items instead of about 25, and about 20 documents instead
  of about 30. Nothing was overdue on the day it was loaded. The package was rebuilt to the brief (generated with
  consistent odometer readings and payment schedules), and `ImportPackages/DemoDataTests` now applies it and checks the
  overdue job, the policy ending within the look-ahead and the payments due.
- **No README.** The brief asks for a short `README.md` (build, run, sign in) and the brief and the template's skills ask
  for instructions on scheduling the reminders; the agent left the template's README unchanged and wrote a handoff guide
  and an implementation report instead. Those two are not published (their useful content is in this file, and they name
  Kandra Core 0.5.21; the solution is on 0.5.25). The README was written by hand.
- **Some reports read documents instead of registers:** Financing (the financed amount and the principal paid; now from
  the debt register), Fuel consumption (litres from the Refuel documents) and the next payment date in Upcoming (the last
  posted payment). Interest and fees per contract are in no register, so Financing still sums them from the payments.
- The launch ports were moved from the template's 7220/5220 to 7226/5226: the trial's own instance held 7220 on the build
  machine, and the examples now use one port each.

## Review notes

- **The reports query the register tables directly** (`dbContext.Set<OdometerRecord>()`, `Set<VehicleCostsMovement>()`,
  `Set<FinanceDebtBalance>()`) through EF LINQ instead of the engine's register readers. Unlike the previous run, every
  query filters out un-posted movements, so the numbers are right, but the reports depend on how the engine stores
  registers. Upcoming also runs a few queries per vehicle and work item, which is fine for a household.
- A sold car still gets service and policy reminders. The brief does not say otherwise.
- A car with no purchase document counts its jobs from the day it was entered and from 0 km.
- The reminders job keeps the look-ahead it was created with; changing the constants later does not change the job.
- Not this configuration's doing: report grids show yes/no columns as `True`/`False` in every language, and a
  document-bound register's balance guard reaches the user as the engine's English message with the row's ids (the
  configuration now checks the rule a user can hit first, see bug 4).

## Fixed after the run

1. **A detailed Service with two or more work items could not be posted** (`UNIQUE constraint failed:
   Reg_LastDone.DocumentId, Reg_LastDone.LineId`). Each record of the service lines and "last done" registers now carries
   its line's id. The agent's tests and demo data had at most one work item per service.
2. **A posted finance payment that pays off the rest of the debt could not be saved again** (the leasing buyout, or any
   payment whose principal is more than the debt left after it). The check read the balance with the payment's own
   principal already taken off; it now adds it back.
3. **A financed purchase changed to cash kept its debt**: the debt register was not posted again for cash.
4. **Un-posting a financed purchase whose contract has payments** failed with the engine's balance message (English, with
   the contract's id). It now fails with a localized message, and so does changing the purchase's contract or lowering
   its financed amount below what has been paid.
5. **A purchase accepted the contract of another car or of the other kind** (a leasing contract for a bank-loan
   purchase), and a down payment equal to or above the price. Both are refused now.
6. **Reminders were sent once per job forever.** The de-duplication key named the vehicle and the work item (or the
   policy kind) but not the occurrence, so after the oil change was done and became due again, no reminder came. The key
   now includes the basis of the occurrence (the last time it was done, the date a policy ends).
7. **Fuel consumption ignored partial refuels**: it divided one refuel's litres by the distance from the previous refuel,
   full or not. It now adds up every refuel since the previous full tank, as the brief says, in the report and in the
   dashboard tile.
8. **Months and "today" were the server's or UTC's, not the user's.** For a user in Kyiv, a cost dated the 1st of the
   month fell into the previous month in the Costs pivot and the chart, and the dashboard's "this month" tile showed 0
   while the pivot showed 202 500 for the same month. Upcoming counted days from the server's date, and a new document
   got the server's local midnight. All of them now use the user's calendar.
9. **Total cost of ownership showed today's debt, not the debt at the end of the period**, and counted kilometres only
   between readings inside the period. A period's last day, and Financing's "as of" day, now include documents posted
   later that day.
10. **Text in English or hard-coded**: event and policy kinds as enum names in Upcoming and on the dashboard, the chart's
    series names, the payment and service kind on print forms, "UAH" on every print form and tile (the configuration's
    currency constant was never read), "L/100km", and "in 12 days" built in the repository. The reminder's body never
    showed the due date or kilometres it was given (a notification's body is shown with the title's arguments); it now
    says when the event is due. All of it now comes from the resources in both languages.
11. **The dashboard's "next five" used a fixed 30 days and 1 000 km** instead of the look-ahead constants, so it could
    disagree with the Upcoming report it opens; a look-ahead of 0 silently became 30.
12. Smaller: report dates showed a time of day, dashboard tables printed numbers unformatted, three unused-parameter
    warnings.

The tests pass after the fixes: 36 of 36, the agent's 23 and 13 new ones, run as a user in Kyiv (`BriefChecksTests`,
`DemoDataTests`). The tests for bugs 1 to 8 were run against the delivered code: each fails there and passes with the fix.

## Previous run

None.
