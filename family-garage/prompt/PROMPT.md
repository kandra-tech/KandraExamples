# Family Garage - build brief (v4)

## First

1. Read the platform's getting-started page, `https://docs.kandra.tech/getting-started`, and follow it to create the
   project in this folder: configuration name `FamilyGarage`, SQLite only. Build and run the empty project once and sign
   in, so you know the starting point is healthy.
2. In the new project, read `AGENTS.md`, then the skill it names for building a whole application from a
   specification. The project's `AGENTS.md` and its skills folder are the rest of the documentation.

## What to build

A single-household, local application to keep the history and the cost of the family's cars: what each car cost to buy,
how it was paid for (cash, a bank loan or leasing), what was spent on it since (loan interest, fuel, services, repairs,
insurance, taxes), what is still owed, what was replaced and when, and what is due next. Build exactly what is listed
here. Anything not listed is out of scope.

## How the application should be modelled

The framework uses the classic 1C:Enterprise-style metadata model; model the domain in its terms rather than as tables
and screens:

- **Catalogs (dictionaries)** hold the *things* people refer to.
- **Documents** record *events* on a date. A document **posts** to registers when it is posted and takes its postings
  back when it is un-posted or changed.
- **Registers** hold everything derived. An **accumulation register with balances** keeps running totals (what is still
  owed); an **accumulation register of turnovers** keeps period totals; a **periodic information register** keeps a value
  that is valid from a date (the odometer, the last time a job was done). Registers are never edited by hand and have no
  form of their own: users change them only by posting documents.
