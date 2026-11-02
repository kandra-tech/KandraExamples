<p align="center">
  <img src="assets/banner.jpg" alt="Kandra Examples" width="100%">
</p>

**English** | [Українська](README.uk.md)

# Kandra Examples

Small, complete applications built on the [Kandra](https://kandra.tech) engine, one per domain. Each one is a reference
implementation you can study and build on, and a home application you can actually use.

Use them to:

- **study** how a domain is modelled in Kandra's terms: catalogs (dictionaries), documents that post to registers,
  balance, turnover and periodic information registers, reports and dashboards;
- **run** them at home with their demo data, or with your own;
- **start from** them: copy one and change it, or give your own coding agent the example's brief.

## Examples

| Example | What it shows | Status |
|---|---|---|
| [Personal Accounting & Budgets](personal-accounting/) | balance register, turnover register in two currencies, exchange rates as a periodic register, a budget register bound to its document, statements, home dashboard | [app](personal-accounting/app/) |
| [Family Garage](family-garage/) | service history with intervals by km or months, detailed or summary service invoices, purchase on a loan or leasing, total cost of ownership, reminders, home dashboard | [app](family-garage/app/) |
| [Home Pantry](home-pantry/) (Grocy-inspired) | stock in batches with best-before dates, first-expired-first-out consumption, recipes scaled into a document, shopping lists; part B extends an existing configuration with chores, batteries and a meal plan | [app](home-pantry/app/) (part A) |
| [Home Utilities & Meters](home-utilities/) | meter readings and tariffs valid from a date, invoices calculated from them, debt and prepayment per provider, year-over-year consumption, home dashboard | [app](home-utilities/app/) |

More ideas, not built yet: [IDEAS.md](IDEAS.md).

## Layout

```
<example>/
  README.md        the domain, what it shows, how it was built
  prompt/          the brief (more than one file when the example is built in steps)
  app/             the application: a standalone .NET solution with its own README
  BUILD-NOTES.md   how the application was built and reviewed, what was fixed, what is still weak
```

An example without `app/` has a brief and is not built yet.

## How they are built

Every application starts from its brief: a coding agent creates the project from the configuration template and builds
the first version. That version is then reviewed and polished, by people and by a second agent, until it meets the
quality bar below. Each example's `BUILD-NOTES.md` names the tools and models, the time the first version took and every
fix the review made, so you can see what came from the brief and what came from the review.

## Running an application

1. Install the .NET 10 SDK.
2. Open the example's `app/` folder and follow its `README.md`: it says how to build, start with the demo data and sign in.

Each application is pinned to the template and Kandra Core versions it was built with. The packages come from the public
Kandra NuGet feed.

## Running a brief yourself

1. Install the .NET 10 SDK and follow [Getting started](https://docs.kandra.tech/getting-started) once.
2. Make an empty folder, open your coding agent in it, and give it the text of a brief.
3. The brief tells the agent to create the project from the configuration template, read its `AGENTS.md` and skills, and
   build, test and run the application.

The briefs ask for a home **dashboard**, which needs a configuration template built on a Kandra Core release that
contains dashboards (after 0.5.21).

## Quality bar

An application is published only when it works (builds, its tests pass, it starts with its demo data, every form and
report opens) and its code has no red flags. The checklist is in [CONTRIBUTING.md](CONTRIBUTING.md), which also says how
to propose a fix or an improvement.

## License

The briefs and the example code are under the [MIT License](LICENSE). The Kandra engine packages they use are not: they
are licensed under the Kandra End-User License Agreement. See [NOTICE.md](NOTICE.md).
