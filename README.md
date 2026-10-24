<p align="center">
  <img src="assets/banner.jpg" alt="Kandra Examples" width="100%">
</p>

**English** | [Українська](README.uk.md)

# Kandra Examples

Small, complete business applications built on the [Kandra](https://kandra.tech) engine by **coding agents**, each from a
single written brief. Every example holds the brief itself and one or more real runs by different agent harnesses
(Google Antigravity, Claude Code, Cursor, ...), published as the agent delivered them.

Use them to:

- **study** how a domain is modelled in Kandra's terms: catalogs (dictionaries), documents that post to registers,
  balance, turnover and periodic information registers, reports and dashboards;
- **compare** what different agents build from the same brief;
- **try it yourself**: give your agent an empty folder and a brief, and see how far it gets in an hour.

## Examples

| Example | What it shows | Brief |
|---|---|---|
| [Personal Accounting & Budgets](personal-accounting/) | balance register, turnover register in two currencies, exchange rates as a periodic register, a budget register bound to its document, statements, home dashboard | [PROMPT.md](personal-accounting/prompt/PROMPT.md) |
| [Family Garage](family-garage/) | service history with intervals by km or months, detailed or summary service invoices, purchase on a loan or leasing, total cost of ownership, reminders, home dashboard | [PROMPT.md](family-garage/prompt/PROMPT.md) |
| [Home Pantry](home-pantry/) (Grocy-inspired) | stock in batches with best-before dates, first-expired-first-out consumption, recipes scaled into a document, shopping lists; part B extends an existing configuration with chores, batteries and a meal plan | [part A](home-pantry/prompt/PROMPT-A-stock.md), [part B](home-pantry/prompt/PROMPT-B-household.md) |
| [Home Utilities & Meters](home-utilities/) | meter readings and tariffs valid from a date, invoices calculated from them, debt and prepayment per provider, year-over-year consumption, home dashboard | [PROMPT.md](home-utilities/prompt/PROMPT.md) |

More ideas, not built yet: [IDEAS.md](IDEAS.md).

## Layout

```
<example>/
  README.md        the domain, what it shows, the table of runs
  prompt/          the brief (more than one file when the example is built in steps)
  runs/
    <harness>/     one run per agent harness: the solution as delivered, plus RESULTS.md
```

A run folder is named after the harness only (`antigravity`, `claude-code`, `cursor`, ...). Its `RESULTS.md` records the
model, the template and Kandra Core versions, the date, the time taken, the test result, the review notes and anything
fixed after the run. A newer run with the same harness replaces the folder; the previous one stays in git history under a
tag (`<example>/<harness>/template-<version>`).

## Running a brief yourself

1. Install the .NET 10 SDK and follow [Getting started](https://docs.kandra.tech/getting-started) once.
2. Make an empty folder, open your coding agent in it, and give it the text of a brief.
3. The brief tells the agent to create the project from the configuration template, read its `AGENTS.md` and skills, and
   build, test and run the application.

The briefs ask for a home **dashboard**, which needs a configuration template built on a Kandra Core release that
contains dashboards (after 0.5.21).

## Running a published run

Each run is a standalone .NET solution pinned to the template and Kandra Core versions it was built with; its
`README.md` says how to build, run and sign in. The packages come from the public Kandra NuGet feed.

## Quality bar

A run is published only when it works (builds, its tests pass, it starts with its demo data, every form and report
opens) and its code has no red flags. The checklist is in [CONTRIBUTING.md](CONTRIBUTING.md), which also says how to
submit a run of your own.

## License

The briefs and the example code are under the [MIT License](LICENSE). The Kandra engine packages they use are not: they
are licensed under the Kandra End-User License Agreement. See [NOTICE.md](NOTICE.md).
