---
name: create-constant
description: Add a new constant (a single, temporally-versioned scalar configuration value — VAT rate, home currency, company name, and similar) to a Kandra configuration. Use when the user asks to add a new setting, configuration value, or system-wide parameter that behaviors/reports need to read.
---

# Creating a constant

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

A constant is the simplest entity kind in the engine: a single scalar value, versioned by an effective instant
— the engine keeps every historical value in one shared physical table, and reads resolve "the value as of a
given date," not just "the current value" (see [`reference.md`](reference.md) for the exact storage/versioning
mechanism, the full read/write service contracts, a real JSON-shape bug worth knowing about, and two fully
worked examples). This recipe is written against **this repo's one reference configuration, `PersonalAccounting`** —
substitute your own configuration's root project name if you're working in a different one. Concrete,
currently-working examples: `HomeCurrencyConstant`/`DefaultPriceTypeConstant`
(`PersonalAccounting.Application/Constants/`) — one file each, nine lines of real code.

**Unlike every other entity kind, a constant needs only two small new files and nothing else.** No Dto, no
Behavior, no Validator, no EF migration, no resx keys, no nav entry, no Refit client, no Blazor page — the
admin UI, the API controller, the client service, the physical table, and the localization strings are all
**engine-level and already fully generic** (`Kandra.ClientLib.MudBlazor/Pages/Admin/Constants/*`,
`Kandra.Api.Lib/Controllers/V1/ConstantsController.cs`, `Sys_Constants` in `PlatformDbContext`) — they read
every `[KandraConstant]` class through `IConstantRegistry` and render generically off its declared value type.
The two files are the constant class itself and its centralized type-identity constant (same
`{Config}.Enums.PersonalAccountingTypeIds` convention Document/Dictionary entities use — see step 1). `reference.md`'s
"Hand-maintained files" table confirms this is a real audit, not an oversimplification.

## 1. Mint a type identity (`{Config}.Enums/PersonalAccountingTypeIds/PersonalAccountingTypeIds.{Name}.cs`)

Generate a fresh GUID and add it as its own new sibling file — **never edit `PersonalAccountingTypeIds.cs` itself**, same
convention Document/Dictionary entities follow for their own `TypeId` (see `create-dictionary` reference.md §6)
and for the same reason: it means adding a new constant never means editing a shared file, only adding one, so
two people adding two different constants in parallel never touch the same line:

```csharp
// {Config}.Enums/PersonalAccountingTypeIds/PersonalAccountingTypeIds.{Name}.cs — a whole new file, not an edit to PersonalAccountingTypeIds.cs
namespace {Config}.Enums;

public static partial class PersonalAccountingTypeIds
{
    public const string {Name} = "{FreshGuid}";
}
```

Never reuse an existing constant's value.

## 2. Declare the constant (`{Config}.Application/Constants/{Name}Constant.cs`)

```csharp
using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using {Config}.Enums;

namespace {Config}.Application.Constants;

[KandraConstant]
[Description("One or two sentences: what this constant controls and what reads/uses it.")]
public sealed class {Name}Constant() : Constant<{T}>({compiledDefault}), IConstant
{
    public static string Name => "{Name}";

    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.{Name});
}
```

Copy [`data/Constant.cs`](data/Constant.cs) as the starting point — `data/README.md` explains every
`{{Placeholder}}`. Key rules, all enforced at compile/build time (see `reference.md` §2 for what breaks and
why if you skip one):

- `{T}` is the value's CLR type (`string`, `decimal`, `int`, `bool`, `DateTime`, ...). The admin UI classifies
  it automatically into a `ConstantValueKind` (`String`/`Integer`/`Decimal`/`Boolean`/`DateTime`/`Other`) and
  picks a matching input control — nothing to configure for this.
- `{compiledDefault}` is the fallback `GetAsync<C>()` returns when nothing has ever been written to the
  database for this constant — never null, always this compiled value. Pick a real, safe default.
