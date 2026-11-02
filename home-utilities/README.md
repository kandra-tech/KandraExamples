# Home Utilities & Meters

**English** | [Українська](README.uk.md)

The household's utility bills: meter readings, tariffs that change over time, monthly invoices from each provider, payments, what is owed to whom, and how consumption changes month to month.

## What it shows

- Tariffs and meter readings as **periodic information registers**; consumption as the difference between two readings.
- Invoice lines **calculated** from readings, tariffs and the property's area, editable before posting.
- Debt and prepayment per provider as a **balance register**; a provider statement.
- Ukrainian **payment details** (IBAN, EDRPOU) and a payment purpose filled from the provider's template, on the invoice, the payment and their print forms.
- A year-over-year consumption pivot, costs by service group, tariff history.
- Daily **reminders** for missing readings and meter verification, and a home **dashboard** with a filter.

## Brief

[prompt/PROMPT.md](prompt/PROMPT.md) (v3)

## How it was built

The first version was built from the brief by Google Antigravity (Gemini 3.8 Flash, medium). A few rounds of
improvements with the same model followed in the same session (Ukrainian payment details and payment purpose templates,
print forms, per-person billing, demo data in two languages), and the agent wrote them back into the brief. Then it was
reviewed and fixed by Gemini Argon 4 (high), and reviewed again with Claude Code when it was adopted here (six more
fixes). What each step did: [BUILD-NOTES.md](BUILD-NOTES.md).

## Run it

The application is in [app/](app/); its [README](app/README.md) says how to build, start with the demo data and sign in.
