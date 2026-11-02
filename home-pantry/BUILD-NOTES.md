# Home Pantry - build notes

The application in [app/](app/) is built in two steps, as the brief is: part A (stock, recipes, shopping) first, then
part B (household), which extends the same configuration. This file records each step as it was done, so you can tell
what came from each brief and what from the reviews.

## Part A: stock, recipes, shopping

| | |
|---|---|
| First version | Google Antigravity / Gemini 3.8 Flash (medium), from [PROMPT-A-stock.md](prompt/PROMPT-A-stock.md) v1 |
| Review and fixes | Google Antigravity / Gemini Argon 4 (high) |
| Brief | [PROMPT-A-stock.md](prompt/PROMPT-A-stock.md) v1 |
| Template / Kandra Core | after 1.3.7 (the 0.5.35 pin) / 0.5.35 |
| Date | 2026-11-02 |
| Session | about 3 h 25 min from scaffolding to the review's final report, both steps together |
| Tests | 28, all passing after the review |

### The first version

The first version followed brief A: hierarchical Product and Location catalogs, Store and Recipe; Purchase, Consume
(first expired, first out), Transfer, Inventory count, Cook recipe and Shopping list documents; stock as a balance
register by product, location and best-before date, and the last price paid as a periodic information register; the
reports, the home dashboard and the demo data.

### Fixed in review

The review by Gemini Argon 4 listed its fixes; in short:

1. **Permissions**: the ordinary user role had no claims, so the demo users `user0` and `user1` could open nothing.
2. **Shopping list**: creating one from the shopping needs was never saved; a new list now starts from the current needs.
3. **Shopping list to Purchase**: the Purchase created from a list had no location, price or best-before date and no
   document link; it now takes the product's defaults and the last price, and links back to the list.
4. **Stock guard on Purchase**: editing or un-posting a posted purchase could leave a batch below zero after part of it
   was consumed; it is now refused, and the check sums the lines of the same batch.
5. **Balances as of the document date** in Consume, Transfer, Cook recipe and Inventory count (they read the latest
   balances).
6. **Inventory count**: duplicate product lines are refused, and the last price is taken from the latest period.
7. **Cook recipe**: servings were whole numbers in the database but fractional on the form; saving an empty Cook recipe
   now fills the scaled ingredients.
8. **Reports**: deleted catalog items and purchases are left out; a date-only upper bound includes its whole day;
   product groups roll up to the top-level group; a location folder in Stock overview includes its sub-locations; mixed
   units are no longer summed; units and the missing ingredients of "Can I cook it?" are localized.
9. **Forms**: the purchase total and line amounts are calculated and read-only; the dashboard tiles and charts drill down
   into their reports.
10. **Localization**: print form titles, navigation, claim, constant and register names were missing from the
    resources; English, Ukrainian and Russian now cover every key, and the localization tests check those sources too.

The review added a migration and three acceptance tests (checks 8 to 10).

### Deviations from the brief

- The agent's own fix report is not published (its content is summarised above).
- The launch ports were moved from the template's 7220/5220 to 7228/5228, so the examples can run side by side
  (Personal Accounting uses 7225, Family Garage 7226, Home Utilities 7227).

### Known limitations

- Part A is recorded as delivered by its session, without a separate review here; the review of the whole application
  follows part B.

## Part B: household

Not built yet.