- Must be `sealed`, parameterless-constructible (the primary-constructor pattern above passing the default to
  the `Constant<T>` base), and implement `IConstant` **directly** (not left to an intermediate base) for the
  static `Name` property — a static abstract interface member can't be deferred through an abstract class.
- `Name` (the string) is the lookup key stored in the database and the label shown as-is (unlocalized, no
  `[Caption]` mechanism exists for constants) in the admin list — keep it stable once shipped.
- `[Description]` (BCL `System.ComponentModel`) is **mandatory** on the class — a constant has no Dto/`[Caption]`
  to fall back on, so this is the only thing the MCP server's `describe_entity_type`/`list_entity_types` tools
  can surface for it (`reference.md` §2).
- `TypeId` references the `PersonalAccountingTypeIds.{Name}` constant from step 1 — the generator resolves the reference via
  semantic constant-folding, not syntax pattern-matching on an inline literal (`reference.md` §2 has the
  mechanics), so this works exactly like an inline literal GUID would, just centralized. `TypeId` identifies
  the constant in the change-tracking log, since constant rows have no per-value entity `Id` of their own.
- The generator (`Kandra.Generators.Constants`) fails the build with a diagnostic (`reference.md` §2 has the
  exact codes) if `Name`/`TypeId` aren't resolvable as compile-time constants, or if the parameterless
  constructor is missing — you'll know immediately if something's off.

## 3. Nothing else to register by hand

