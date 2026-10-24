# Ideas for more examples

Domains considered and kept for later. Most of what they would show is already covered by the published examples.

| Idea | What it would show | Why not now |
|---|---|---|
| Tutor / small studio (students, lesson packages, lessons, payments) | prepaid lesson balance per student, money settlements, pivot "chess" schedule (weekday × hour), monthly invoice print form | settlements are covered by Home Utilities; worth it if a pivot-heavy example is wanted |
| Club membership (members, fee plans, monthly accrual, payments, attendance) | scheduled monthly accrual, debt per member, a selector filtered by another field (plan by member type), Accrual → Payment link | the scheduler is covered by the reminder jobs, debt by Home Utilities |
| Collection catalog (coins, stamps, vinyl: acquisitions, sales, valuations) | deep hierarchy, photos, periodic valuation register, realised and unrealised gain | light on posting |
| Small landlord (units, tenants, leases, rent accrual, utility rebilling, payments) | lease contracts, scheduled rent accrual, occupancy pivot, invoice print forms | about 1.5 h; overlaps Home Utilities and Club membership |
| Home renovation project (tasks, contractors, materials, payments) | budget vs. actual on a project, materials bought vs. used | overlaps Personal Accounting's budgets |
| Pet care / family health (vaccinations, visits, medications) | due-by-interval reminders | the same pattern as Family Garage and Home Pantry part B |
| Beekeeping / small farm (hives, inspections, harvest, treatments, sales) | inventory of produce and sales from a small holding | niche audience |

## Adding an example

1. Make a folder named after the example (kebab-case) with a `README.md`, the brief in `prompt/PROMPT.md` (or several,
   ordered, when the example is built in steps) and an empty `runs/`.
2. Keep the shape of the existing briefs: *First*, *What to build*, *How the application should be modelled* (with the
   domain's key modelling decisions, so the agent does not have to guess them), reference data, documents, registers and
   posting, screens, non-functional, seed data, acceptance checks (each an automated test), *How to work*, *Deliver*.
3. Size it for about an hour: four or five catalogs, up to six documents, four or five registers, five or six reports and a
   dashboard.
4. Add a row to the table in the root `README.md` and remove the idea from this file.
