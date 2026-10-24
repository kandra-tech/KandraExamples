# Contributing a run

A run is one agent harness building one example from its brief, published as delivered. Runs are welcome from anyone:
open a pull request that adds `<example>/runs/<harness>/` with the solution and a `RESULTS.md`.

## Quality bar

A run is published only when both lists hold.

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

## When a run misses the bar

Prefer, in this order:

1. Fix the cause where it is (the brief, the template's skills, the engine) and run the agent again.
2. Make small fixes by hand and list every one in `RESULTS.md` under *Fixed after the run*, with the reason, so a reader
   still sees what the agent did on its own.

## RESULTS.md

```markdown
# <Example> - <harness>

| | |
|---|---|
| Harness / model | e.g. Google Antigravity / Gemini 3 Pro |
| Brief | <file> v<N> |
| Template / Kandra Core | 1.x.y / 0.x.y |
| Date | YYYY-MM-DD |
| Time to first delivery | N min (+ N min of follow-up prompts, if any) |
| Tests | N passed |

## Verdict
Two or three sentences: is the model right, what is the code like.

## Deviations from the brief
What the agent changed and why (its own words, checked against the code).

## Review notes
What a reader should know: weak spots, known issues, anything to learn from.

## Fixed after the run
Every hand fix, with the reason. "None" when there is none.

## Previous run
Tag of the run this one replaced, if any.
```

Summarise the agent's own report into `RESULTS.md` after checking its claims against the code; don't publish the
agent's report as is.