`[KandraConstant]`-attributed classes in `{Config}.Application` are discovered by `Kandra.Generators.Constants`
(already wired as an analyzer on that project — this line doesn't change per-constant), which emits
`GeneratedConstantRegistry : IConstantRegistry` — already wired in `ConfigureServices.cs` via
`services.AddSingleton<IConstantRegistry, GeneratedConstantRegistry>()`. That registry is the single source
every other piece (the admin controller, the admin UI, `IConstantsService`/`IConstantsManageService`) reads
from — see `reference.md` §3-5 for exactly how each one uses it. No EF migration (every constant's value rows
share the one pre-existing `Sys_Constants` table — a new constant just means new rows keyed by `Name`, not a
new column or table), no controller, no Dto, no resx, no nav entry, no Refit client, no Blazor page.

## 4. Read it from a behavior, report, or anywhere else that needs it

```csharp
public class SomeBehavior(IConstantsService constants) : IDocumentBehavior<Some>
{
    public async ValueTask OnSubmitAsync(Some entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var value = await constants.GetAsync<{Name}Constant>(entity.Date, cancellationToken);
        // value.Value is the typed value, resolved as of entity.Date (or the compiled default if unset)
    }
}
```

`IConstantsService` is deliberately **ungated** (no permission check) and constructor-injectable anywhere — see
`RateImportBehavior` (`PersonalAccounting.Application/Documents/RateImport.cs`,
`constants.GetAsync<HomeCurrencyConstant>(entity.Date, cancellationToken)`) and `PriceListReportBehavior`
(`PersonalAccounting.Application/Reports/PriceListReport.cs`) for real posting-path/report-path usage. Prefer the typed
`GetAsync<C>(asOf, ct)` overload (compile-time-safe, resolves the compiled default automatically) over the
stringly-typed `GetAsync(name, ...)`/`GetAsync<T>(name, ...)` overloads unless you're writing generic
admin/reporting code that genuinely doesn't know the constant type ahead of time. `reference.md` §4 covers a
real JSON-shape gotcha (`GetAsync<T>(name, ...)` vs. `GetAsync<C>()`) worth reading before hand-rolling a
`SetAsync` call anywhere outside the admin UI.

## 5. Writing/managing a value — already has a real admin UI, no work needed here

Unlike the stale assumption this skill used to carry, **there already is an admin UI** — `Admin → Constants` in
the running app (`/admin/constants`, `ManageConstants` policy), engine-level
(`Kandra.ClientLib.MudBlazor/Pages/Admin/Constants/{List,ConstantEditDialog,ConstantHistoryDialog}.razor`), and
it picks up a brand-new `[KandraConstant]` class automatically the next time the app builds — nothing to wire.
It lists every registered constant (overridden or still on its compiled default), lets an admin add a new dated
override, edit the current row's value in place, view full history, and delete a historical row. Changing a
value programmatically (tests, seed data) goes through `IConstantsManageService` (extends `IConstantsService`,
every write method permission-gated) — `SetAsync<T>(name, value, date, ct)` upserts by exact effective instant.
`reference.md` §4-5 has the full write-side contract and the effective-instant design (why it's real
instant-precision, not day-truncated — see also the linked memory on multi-timezone "effective from" design if
you're ever asked to replicate this pattern elsewhere).

## 6. Verify

`dotnet build PersonalAccounting.slnx` (confirms the generator picked the new class up — no diagnostic, and
`GeneratedConstantRegistry.g.cs` under `PersonalAccounting.Application/Generated.Net/Kandra.Generators.Constants/...`
contains the new entry), `dotnet test PersonalAccounting.slnx` (see
`PersonalAccounting.Tests/Application/Constants/ConstantsServiceTypedGetTests.cs`,
`ConstantsManageServiceGetAllTests.cs`, `ConstantsManageServiceRoundTripTests.cs`, and
`PersonalAccounting.Tests/ChangeTracking/ConstantsManageServiceChangeEventTests.cs` for the existing coverage shape —
extend rather than duplicate), then confirm live: run the app, open `Admin → Constants`, confirm the new
constant appears with its compiled default and `Overridden` unset, add an override through the dialog, and
confirm a behavior/report reading it via `GetAsync<{Name}Constant>()` sees the new value.

## Company requisites and file-valued constants (print forms read them)

Print forms and the letterhead read the company's own details as **constants**, not from a settings table: the
`Company*Constant` family in `{Config}.Application/Constants/` (`CompanyName`, `CompanyTaxCode`, `CompanyAddress`,
`CompanyPhone`, `CompanyLogo`, and whatever else your letterhead carries). Each is read **as of the document's
date** (`constants.GetAsync<CompanyNameConstant>(document.Date, ct)`), so a reprint of an old document shows the
details it had then; the frame provider and the configuration's party service do the reading, a print form never
does. A new requisite (a VAT number, a website) is an ordinary string constant plus a field on the `CompanySettings`
data processor and a line in the frame provider; see `create-print-form` (the letterhead section).

A **file-valued constant** (the logo) is a blob reference: `Constant<Guid?>(null)`, as `CompanyLogoConstant`. Don't
let anyone edit it on the Constants admin page: it shows the raw JSON (`{"Value": "<guid>"}`), and a hand-typed id
points at a blob that expires or is purged. Edit it through a **data processor** (`CompanySettings`) that uploads the
file, validates it, makes it permanent and writes the constant; see `create-data-processor` ("A file field"). Say so
in the constant's `[Description]`, as `CompanyLogoConstant` does, because that text is what an AI agent reads.

## If something here doesn't match the engine

`reference.md` is a point-in-time snapshot of engine source, not a live view — if a build error contradicts a
signature it shows, the code wins. Re-verify by grepping the type name under
`src/KandraCore/Kandra.Application.Abstractions/Constants/`, `Kandra.Application/Services/Constants*.cs`,
`Kandra.Forms/Constants/`, `Kandra.Api.Lib/Controllers/V1/ConstantsController.cs`, or
`Kandra.Generators.Constants/`, then fix `reference.md`/`data/` so the next person doesn't hit the same
surprise. Note that `docs/kandra-architecture.md` §2.7's own example (`Get<VatRate>()`, "append-only, closes
the previous range") is aspirational/pre-implementation — it predates the actual shipped shape
(`GetAsync<C>()`, upsert-by-exact-instant, no range-closing) — trust this reference and the code over that
section.
