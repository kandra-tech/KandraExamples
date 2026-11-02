# Contributing

Each example has one application, kept as a reference implementation. Contributions are welcome from anyone: open an
issue or a pull request with a bug fix, an improvement, an upgrade to a newer Kandra Core, or a brief for a new example
(see [IDEAS.md](IDEAS.md)).

## Quality bar

An application is published, and every change to it is merged, only when both lists hold.

**It works**

- It builds without errors and with no new warnings.
- The whole test suite passes, including a test for every acceptance check in the brief.
- It starts, you can sign in, and its demo data loads.
- Every form, report and dashboard opens.

**No red flags in the code**

- No raw SQL against the engine's tables; registers are read through their readers.
- No user-facing text written in code (repositories, register data, notifications, print forms): it comes from resource
  files, in every language the brief asks for.
- No silent fallbacks: a missing exchange rate is not `1`, a missing currency is not a hardcoded `"UAH"`, no bare `catch`
  that returns a default.
- No code looks up reference data by its name, and nothing the code needs exists only in the demo data.
- Nothing hand-written where a generator owns the output (`Generated.Net/` is never edited).
- No secrets, local paths, `bin`/`obj`, databases or logs committed.

A fix comes with a test that fails without it, where a test can show the bug.

## Building a new example

1. Write the brief and agree on it in an issue first (shape and size: [IDEAS.md](IDEAS.md)).
2. Build the first version from the brief with a coding agent, starting from the configuration template.
3. Review it against the quality bar and the brief; check the agent's claims against the code, don't trust its report.
   Fix what the review finds and add the missing tests.
4. Put the solution in `<example>/app/` and write `<example>/BUILD-NOTES.md`.

## BUILD-NOTES.md

```markdown
# <Example> - build notes

| | |
|---|---|
| First version | e.g. Google Antigravity / Gemini 3 Pro, N min from the brief to the agent's report |
| Review and polish | e.g. Claude Code / Claude Opus 5.5 |
| Brief | <file> v<N> |
| Template / Kandra Core | 1.x.y / 0.x.y (first version) |
| Date | YYYY-MM-DD |
| Tests | N in the first version, N after the review |

## The first version
Two or three sentences: is the model right, what was the code like.

## Deviations from the brief
What the first version changed and why, checked against the code.

## Fixed in review
Every fix the review made, with the reason.

## Known limitations
What a reader should know: weak spots, known issues, anything to learn from.

## Since then
Upgrades and changes after the review, one line each.
```