- **Reports** read registers (not the documents' tables) and compute their rows on request.
- A **dashboard** is its own kind, not a report: a page of figures and charts computed from the registers, opened
  without filling anything in and refreshed on a timer.
- Grouping inside a catalog (an item under a group of items) is the catalog's own hierarchy of groups and items, not a
  "parent" field.
- Fixed lists of choices are enumerations; system-wide settings are constants.

Where this brief says "cost", "debt", "odometer", "last done" or "history", that is a register. In particular:

- **A car stores no mileage of its own; the mileage is a periodic information register written by every document that
  records an odometer reading.**
- **There is no "service types" catalog: a job such as "Oil change" is a work item in the Items catalog, and its service
  interval is stored on the item.**
- **A finance contract stores no remaining debt: the debt is a balance register, increased by the financed purchase and
  reduced by the principal of each payment.**
- **The car's price is a cost once, on the purchase date, however it was paid for. Paying back a loan's principal is not
  a cost (it pays for the price already counted); interest and fees are.**

## Reference data (catalogs)

- **Vehicle**: name, make, model, year, plate number, VIN, fuel type (petrol / diesel / LPG / electric / hybrid), photo.
- **Item** (hierarchical, groups such as Engine, Brakes, Tyres, Works): name, kind (part / work / consumable), unit,
  default price; for work items an optional service interval in kilometres and an optional interval in months.
- **Provider**: name, kind (service station / insurer / fuel station / dealer / bank / leasing company / other), phone,
  comment.
- **Finance contract**: number, vehicle, kind (bank loan / leasing), lender (provider), signed on, annual interest rate
  in percent, term in months, monthly payment, payment day of the month, comment. Leasing here is leasing with the car
  bought out at the end; a rental without a buyout is out of scope.

Cost categories are a fixed list: Purchase, Interest and fees, Fuel, Maintenance, Repair, Insurance, Tax, Other.

Constants: the currency code shown on forms (UAH), the number of days ahead that "Upcoming" looks (default 30) and the
number of kilometres ahead it looks (default 1 000).

## Documents

A new document gets the next number of its kind and today's date. All documents have a printable PDF form with the
household letterhead, document number, date, details, and totals.

- **Vehicle purchase**: date, vehicle, seller (provider), price, odometer, payment (cash / bank loan / leasing). When the
  payment is not cash, the form also shows the down payment and the finance contract (of the same vehicle and kind), and
  both are required; for cash they are hidden. The financed amount is the price minus the down payment.
- **Finance payment**: date, finance contract, principal, interest, fees, comment; the total is their sum. The leasing
  buyout at the end is a finance payment too. A principal larger than the debt still owed on the contract refuses the
  post.
- **Vehicle sale**: date, vehicle, buyer (text), price, odometer. The sale price reduces what the car has cost. A car
  whose contract still has debt can be sold; the debt stays on the contract until it is paid.
- **Refuel**: date, vehicle, fuel station (provider), litres, amount, odometer, full tank (yes / no).
- **Service**: date, vehicle, service station (provider), kind (maintenance / repair), odometer, comment, **detailed**
  (yes / no). When *detailed* is on, the form shows lines of (item, quantity, price, amount) and the total is their sum;
  when it is off, the lines are hidden and the user enters one total amount. A detailed Service needs at least one line;
  a summary Service needs an amount.
- **Accident**: date, vehicle, odometer, place, description, estimated damage, photo. From an Accident the user can
  create a Service (kind repair) for the same vehicle, linked to it.
- **Policy**: date, vehicle, kind (compulsory insurance / comprehensive insurance / tax / other), provider, valid from,
  valid to, amount, scanned document (file).

## Registers and posting

| Register | Kind | Written by | Meaning |
|---|---|---|---|
| Odometer | periodic information | Vehicle purchase, Vehicle sale, Refuel, Service, Accident | kilometres per vehicle valid from the document's date |
| Vehicle costs | accumulation, turnovers | Vehicle purchase (the full price), Vehicle sale (reduces the costs), Finance payment (interest and fees only), Refuel, Service, Policy | amount per vehicle and cost category per day |
| Finance debt | accumulation, balances | Vehicle purchase (financed amount, in), Finance payment (principal, out) | debt still owed per finance contract |
| Service lines | accumulation, turnovers | Service (detailed only) | quantity and amount per vehicle and item per day: what was changed and when |
| Last done | periodic information | Service (detailed only, work items that have an interval) | per vehicle and work item: the date and the odometer of the last time it was done |
| Policy validity | periodic information | Policy | per vehicle and policy kind: the date the current policy is valid to |

Posting rules: saving a **posted** document re-posts it with the new values; un-posting or deleting it takes its
entries back; a document can be deleted only when not posted. Registers are never edited by hand. An odometer reading
lower than the vehicle's last reading before the document's date refuses the post. The debt on a contract never goes
below zero, so a financed purchase whose contract already has payments posted cannot be un-posted.

## Screens

1. **Garage dashboard** (the home page; opens without parameters and refreshes itself every minute): tiles for this
   month's costs (against last month; lower is better), the debt still owed on all contracts, the number of overdue
   events (lower is better) and the average fuel consumption of the last three months; costs per month by category for
   the last twelve months (stacked bar chart); one row per vehicle with its odometer, total cost of ownership so far,
   debt still owed and next due event (table); the next five due events (table), each opening the Upcoming report.
2. **Upcoming** (a report, no required parameters): every due event within the look-ahead constants or already overdue:
   a work item whose interval in kilometres or in months (whichever comes first) runs out within the look-ahead days or
   kilometres; a policy whose validity ends within the look-ahead days; the next payment of every contract that still has
   debt (on its payment day in the month after its last posted payment, or after the purchase if none). For kilometres,
   "now" is the vehicle's last odometer reading; show remaining km and days (where the event has them) and an overdue
   flag.
3. **Total cost of ownership** (a report): choose a period; per vehicle the purchase price, costs by category (interest
   and fees among them), sale price, net cost, kilometres driven in the period, cost per kilometre and cost per month; the
   debt still owed at the end of the period is shown beside it, not added to the cost.
4. **Costs pivot** (a report): choose a period; vehicles as rows, months as columns, amounts in the cells, with totals,
   so the monthly cost shows the interest of financed cars month by month.
5. **Financing** (a report): choose a date; per contract the vehicle, lender, financed amount, principal paid, interest
   and fees paid, debt still owed, payments made and months left of the term.
6. **Vehicle history** (a report): choose a vehicle and a period; one row per item from the service lines register
   (date, odometer, item, quantity, amount, document), so the owner sees what was replaced and when.
7. **Fuel consumption** (a report): choose a vehicle and a period; litres per 100 km between consecutive full-tank
   refuels, and the average for the period.
8. Forms for the catalogs and documents above; quick-add entries for Refuel, Service and Finance payment in the
   navigation.

Reports are tables (no charts; charts belong on the dashboard) and export to CSV, Excel, and PDF.

## Reminders

An in-app notification for every event from the Upcoming report that has become due or overdue since the last run, each
event notified once. It should run every day without anyone starting it; if the framework needs an administrator to set
that up, say how in the README.

## Non-functional

Local database, no cloud services. Amounts are decimals. UI languages: English and Ukrainian (labels as resource strings
in both English and Ukrainian resource files). Login: the framework's default.

## Seed data (the demo data of the project)

Two cars: one bought new two years ago on leasing (36 months, a down payment, the buyout still ahead), one bought used
last year with a 24-month bank loan; their monthly finance payments posted up to last month. About 25 items in groups
(oil, filters, brake pads, tyres, spark plugs, timing belt, works such as oil change every 10 000 km or 12 months), six
providers (a bank and a leasing company among them), and about 30 other documents over the last year (refuels, three
detailed and two summary services with several lines each, one accident with its repair, insurance and tax policies),
all posted; both cars have a service history. At least one item is overdue, one policy expires within the look-ahead
period and one finance payment is due, on whatever day the demo is loaded. The application must also work on a new
installation without the demo data.

## Acceptance checks (write each as an automated test)

1. Posting a Refuel increases the vehicle's fuel costs and records its odometer.
2. A detailed Service records each line in the service history and moves "last done" for its work items; a summary
   Service adds only its cost.
3. Changing a posted Service, un-posting it, or deleting it updates costs, history and "last done" correctly.
4. Upcoming reports a work item as due when either its kilometre or its month interval runs out, whichever is first.
5. A purchase on a bank loan adds the full price to the purchase costs and the price minus the down payment to the
   contract's debt; a cash purchase adds no debt.
6. A finance payment reduces the debt by its principal and adds only its interest and fees to the costs; a principal
   above the debt still owed refuses the post.
7. Total cost of ownership equals the purchase price, minus the sale price, plus all posted costs of the vehicle
   (interest and fees included, loan principal not).
8. An odometer reading below the vehicle's previous reading refuses the post, with a message the user can read.
9. On an empty database (no demo data), after creating one vehicle, one provider and one contract, every document type
   can be posted, and a new document gets a number and today's date.
10. Running the reminders twice on the same day sends each due event only once.
11. The dashboard's figures equal the reports for the same period: this month's costs as in the Costs pivot, the debt
    as in Financing, the overdue count as the overdue rows of Upcoming.

## How to work

Work in stages and check each before the next: reference data, then registers, then documents (with their tests), then
reports, the dashboard, the reminder job, navigation, one database migration, demo data. At the end run the whole test suite, start the
application and open every form and report and the dashboard once in a browser. Do not ask questions unless something blocks you.

## Deliver

The running project with a short `README.md` (how to build, run, sign in) and the tests. Do not write other documents.
Finish with a reply of at most fifteen lines: what was built, how to run it, and any place where you had to deviate from
this brief and why.
