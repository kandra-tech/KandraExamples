# Home Pantry, part A: stock, recipes, shopping - build brief (v1)

An application in the spirit of [Grocy](https://grocy.info/) ("ERP beyond your fridge"), built on Kandra. Part B
([PROMPT-B-household.md](PROMPT-B-household.md)) extends the same configuration later; build only part A now.

## First

1. Read the platform's getting-started page, `https://docs.kandra.tech/getting-started`, and follow it to create the
   project in this folder: configuration name `HomePantry`, SQLite only. Build and run the empty project once and sign
   in, so you know the starting point is healthy.
2. In the new project, read `AGENTS.md`, then the skill it names for building a whole application from a
   specification. The project's `AGENTS.md` and its skills folder are the rest of the documentation.

## What to build

A single-household, local application to know what food and household supplies are at home, where they are, when they
spoil, what can be cooked from them, and what to buy. Build exactly what is listed here. Anything not listed is out of
scope.

## How the application should be modelled

The framework uses the classic 1C:Enterprise-style metadata model; model the domain in its terms rather than as tables
and screens:

- **Catalogs (dictionaries)** hold the *things* people refer to.
- **Documents** record *events* on a date. A document **posts** to registers when it is posted and takes its postings
  back when it is un-posted or changed.
- **Registers** hold everything derived. An **accumulation register with balances** keeps running totals; a **periodic
  information register** keeps a value that is valid from a date (the last price paid). Registers are never edited by
  hand and have no form of their own: users change them only by posting documents.
- **Reports** read registers (not the documents' tables) and compute their rows on request.
- A **dashboard** is its own kind, not a report: a page of figures and charts computed from the registers, opened
  without filling anything in and refreshed on a timer.
- Grouping inside a catalog (a product under a product group, a fridge inside the kitchen) is the catalog's own hierarchy
  of groups and items, not a "parent" field.
- Fixed lists of choices are enumerations; system-wide settings are constants.

Where this brief says "stock", "on hand" or "last price", that is a register. In particular: **a product stores no
quantity of its own; stock is a balance register by product, location and best-before date (a batch), and every
movement is a document.** **A recipe is a catalog item with ingredient lines; cooking it is a document.**

## Reference data (catalogs)

- **Product** (hierarchical, groups such as Dairy, Vegetables, Cleaning): name, unit (enumeration: pcs, g, kg, ml, l,
  pack), barcode, minimum stock, default location, default shelf life in days (empty for products that do not spoil).
  Product selectors find a product by its name **or its barcode**.
- **Location** (hierarchical: Kitchen > Fridge, Freezer, Shelf; Bathroom; Storage room): name, is freezer (yes / no).
- **Store**: name.
- **Recipe**: name, servings, instructions (text), lines of (product, quantity for the stated servings).

Constants: the currency code shown on forms (UAH) and the number of days ahead that "Expiring soon" looks (default 5).

## Documents

All documents except Consume have a printable PDF form with the household letterhead, document number, date, details,
and totals.

- **Purchase**: date, store; lines of (product, quantity, price, amount, location, best-before date). A new line takes the
  product's default location, and its best-before date is the document date plus the product's shelf life.
- **Consume**: date, reason (eaten / spoiled / used up / other); lines of (product, location, quantity). The user does not
  choose batches: posting takes the quantity from the batches of that product in that location **with the earliest
  best-before date first**. Consuming more than is on hand refuses the post.
- **Transfer**: date; lines of (product, from location, to location, quantity). Batches move with their best-before date,
  earliest first.
- **Inventory count**: date, location; lines of (product, counted quantity). Posting writes the difference from the
  balance on the document's date: a surplus as a new batch, a shortage earliest batch first.
- **Cook recipe**: date, recipe, servings, location. A "Fill" button fills the lines (product, quantity) from the recipe,
  scaled from the recipe's servings to the document's; the user may edit them before posting. Posts like Consume.
- **Shopping list**: date, store; lines of (product, quantity, bought yes / no). It does not post. From a Shopping list the
  user can create a Purchase for its lines that are not marked bought, linked to it.

## Registers and posting

| Register | Kind | Written by | Meaning |
|---|---|---|---|
| Stock | accumulation, balances | Purchase (in), Consume (out), Transfer (out and in), Inventory count (in or out), Cook recipe (out) | quantity and value per product, location and best-before date |
| Last price | periodic information | Purchase | price per product and store valid from the document's date |

Value leaves a batch at the batch's average price. Posting rules: saving a **posted** document re-posts it with the new
values; un-posting or deleting it takes its entries back; a document can be deleted only when not posted. Stock may
never go negative.

## Screens

1. **Pantry dashboard** (the home page; opens without parameters and refreshes itself every minute): tiles for the
   value of the stock, the number of products below their minimum (lower is better), the number of batches expiring
   within the look-ahead period (lower is better) and this month's spending (against last month); spending per month
   for the last six months (bar chart); stock value by product group (donut, the five largest and the rest as "Other");
   the next ten batches to expire with their location and days left (table).
2. **Stock overview** (a report, no required parameters): per product the quantity on hand, the value, the nearest
   best-before date and a below-minimum flag; filterable by location and product group.
3. **Expiring** (a report, no required parameters): every batch already expired or expiring within the look-ahead
   constant, with its location and days left.
4. **Shopping needs** (a report): optionally choose a recipe and servings; per product the shortfall to the minimum stock,
   plus what the recipe needs beyond what is on hand, with the last price and the estimated cost. A "Create shopping
   list" button turns the rows into a Shopping list document.
5. **Can I cook it?** (a report, no required parameters): per recipe whether every ingredient is on hand for its stated
   servings, and the missing ingredients.
6. **Spending** (a report): choose a period; purchased amount per product group and store.
7. Forms for the catalogs and documents above; quick-add entries for Purchase and Consume in the navigation.

Reports are tables (no charts; charts belong on the dashboard) and export to CSV, Excel, and PDF.

## Non-functional

Local database, no cloud services. Quantities and amounts are decimals. UI languages: English and Ukrainian (labels as
resource strings in both English and Ukrainian resource files). Login: the framework's default.

## Seed data (the demo data of the project)

About 40 products in groups (with barcodes for most), the locations above, three stores, five recipes, about 20 posted
documents over the last month (purchases, consumes, one transfer to the freezer, one inventory count, two cooked
recipes) and one open shopping list. At least two batches are expired or expiring soon and three products are below
their minimum.

## Acceptance checks (write each as an automated test)

1. A Purchase adds a batch per line with its best-before date and records the last price.
2. Consume takes the batch with the earliest best-before date first and splits across batches when needed.
3. Consuming more than is on hand refuses the post and leaves stock unchanged.
4. Cook recipe consumes the recipe's ingredients scaled to the document's servings.
5. Changing a posted Purchase, un-posting it, or deleting it updates stock correctly; a Purchase whose stock was
   already consumed cannot be un-posted.
6. Shopping needs for a product equals its minimum stock minus the quantity on hand, when that is positive.
7. The dashboard's figures equal the reports: the below-minimum count as the flagged rows of Stock overview, the
   expiring count as the rows of Expiring, this month's spending as Spending for the month.

## How to work

Work in stages and check each before the next: reference data, then registers, then documents (with their tests), then
reports, the dashboard, navigation, one database migration, demo data. At the end run the whole test suite, start the application and
open every form and report and the dashboard once in a browser. Do not ask questions unless something blocks you.

## Deliver

The running project with a short `README.md` (how to build, run, sign in) and the tests. Do not write other documents.
Finish with a reply of at most fifteen lines: what was built, how to run it, and any place where you had to deviate from
this brief and why.
