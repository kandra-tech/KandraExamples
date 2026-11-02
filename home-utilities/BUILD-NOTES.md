# Home Utilities & Meters - build notes

The application in [app/](app/) was built in one Google Antigravity session: a first version from the brief, a few
rounds of improvements asked for during the session, then a review with a stronger model. A second review, with Claude
Code, followed when it was adopted into Kandra Examples. This file says what each step
did, so you can tell what came from the brief and what from the later rounds.

| | |
|---|---|
| First version | Google Antigravity / Gemini 3.8 Flash (medium), from [PROMPT.md](prompt/PROMPT.md) v2 |
| Improvement rounds | Google Antigravity / Gemini 3.8 Flash (medium), requirements added in the session (now in the brief, v3) |
| Review and fixes | Google Antigravity / Gemini Argon 4 (high) |
| Adoption review | Claude Code / Claude Opus |
| Brief | [PROMPT.md](prompt/PROMPT.md) v3 (v2 for the first version; v2 is in git history) |
| Template / Kandra Core | 1.3.7 / 0.5.27 |
| Date | 2026-11-01 |
| Session | about 5 h 40 min from scaffolding to the final report, all three steps together |
| Tests | 39 after the Antigravity session, 44 after the adoption review |

## The first version and the improvement rounds

The first version followed brief v2: properties, providers, a hierarchical service catalog and meters; Set tariffs,
Meter reading (with Fill), Invoice (with Calculate) and Payment documents; tariffs and readings as periodic information
registers, debt as a balance register, costs as a turnover register; the reports, the home dashboard and the reminder
job.

The improvement rounds then added what a household in Ukraine actually needs to pay a bill, and the agent wrote those
requirements back into the brief (v3):

- the provider's payment details (IBAN, EDRPOU code, bank name, legal address) and a payment purpose template with
  placeholders, filled on the Invoice by Calculate, copied to the Payment created from it, and filled on save when empty;
- print forms of Invoice and Payment with those details and the filled payment purpose;
- the property's owner and number of registered residents, and billing per person (garbage removal);
- a Meter readings history report, and reports that fill in the property when there is only one;
- demo data in Ukrainian (the default) and in English, with a provider per service (building maintenance, garbage,
  elevator and intercom fees included).

## Deviations from the brief

- The agent's own handoff guide and implementation report are not published (their useful content is in this file and
  in the app's `README.md`; the report's notes about the template and skills went back into the template).
- The launch ports were moved from the template's 7220/5220 to 7227/5227, so the examples can run side by side
  (Personal Accounting uses 7225, Family Garage 7226).

## Fixed in review

The review by Gemini Argon 4 was done in the same session; its fixes were not listed one by one.

The adoption review (Claude Code) found six more, all fixed. The five testable ones are in
`Acceptance/ReviewFixesTests.cs`; each test was checked to fail with its fix taken out.

1. **The billing month moved back a month for a user west of UTC.** The invoice's month was stored as midnight UTC of
   the 1st and converted through the user's time zone again on every save and post, so in New York a November invoice
   was booked, and its costs posted, as October. The invoice now keeps the instant its month starts in the user's time
   zone (it survives the client's UTC conversion, and normalising it again changes nothing); the register key, the
   one-invoice-per-month check and the print form derive the month from it. The dashboard's twelve-month chart now
   buckets costs by that key too. The other tests run in UTC or Kyiv, east of UTC, where this never shows.
2. **Saving replaced an amount typed from the provider's invoice** with quantity × price, although the brief says the
   provider's invoice is what counts. A typed amount now stays; only an empty one is computed. Editing an amount on the
   form updates the total.
3. **Calculate filled a line with price 0 when no tariff was valid** at the end of the month. It now refuses, naming
   the services (and zones) without a tariff.
4. **Text in code**: with no template on the provider, the payment purpose was hard-coded Ukrainian; the meter readings
   in it used the word "Meter" and English zone names; a Payment created from an Invoice got the English comment
   "Payment for Invoice ...". All of it now comes from the resources in both languages.
5. **A backdated meter reading could be higher than a later one** (only the previous reading was checked), which turned
   the later reading into a drop and its month's consumption into 0. Posting it is now refused.
6. Smaller: the print forms fell back to a hard-coded "UAH" instead of reading the currency constant.

## Known limitations

- Month names on print forms and in the payment purpose follow the language of the request, not of the property's
  owner.
- A Payment created from an Invoice copies the payment purpose into its comment as well.
- The provider statement run without a period dates its opening and closing rows with the server's date.
- The build shows NuGet audit warnings for packages pinned by the configuration template, not by this application.
- The fixes were checked by tests, not yet by opening the forms in a browser.

## Since then

- 2026-11-02: adopted into Kandra Examples (ports 7227/5227) and the adoption review's fixes above.
