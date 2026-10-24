# Home Utilities & Meters - build brief (v2)

## First

1. Read the platform's getting-started page, `https://docs.kandra.tech/getting-started`, and follow it to create the
   project in this folder: configuration name `HomeUtilities`, SQLite only. Build and run the empty project once and sign
   in, so you know the starting point is healthy.
2. In the new project, read `AGENTS.md`, then the skill it names for building a whole application from a
   specification. The project's `AGENTS.md` and its skills folder are the rest of the documentation.

## What to build

A single-household, local application for the household's utility bills: meter readings, tariffs that change over time,
the monthly invoices from each provider, payments, what is owed to whom, and how consumption and costs change month to
month. Build exactly what is listed here. Anything not listed is out of scope.

## How the application should be modelled

The framework uses the classic 1C:Enterprise-style metadata model; model the domain in its terms rather than as tables
and screens:

- **Catalogs (dictionaries)** hold the *things* people refer to.
- **Documents** record *events* on a date. A document **posts** to registers when it is posted and takes its postings
  back when it is un-posted or changed.
- **Registers** hold everything derived. An **accumulation register with balances** keeps running totals; an
  **accumulation register of turnovers** keeps period totals; a **periodic information register** keeps a value that is
  valid from a date (a tariff, a meter reading). Registers are never edited by hand and have no form of their own: users
  change them only by posting documents.
- **Reports** read registers (not the documents' tables) and compute their rows on request.
- A **dashboard** is its own kind, not a report: a page of figures and charts computed from the registers, opened
  without filling anything in and refreshed on a timer.
- Grouping inside a catalog is the catalog's own hierarchy of groups and items, not a "parent" field.
- Fixed lists of choices are enumerations; system-wide settings are constants.

Where this brief says "reading", "tariff", "debt" or "consumption", that is a register. In particular: **tariffs are a
periodic information register entered through a "Set tariffs" document (a date plus one line per service and zone);
there is no tariffs catalog.** **A meter stores no reading of its own; readings are a periodic information register
written by "Meter reading" documents, and consumption is the difference between two readings.** **Debt to a provider is
a balance register: invoices increase it, payments decrease it; neither the provider nor the invoice stores it.**

## Reference data (catalogs)

- **Property**: name, address, area in square metres. (A household may have a flat and a country house.)
- **Provider**: name, kind (enumeration: electricity / gas / water / heating / internet / housing management / other),
  personal account number, phone.
- **Service** (hierarchical, groups such as Energy, Water, Fees): name, unit (kWh, m³, Gcal, month, m²), billing
  (enumeration: metered / fixed per month / per square metre), provider.
- **Meter**: property, service, serial number, zones (enumeration: single / day-night), next verification date.

Constants: the currency code shown on forms (UAH) and the day of the month by which readings are expected (default 25).

## Documents

All documents have a printable PDF form with the household letterhead, document number, date, details, and totals.

- **Set tariffs**: date; lines of (service, zone: single / day / night, price per unit).
- **Meter reading**: date, property; lines of (meter, zone, reading). A "Fill" button adds a line per meter and zone of
  the property, showing the previous reading next to each. A reading lower than the previous one refuses the post.
- **Invoice**: date, provider, property, month; lines of (service, meter, zone, quantity, price, amount). A "Calculate"
  button fills the lines: for a metered service the quantity is the consumption in the month (last reading in the month
  minus the last reading before it) and the price is the tariff valid on the last day of the month; for "fixed per
  month" the quantity is 1; for "per square metre" the quantity is the property's area. The user may edit any line
  before posting (a provider's invoice is what counts).
- **Payment**: date, provider, property, amount, comment. From an Invoice the user can create a Payment for its total,
  linked to it.

## Registers and posting

| Register | Kind | Written by | Meaning |
|---|---|---|---|
| Tariffs | periodic information | Set tariffs | price per service and zone valid from the document's date |
| Meter readings | periodic information | Meter reading | reading per meter and zone valid from the document's date |
| Settlements | accumulation, balances | Invoice (+total), Payment (−amount) | what the household owes per provider and property; a negative balance is a prepayment |
| Utility costs | accumulation, turnovers | Invoice | quantity and amount per property, service and month |

Posting rules: saving a **posted** document re-posts it with the new values; un-posting or deleting it takes its
entries back; a document can be deleted only when not posted. Registers are never edited by hand. Only one posted
Invoice per provider, property and month.

## Screens

1. **Home dashboard** (the home page; opens with its defaults and refreshes itself every five minutes; one filter, a
   service, default electricity): tiles for the total debt to all providers (lower is better), the invoices of the last
   month (against the same month a year before) and the number of meters missing this month's reading (lower is
   better); costs per month by service group for the last twelve months (stacked bar chart); the chosen service's
   consumption per month, this year and last year (line chart, two series); the debt or prepayment per provider
   (table); meters missing a reading or due for verification within 60 days (table).
2. **Provider statement** (a report): choose a provider, a property and a period; opening balance, invoices and payments
   as rows, closing balance.
3. **Consumption** (a report): choose a property and a year; services (and zones) as rows, months as columns, quantities
   in the cells, with totals and the change against the same month of the previous year.
4. **Costs** (a report): choose a period; amount per property and service group, with totals.
5. **Tariff history** (a report): choose a service; each price with the date it became valid and the change in percent.
6. Forms for the catalogs and documents above; quick-add entries for Meter reading and Payment in the navigation.

Reports are tables (no charts; charts belong on the dashboard) and export to CSV, Excel, and PDF.

## Reminders

A daily scheduled job sends the user an in-app notification on the expected-readings day if any meter of any property
has no reading this month, and when a meter's verification date is 30 days away.

## Non-functional

Local database, no cloud services. Quantities and amounts are decimals. UI languages: English and Ukrainian (labels as
resource strings in both English and Ukrainian resource files). Login: the framework's default.

## Seed data (the demo data of the project)

One flat with electricity (day-night meter), gas, cold and hot water meters and heating by area; five providers; tariffs
with one price change in the period; readings, invoices and payments for the last six months, all posted. One provider
is in debt, one has a prepayment, and this month's readings are missing for one meter.

## Acceptance checks (write each as an automated test)

1. Calculate on an Invoice takes the metered quantity from the readings and the price from the tariff valid at the end of
   the month.
2. A tariff change in the middle of the period applies only to the months after its date.
3. Posting an Invoice increases the debt to its provider; a Payment decreases it.
4. Changing a posted Invoice, un-posting it, or deleting it updates the debt and the costs correctly.
5. A reading lower than the meter's previous reading refuses the post.
6. A provider statement's closing balance equals the provider's debt on that date.
7. The dashboard's figures equal the reports: the total debt as the sum of the provider statements' closing balances,
   the chosen service's consumption as in Consumption for the same months.

## How to work

Work in stages and check each before the next: reference data, then registers, then documents (with their tests), then
reports, the dashboard, the reminder job, navigation, one database migration, demo data. At the end run the whole test suite, start the
application and open every form and report and the dashboard once in a browser. Do not ask questions unless something blocks you.

## Deliver

The running project with a short `README.md` (how to build, run, sign in) and the tests. Do not write other documents.
Finish with a reply of at most fifteen lines: what was built, how to run it, and any place where you had to deviate from
this brief and why.
