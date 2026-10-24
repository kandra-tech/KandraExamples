# AGENTS.md — KandraExamples

Public repository of example configurations built on Kandra by coding agents, each from one written brief. It holds the
briefs and published agent runs; it is **not** a configuration itself and has no solution at the root. See
[README.md](README.md) for the layout and [CONTRIBUTING.md](CONTRIBUTING.md) for the publication checklist.

## Structure

- `<example>/README.md`: the domain, what it shows, the table of runs (update it when a run is added or replaced).
- `<example>/prompt/`: the brief(s). A brief is written as a user would write it: the domain, the modelling decisions in
  1C terms (catalogs, documents, registers, reports, dashboards), screens, seed data, acceptance checks. **No framework API
  names** (`OnNewAsync`, `[PivotRow]`, ...) in a brief; that knowledge belongs in the template's skills.
- `<example>/runs/<harness>/`: one folder per agent harness (`antigravity`, `claude-code`, `cursor`, ...), never per
  date. A newer run replaces the folder; tag the replaced one `<example>/<harness>/template-<version>` first.
- `IDEAS.md`: domains considered and not built, plus the rules for adding an example.
- `assets/`: images for the READMEs (the banner is 1600 px wide).

## Conventions

- A brief's version is in its first line (`build brief (vN)`); bump it on every change that would change what an agent
  builds, and note in the example's README which version each run used.
- A run is published as delivered. Hand fixes are allowed only when listed in its `RESULTS.md` (*Fixed after the run*).
  Every run must meet the quality bar in `CONTRIBUTING.md`: check it, don't trust the agent's own report.
- Never commit `bin/`, `obj/`, `Generated.Net/`, SQLite databases, logs or secrets from a run (`.gitignore` covers the
  usual ones; check `appsettings*.json` by hand).
- Every `README.md` (the root one and each example's) has a Ukrainian twin, `README.uk.md`, linked from the language
  line at the top of each: change both together, and link the Ukrainian pages to each other. Briefs and
  `CONTRIBUTING.md` stay English only (a brief is the exact text an agent gets).
- Licensing: content here is MIT (`LICENSE`); the Kandra packages the runs reference are under the Kandra EULA
  (`NOTICE.md`). Keep both statements when editing the README.
- The internal trial write-ups (what the run taught us about the template, skills and engine) live in the private
  monorepo under `docs/agent-trials/`; `RESULTS.md` is the public, edited summary.

## Working on a run

Each run is a standalone .NET solution pinned to its template and Kandra Core versions; build and test it from its own
folder (`dotnet build`, `dotnet test`), following that run's `README.md`.
