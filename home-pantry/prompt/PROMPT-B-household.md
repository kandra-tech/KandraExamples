# Home Pantry, part B: household - extension brief (v1)

The second step of the Grocy-inspired application. It **extends the existing `HomePantry` configuration** built from
[PROMPT-A-stock.md](PROMPT-A-stock.md); it does not start a new project.

## First

1. Open the existing `HomePantry` project in this folder. Build it, run its tests and start it once, so you know the
   starting point is healthy.
2. Read the project's `AGENTS.md` and the skills it names for planning and for each kind of object you add. Read the
   existing catalogs, documents, registers and reports before adding anything, and reuse them where this brief says so.

## What to build

Household routines on top of the pantry: recurring chores, batteries that need charging or replacing, household
equipment with its manuals and warranties, and a weekly meal plan that feeds the shopping needs. Build exactly what is
listed here. Anything not listed is out of scope. Do not change the behaviour of part A except where this brief says so.

## How the application should be modelled

Same model as part A: catalogs hold things, documents record events and post to registers, registers are never edited
by hand, reports read registers. In particular: **"last done", "last charged" and "planned" are registers written by
documents; a chore or a battery stores no date of its own.**

## Reference data (catalogs)

- **Household member**: name. Seed the members of the demo family.
- **Chore**: name, recurrence (enumeration: daily / weekly / monthly / every N days), N (only shown and required for every
  N days), assigned to (household member), description.
- **Battery**: name, used in (text), charge interval in days, battery kind (rechargeable / disposable).
- **Equipment**: name, location (part A's Location catalog), bought on, warranty until, manual (file), photo.

## Documents

- **Chore done**: date, chore, done by (household member), comment.
- **Battery charged**: date, battery, replaced (yes / no; a disposable battery is always replaced).
- **Meal plan**: week (its Monday); lines of (day, recipe, servings). It does not consume anything; cooking stays the
  part A Cook recipe document.

## Registers and posting

| Register | Kind | Written by | Meaning |
|---|---|---|---|
| Chore log | periodic information | Chore done | per chore: who did it last, valid from the document's date |
| Battery log | periodic information | Battery charged | per battery: last charged, valid from the document's date |
| Battery cycles | accumulation, turnovers | Battery charged | per battery and day: charge cycles and replacements |
| Planned meals | information, bound to its document | Meal plan | per day and recipe: planned servings |

Same posting rules as part A.

## Changes to part A

- **Shopping needs** gets an optional "for the meal plan of week" parameter: the ingredients of the planned meals
  (scaled to their servings) count as needs, on top of the minimum stock and the chosen recipe.
- The **Pantry dashboard** gets two more tiles, overdue chores and batteries due (both lower is better), and a table of
  today's chores and planned meals.

## Screens

1. **Chores** (a report, no required parameters): per chore the last time it was done, by whom, the next due date and an
   overdue flag.
2. **Batteries** (a report, no required parameters): per battery the last charge, the next due date, overdue flag, and
   the total charge cycles.
3. **Equipment** (a report): warranties that end within a chosen number of days.
4. **Week plan** (a report): choose a week; days as rows, recipes and servings, and whether each meal can be cooked
   from stock.
5. **Calendar** (a report): choose a period; one row per day with the chores and batteries due and the planned meals.
6. Forms for the new catalogs and documents; a quick-add entry for Chore done in the navigation.

## Reminders

A daily scheduled job sends the household's users an in-app notification for chores and batteries that became due, and
for part A's stock batches that expire within the look-ahead period.

## Seed data

Add to the existing demo data: three household members, eight chores (one overdue), four batteries, three pieces of
equipment (one warranty ending soon, each with a manual file), one meal plan for the current week, and posted Chore done
and Battery charged documents over the last month.

## Acceptance checks (write each as an automated test)

1. Posting a Chore done moves the chore's next due date by its recurrence.
2. A chore that was never done is due from the day it was created.
3. Un-posting or deleting the last Chore done brings back the previous last-done date.
4. Shopping needs for a meal-plan week equals the scaled ingredients of its planned meals minus the stock on hand.
5. The dashboard's overdue chores tile equals the overdue rows of the Chores report.
6. All part A tests still pass.

## How to work

Work in stages and check each before the next: catalogs, registers, documents (with their tests), the changes to
part A, reports, the reminder job, navigation, one database migration, demo data. At the end run the whole test
suite, start the application and open every new form and report, and part A's Shopping needs and dashboard, once in a browser. Do not
ask questions unless something blocks you.

## Deliver

The extended project, its `README.md` updated, and the tests. Do not write other documents. Finish with a reply of at
most fifteen lines: what was added, what part A code had to change, and any place where you had to deviate from this
brief and why.
