# Personal Accounting & Budgets

**English** | [Українська](README.uk.md)

A single-user application to record personal money movements across accounts in several currencies, see balances, and compare monthly spending with budgets.

## What it shows

- An **accumulation register with balances** (account balances) and one **of turnovers** (category turnover, in the account currency and in the base currency).
- **Periodic information registers** fed by documents: exchange rates (a "Set exchange rates" document, no rates catalog) and the budget plan bound to its Budget document.
- Opening balances as a document, not a field on the account.
- Reports over register movements: transaction journal, budget vs. actual, spending by category, account statement.
- A home **dashboard**: balance tiles, income vs. expense chart, expenses by category.

## Brief

[prompt/PROMPT.md](prompt/PROMPT.md) (v4)

## Runs

| Harness | Model | Brief | Template / Core | Time | Tests |
|---|---|---|---|---|---|
| [Google Antigravity](runs/antigravity/) ([results](runs/antigravity/RESULTS.md)) | Gemini 3.8 Flash (medium) | v4 | 1.3.5 / 0.5.25 | ~65 min | 18 delivered, 21 after review fixes |

To add a run, see [CONTRIBUTING.md](../CONTRIBUTING.md).

![The home dashboard of the Antigravity run](runs/antigravity/screenshots/dashboard.en.jpg)
