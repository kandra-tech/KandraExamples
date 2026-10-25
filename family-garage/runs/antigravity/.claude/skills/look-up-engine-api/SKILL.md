---
name: look-up-engine-api
description: Find the exact signature, generic arguments, DI registration, HTTP route or localization key of something the Kandra engine provides, when a skill does not show it or a build error contradicts it. Use before guessing generics, before reading DLLs with scripts, and whenever "No overload", "Unable to resolve service" or a raw key on screen sends you looking.
---

# Looking up the engine's API

The engine comes as NuGet packages, without source. Everything you need is still on disk or one command away. Look in
this order and stop at the first answer; **do not reverse-engineer the binaries** (string-table scans, decompiling): it
is slow and reads internals that change between versions.

## 1. What your own build generated (`Generated.Net/`)

The generators write their output into each project's `Generated.Net/` folder. It is the most exact answer, because it is
what your configuration actually runs:

| Question | Look at |
|---|---|
| Which service type is registered for my dictionary/document/report (and its exact generics)? | `{Config}.Application/Generated.Net/.../Generated{Config}ApplicationDiExtensions.g.cs` |
| Which register writer/reader types exist for my registers? | `{Config}.Persistence/Generated.Net/.../Generated{Config}PersistenceDiExtensions.g.cs` (and `create-register` section 8a) |
| What is the HTTP route and body of an endpoint? | `{Config}.WebApi/Generated.Net/.../{Plural}Controller.g.cs`, or `/swagger` while the app runs |
| Which resx keys does a page ask for? | `{Config}.Client.MudBlazor/Generated.Net/.../*.g.cs` (`Loc["..."]`), or run `LocalizationKeysTests` |

## 2. The XML docs inside the packages

Every `Kandra.*` package ships its XML documentation next to the DLL:
`~/.nuget/packages/kandra.<package>/<version>/lib/net10.0/Kandra.<Package>.xml` (Windows: `%USERPROFILE%\.nuget\packages\...`;
the version is `KandraCoreVersion` in `Directory.Build.props`). Search it by member name:

```bash
grep -n "ITurnoverReader" ~/.nuget/packages/kandra.persistence.abstractions/0.5.18/lib/net10.0/Kandra.Persistence.Abstractions.xml
grep -n "GetDictionaryAsync" ~/.nuget/packages/kandra.application.abstractions/0.5.18/lib/net10.0/*.xml
```

A member id like `M:Kandra.Persistence.Abstractions.Registers.ITurnoverReader`1.Movements(System.DateTime,System.DateTime,...)`
gives the parameter types; the `<summary>`/`<param>`/`<returns>` text explains them. Which package holds a type: the
namespace's first two segments (`Kandra.Persistence.Abstractions.*` lives in `kandra.persistence.abstractions`).

## 3. The documentation site

`https://docs.kandra.tech/reference/` has a hand-written overview per area (attributes, entities, register engine,
services, generators) and pages generated from the same XML docs, with cross-links.

## 4. The compiler

Let the compiler state the type: write the call, build, and read the error. "No overload takes 1 argument" lists nothing
useful, but assigning the result to a deliberately wrong type (`int x = await lookup.GetDictionaryAsync<Account>(ids, ct);`)
prints the real return type. Use it to confirm, not as the first step.

## 5. Localization keys of the platform

Whether a key exists in the platform's own resources is answered by `LocalizationKeysTests`: it resolves every key
through the same module chain as the running app (platform first, then yours) and lists what is missing. A platform key
that is missing (a generated button showing its raw key) is fixed by defining it in your resx; see
`localize-configuration` section 4.

## When nothing answers

Say so in your report and work around it in your own code. Do not patch generated files, do not copy engine internals,
and do not depend on something you could only find by scanning a binary.
