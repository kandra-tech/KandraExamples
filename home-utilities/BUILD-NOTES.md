# Home Utilities & Meters - build notes

The application in [app/](app/) was built in one Google Antigravity session: a first version from the brief, a few
rounds of improvements asked for during the session, then a review with a stronger model. This file says what each step
did, so you can tell what came from the brief and what from the later rounds.

| | |
|---|---|
| First version | Google Antigravity / Gemini 3.8 Flash (medium), from [PROMPT.md](prompt/PROMPT.md) v2 |
| Improvement rounds | Google Antigravity / Gemini 3.8 Flash (medium), requirements added in the session (now in the brief, v3) |
| Review and fixes | Google Antigravity / Gemini Argon 4 (high) |
| Brief | [PROMPT.md](prompt/PROMPT.md) v3 (v2 for the first version; v2 is in git history) |
| Template / Kandra Core | 1.3.7 / 0.5.27 |
| Date | 2026-11-01 |
| Session | about 5 h 40 min from scaffolding to the final report, all three steps together |
| Tests | 39 |

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

## Known limitations

To be filled from the adoption review.

## Since then

- 2026-11-02: adopted into Kandra Examples (ports changed, nothing else).
