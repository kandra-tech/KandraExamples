# AGENTS.md — KandraExamples

Public repository of example applications built on Kandra, one polished implementation per domain. Each is a reference
for configuration developers and a home application people can use. It holds the briefs, the applications and their build
notes; it is **not** a configuration itself and has no solution at the root. See [README.md](README.md) for the layout and
[CONTRIBUTING.md](CONTRIBUTING.md) for the quality bar.

## Structure

- `<example>/README.md`: the domain, what it shows, a short "how it was built" paragraph and a link to the app.
- `<example>/prompt/`: the brief(s). A brief is written as a user would write it: the domain, the modelling decisions in
  1C terms (catalogs, documents, registers, reports, dashboards), screens, seed data, acceptance checks. **No framework API
  names** (`OnNewAsync`, `[PivotRow]`, ...) in a brief; that knowledge belongs in the template's skills.
- `<example>/app/`: the application, a standalone .NET solution with its own `README.md` (build, run with the demo data,
  sign in). There is one per example; an example without `app/` is a brief that is not built yet.
- `<example>/BUILD-NOTES.md`: how the application was built (the tool and model of the first version and of the review,
  the brief version, template / Kandra Core versions, time of the first version), deviations from the brief, every fix
  the review made, and the known limitations.
- `IDEAS.md`: domains considered and not built, plus the rules for adding an example.
- `assets/`: images for the READMEs (the banner is 1600 px wide).

## Conventions

- The application is maintained, not frozen: fix bugs, keep it on current Kandra Core and template versions, follow the
  current conventions of the template's skills. It is the source of truth developers copy from, so it must not drift.
- `BUILD-NOTES.md` records the first version honestly: what the agent built from the brief, and every fix the review made
  after it, with the reason. Later maintenance (upgrades, new features) goes in its *Since then* section, briefly.
- Public text says how an application was built (initial version by an agent from the brief, then reviewed and polished);
  it never claims a one-shot result and never compares agents. Model names and versions belong in `BUILD-NOTES.md`, not in
  headlines.
- A brief's version is in its first line (`build brief (vN)`); bump it on every change that would change what an agent
  builds. `BUILD-NOTES.md` names the version the application was built from.
- Never commit `bin/`, `obj/`, `Generated.Net/`, SQLite databases, logs or secrets (`.gitignore` covers the usual ones;
  check `appsettings*.json` by hand).
- Every `README.md` (the root one and each example's) has a Ukrainian twin, `README.uk.md`, linked from the language
  line at the top of each: change both together, and link the Ukrainian pages to each other. Briefs, `BUILD-NOTES.md` and
  `CONTRIBUTING.md` stay English only (a brief is the exact text an agent gets).
- Licensing: content here is MIT (`LICENSE`); the Kandra packages the applications reference are under the Kandra EULA
  (`NOTICE.md`). Keep both statements when editing the README.
- The internal trial write-ups (what building an example taught us about the template, skills and engine) live in the
  private monorepo under `docs/agent-trials/`; `BUILD-NOTES.md` is the public, edited summary.

## Working on an application

Each `app/` is a standalone .NET solution pinned to its template and Kandra Core versions; build and test it from its own
folder (`dotnet build`, `dotnet test`), following its `README.md` and `AGENTS.md`. Every change must keep it at the quality
bar in `CONTRIBUTING.md`.
