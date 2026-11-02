# Constant engine — reference

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

Self-contained explanation of every engine type/service `SKILL.md` walks through — written so you don't need to
go read `src/KandraCore/*` source to use it. Exact signatures as of the last time this was verified against the
engine (2026-09); if a build error contradicts something here, the code wins (see "If this reference goes
stale" at the bottom). A ready-to-copy `.cs` skeleton matching everything below lives in
[`data/Constant.cs`](data/Constant.cs).

## What a Constant actually is

A single, temporally-versioned scalar configuration value — VAT rate, home currency, default price type, and
similar system-wide parameters a behavior or report needs to read. **Not** built like Document/Dictionary/
Report/DataProcessor at all: there's no Dto/Behavior/Validator split, no per-entity table, no generated
controller. One `Constant<T>`-derived class *is* the whole entity declaration (plus its centralized `TypeId`
constant, §2) — the engine reads its compiled default, its `Name`, and its `TypeId` straight off the class via
a source generator, and every constant's *values* (current + full history) live as rows in one shared table,
`Sys_Constants` (`Kandra.Persistence/Contexts/PlatformDbContext.cs`), keyed by `(Name, Date)`.

```csharp
modelBuilder.Entity<ConstantRecord>(e =>
{
    // Engine-created infrastructure table, not domain data - same category as the
    // numbering tables, hence the shared Sys_ prefix.
    e.ToTable("Sys_Constants");
    ...
});
```

Because this table is part of the engine's own base schema (mapped once, engine-level, never per-configuration),
**adding a new constant never needs an EF migration** — a new constant is new rows in an already-existing
table, not a new column or table. This is the single biggest structural difference from every other entity kind
in the engine, and the reason this skill has no migration step.

## 1. Storage/versioning mechanism — how "temporally-versioned" is actually implemented

`Kandra.Domain/Constants/ConstantRecord.cs`:

```csharp
public class ConstantRecord : ChangesInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public string ValueJson { get; set; }
}
```

Deliberately a plain flat entity — no dimensions/details owned-type composition (the register engine's shape
would be overkill here). Inherits audit stamps, soft-delete (`IsDeleted`), and the optimistic-concurrency
`Version` column from `ChangesInfo`, same as every other engine entity.

**"Temporally-versioned" = effective-dated rows, one per historical value, matched by exact instant — not a
`ValidFrom`/`ValidTo` range pair.** A read resolves "the latest row whose `Date` is `<=` the asked-for instant":

```csharp
// ConstantsService.GetAsync
var at = asOf ?? DateTime.UtcNow;
var row = await dbContext.Set<ConstantRecord>()
    .Where(c => c.Name == name && c.Date <= at && !c.IsDeleted)
    .OrderByDescending(c => c.Date)
    .FirstOrDefaultAsync(cancellationToken);
```

No row at all (nothing ever written, or every row is dated after `at`) → falls back to the class's own compiled
default. A write (`ConstantsManageService.SetInternalAsync`) upserts by **exact** `(Name, Date)` match — sending
the same instant back again corrects that row in place instead of creating a duplicate history entry; a
different instant always creates a new, independently-dated row. There is no "close the previous range"
step and no separate append-only/undo mechanism — `docs/kandra-architecture.md` §2.7's own illustrative example
("value rows are append-only... a change inserts a new row closing the previous range") describes a design that
was **not** what shipped; trust this document and the code over that section.

### Instant precision, not day-truncation — and why

Early in this feature's life, `Date` was truncated to a calendar day both on write and on read. That was
deliberately removed: **day-granularity plus "treat the picked date as UTC midnight" is wrong by exactly the
picking admin's own UTC offset** — a Kyiv admin picking "today" would see the value apply ~3 hours late from
their own perspective, and in a system with users across time zones there is no single instant that is
simultaneously everyone's local midnight (this is the same class of problem covered by the timezone-handling
design in `src/KandraCore/CLAUDE.md`'s "Time zone handling" section, and by the linked memory below). So `Date` on
every `ConstantRecord` is a genuine UTC instant, and every read/write path compares it exactly as such — no
`.Date` truncation anywhere in `ConstantsService`/`ConstantsManageService`.

What the admin UI does about it (nothing a constant author needs to build, just useful context if you're ever
asked to replicate the pattern elsewhere): the edit dialog computes the *editing admin's own* local-midnight
instant via `DateTimeOffset.Now.Offset` (never a hardcoded/guessed offset), defaults to that with a "use my
local midnight" toggle, and always previews both sides ("Applies from Sep 6, 5:00 PM (UTC+3) = Sep 6, 2:00 PM
UTC") so nothing is silently ambiguous. See the `feedback_multitimezone_effective_instant_design` memory for
the full reasoning — it generalizes past Constants to any future "effective from" feature.

## 2. Declaring a constant — `Constant<T>`/`IConstant`/`[KandraConstant]`

`Kandra.Application.Abstractions/Constants/Constant.cs`:

```csharp
public interface IConstant<T>
{
    T Value { get; }
}

public abstract class Constant<T>(T value) : IConstant<T>
{
    public T Value { get; set; } = value;
}
```

`Kandra.Application.Abstractions/Constants/IConstant.cs`:

```csharp
public interface IConstant
{
    static abstract string Name { get; }
}
```

**Why the split**: a static abstract interface member (`IConstant.Name`) can't be left unimplemented by an
intermediate abstract class the way an instance member can — the compiler requires a concrete provider the
moment the interface is implemented, and an abstract class can't declare an abstract *static*. So `Constant<T>`
only carries `Value` (an ordinary instance member, fine on an abstract base) via `IConstant<T>`; every concrete
leaf class implements the non-generic `IConstant` **directly**, for `Name`. `TypeId` isn't part of either
interface at all — it's a bare `static Guid TypeId { get; }` the generator reads by convention (same shape as
`IEntityWithTypeId.TypeId` on Document/Dictionary entities), not an interface member, so there's no
compiler-level requirement for it — only the generator's own diagnostics enforce its presence.

The real, current worked examples (`PersonalAccounting.Application/Constants/`):

```csharp
[KandraConstant]
[Description("The ISO 4217 code of the company's home/base currency (e.g. \"UAH\") - RateImport rates and " +
    "the ExchangeRatesReport convert every other Currency to this one. Default \"UAH\" unless overridden.")]
public sealed class HomeCurrencyConstant() : Constant<string>("UAH"), IConstant
{
    public static string Name => "HomeCurrency";
    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.HomeCurrency);
}

[KandraConstant]
[Description("The PriceType Code used as the default price list when a new Waybill/Invoice is created " +
    "without one explicitly chosen. Default \"RETAIL\" unless overridden.")]
public sealed class DefaultPriceTypeConstant() : Constant<string>("RETAIL"), IConstant
{
    public static string Name => "DefaultPriceType";
    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.DefaultPriceType);
}
```

`[KandraConstant]` (`Kandra.Attributes.Constants.KandraConstantAttribute`) is a pure marker — never reflected on
at runtime, purely a discovery target for `Kandra.Generators.Constants`.

**`[Description]` (BCL `System.ComponentModel`) is mandatory on every constant class.** A constant has no
Dto/`[Caption]` to fall back on — this attribute, placed directly on the `Constant<T>`-derived class, is the
*only* semantic explanation the MCP server's `describe_entity_type`/`list_entity_types` tools can
surface for it (`Kandra.Mcp`'s `EntityFieldMetadataBuilder.ResolveDescription` reflects it off
`ConstantRegistryEntry.ConstantType`, i.e. this exact class). `HomeCurrencyConstant`'s real content —
`"The ISO 4217 code of the company's home/base currency (e.g. \"UAH\") - RateImport rates and the
ExchangeRatesReport convert every other Currency to this one. Default \"UAH\" unless overridden."` — is a good
model for how much detail is useful.

### `TypeId` is centralized through `PersonalAccountingTypeIds`, same as Document/Dictionary entities

Document/Dictionary entities funnel their `TypeId` through a centralized, `partial`-per-file
`{Config}.Enums.PersonalAccountingTypeIds` class (see `create-dictionary`/`create-document` — deliberately so two people adding
two different entities never touch the same file). **Constants follow the exact same convention**: `TypeId`
references a `PersonalAccountingTypeIds.{Name}` const string, declared in its own new sibling file
(`{Config}.Enums/PersonalAccountingTypeIds/PersonalAccountingTypeIds.{Name}.cs`), never as an edit to `PersonalAccountingTypeIds.cs` itself. This wasn't
always the case — constants originally declared `TypeId` as an inline string literal directly on the class
(`new("01d46587-...")`), reasoned at the time to be unnecessary to centralize since a constant's `TypeId` has
only one consumer (the generator that discovers the constant class itself), unlike a Dictionary's `TypeId`,
which the Domain-entity and Forms-Dto planes both need to agree on independently. That reasoning held up
technically but broke configuration-wide consistency — every other entity kind centralizes its `TypeId`, so
constants now do too, for the same "two people never touch the same file" reason and so the convention doesn't
have an unexplained exception.

Making this work required a generator change (see below) — the original `ConstantDiscovery.TryGetLiteralTypeId`
only accepted an inline string-literal syntax node (`new("<guid>")`), which would have rejected `new(PersonalAccountingTypeIds.HomeCurrency)`
outright. Generate a **fresh** GUID for the `PersonalAccountingTypeIds.{Name}` constant and never reuse one.

### The generator: `Kandra.Generators.Constants`

Deliberately simpler than the Api/ClientLib generators' `FormDiscovery`: those walk every referenced assembly
because `Kandra*Form`-attributed Dtos live in a project the generator's *host* project only references (e.g.
`PersonalAccounting.WebApi` referencing `PersonalAccounting.Forms`). `[KandraConstant]`-attributed classes live in the very
project that references this generator as an analyzer (`PersonalAccounting.Application` itself), so the standard
`SyntaxProvider.ForAttributeWithMetadataName` fast path is sufficient — no cross-assembly walk needed
(`ConstantDiscovery.cs`'s own doc comment explains this).

`ConstantDiscovery.Transform` reads:

- The class implements `IConstant<T>` (for `T`/`ValueType`) **and** the non-generic `IConstant` directly.
- A public parameterless constructor exists — else `MissingParameterlessConstructor` diagnostic.
- `Name` is a `static string` property with a literal string expression body (`=> "..."`) — read purely from
  syntax (`LiteralExpressionSyntax`), so `Name` must be an inline literal, not a const reference — else
  `MissingOrInvalidNameProperty`.
- `TypeId` is a `static Guid` property initialized as `new(<expr>)` where `<expr>` resolves to a Guid-parseable
  string via the **semantic model's constant folding** (`SemanticModel.GetConstantValue(expr)`) — else
  `MissingOrInvalidTypeIdProperty`. This is what makes `new(PersonalAccountingTypeIds.HomeCurrency)` work exactly like an
  inline literal `new("01d46587-...")` would: `GetConstantValue` resolves a reference to a `const string` field
  (like `PersonalAccountingTypeIds.HomeCurrency`) to its compile-time value the same way it resolves a literal, so the
  discovery code doesn't need to special-case which shape the argument takes. (Contrast with `Name` above,
  still syntax-literal-only — nothing needed it to support a const reference, so it wasn't changed.) The
  property's syntax is always found in the same syntax tree the semantic model is bound to, because
  `Constant<T>`-derived classes are `sealed` (never `partial`) — `TypeId` can't be declared in a different file
  from the class itself.
- No two constants share the same `Name` — else `DuplicateName` on every colliding class.

Any failure is a build-time diagnostic (`KANDRACONST0xx`), not a runtime surprise — you'll see it immediately.
On success, `ConstantRegistryEmitter` emits one file, `GeneratedConstantRegistry.g.cs`, landing under
`PersonalAccounting.Application/Generated.Net/Kandra.Generators.Constants/...`:

```csharp
public sealed class GeneratedConstantRegistry : IConstantRegistry
{
    public IReadOnlyList<ConstantRegistryEntry> Entries { get; } = new[]
    {
        new ConstantRegistryEntry("HomeCurrency", new Guid("01d46587-..."), typeof(HomeCurrencyConstant), typeof(string), new HomeCurrencyConstant()),
        new ConstantRegistryEntry("DefaultPriceType", new Guid("9c038a10-..."), typeof(DefaultPriceTypeConstant), typeof(string), new DefaultPriceTypeConstant()),
        // ...
    };
    public IReadOnlyDictionary<string, ConstantRegistryEntry> ByName { get; }  // keyed by Name
    public IReadOnlyDictionary<Type, ConstantRegistryEntry> ByType { get; }    // keyed by the concrete Constant<T> type
}
```

`ConstantRegistryEntry` (`Kandra.Application.Abstractions/Constants/ConstantRegistryEntry.cs`):

```csharp
public sealed record ConstantRegistryEntry(string Name, Guid TypeId, Type ConstantType, Type ValueType, IConstant DefaultInstance);
```

This is the **one and only place** every downstream piece (read service, write service, admin controller, admin
UI) learns what constants exist — see §3-5. `ConfigureServices.cs` already wires
`services.AddSingleton<IConstantRegistry, GeneratedConstantRegistry>()` once; that line never changes
per-constant.

## 3. Reading: `IConstantsService`

`Kandra.Application.Abstractions/Services/IConstantsService.cs`:

```csharp
public interface IConstantsService
{
    Task<string?> GetAsync(string name, DateTime? asOf = null, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string name, DateTime? asOf = null, CancellationToken cancellationToken = default);
    Task<C> GetAsync<C>(DateTime? asOf = null, CancellationToken cancellationToken = default) where C : IConstant, new();
}
```

Implementation (`ConstantsService`, `Kandra.Application/Services/`) is a thin, **ungated** (no permission check
— any behavior/report can constructor-inject it freely) wrapper over the raw `DbContext`:

- `GetAsync(name, asOf, ct)` — the raw stored `ValueJson` string, or null if nothing matches.
- `GetAsync<T>(name, asOf, ct)` — parses `GetAsync(name, ...)`'s JSON, unwraps the `{"Value": ...}` storage
  envelope, deserializes just the inner value as `T`. Returns `default` (not throw) when nothing's stored.
- `GetAsync<C>(asOf, ct)` where `C : IConstant, new()` — the one used by real call sites. Looks up by `C.Name`,
  and if nothing's stored, returns `new C()` (i.e. the compiled default) instead of a null/empty result —
  **never returns null.** If something *is* stored, deserializes the *whole* stored JSON directly into `C` via
  `JsonSerializer.Deserialize<C>(json)` — this only works because of the storage envelope described next.

## 4. The storage envelope — `{"Value": ...}`, and a real bug this fixed

Every value is stored wrapped as a one-property JSON object, `{"Value": <the actual value>}` — **not** the bare
value. This is because `GetAsync<C>()` deserializes the *entire* stored JSON straight into the concrete
`Constant<T>`-derived type `C` (which has exactly one instance property, `Value`) — so the stored shape has to
match `C`'s own JSON shape, envelope and all.

**A real bug existed here and was fixed**: `ConstantsManageService.SetAsync<T>` used to serialize just the bare
value (`JsonSerializer.Serialize(value)`), while `GetAsync<C>()` — every real consumer's actual read path —
always expected the wrapped shape. Anything written through the generic setter was silently unreadable via the
typed getter (no exception — `JsonSerializer.Deserialize<C>` on a bare-value JSON string just produces a `C`
with `Value` still at its default). Fixed by wrapping in every write path:

```csharp
// ConstantsManageService.SetAsync<T>
public Task SetAsync<T>(string name, T value, DateTime? date = null, CancellationToken cancellationToken = default)
    => SetInternalAsync(name, JsonSerializer.Serialize(new { Value = value }), date, cancellationToken);
```

`SetRawAsync`/`UpdateValueAsync` (the admin controller's untyped-at-compile-time paths, working off a
`JsonElement` since the controller doesn't know `T` at compile time) wrap the same way via a shared
`WrapValue(JsonElement)` helper. `ConstantsService.GetAsync<T>(name, ...)` correspondingly **unwraps** the
envelope (`document.RootElement.GetProperty("Value").Deserialize<T>()`) rather than deserializing the raw JSON
directly as `T`.

**If you ever hand-write a raw `INSERT`/seed row for a constant (tests, migrations, seed scripts), get this
envelope right** — a bare `"UAH"` (no wrapper) is silently unreadable via `GetAsync<C>()` and throws
`JsonException` via `GetAsync<T>(name, ...)`. `ConstantsServiceTypedGetTests.GetAsync_LegacyBareValueShape_ThrowsOnDeserialize`
is a regression test pinned exactly to this historical bug shape. The correct way to seed a constant's value in
a test is always through `ConstantsManageService.SetAsync<T>`/`SetRawAsync`, or by hand-constructing the
envelope: `ValueJson = JsonSerializer.Serialize(new HomeCurrencyConstant { Value = "EUR" })` (serializing the
whole `Constant<T>` instance happens to produce the same `{"Value": ...}` shape, since `Value` is its only
instance property).

## 5. Writing/managing: `IConstantsManageService`

`Kandra.Application.Abstractions/Services/IConstantsManageService.cs` extends `IConstantsService` and adds
(every method here checks the write-constant permission — `ConstantsAuthorizationPoint`/
`ApplicationArea.Constant`, unlike the ungated read side):

| Method | Purpose |
|---|---|
| `SetAsync(name, value: string, date?, ct)` | Stores `value` **verbatim** as `ValueJson` — no envelope wrapping, caller's responsibility to shape it correctly. Rarely what you want directly. |
| `SetAsync<T>(name, value: T, date?, ct)` | Wraps as `{"Value": ...}` — the normal typed write path. **Overload-resolution gotcha**: if `T` is itself `string`, a call written as `SetAsync(name, someString)` resolves to the non-generic overload above instead — pass the type argument explicitly, `SetAsync<string>(name, someString)`, to actually reach this one. |
| `SetRawAsync(name, value: JsonElement, date?, ct)` | Same wrapping, for a value whose CLR type isn't known at compile time (the admin controller's add path). Upserts by exact `(Name, Date)` — the "add a new dated value" path. |
| `UpdateValueAsync(id: Guid, value: JsonElement, ct)` | Edits **one existing row's value in place**, identified purely by `ConstantRecord.Id` — deliberately never touches or re-derives `Date`. Throws `NotFoundException` if the id doesn't match a live row. This is the admin UI's "edit in place" path — added specifically because re-deriving/matching a row by its effective instant across a network round trip (offset differences, DST, JSON precision) turned out to be unreliably fragile; see the `project_constants_admin_ui_implemented` memory's "Fourth round" for the incident that drove this split. |
| `GetNames(ct)` | Every constant name known to the registry. |
| `GetHistoryAsync(name, ct)` | Every dated row for one constant, newest first. |
| `GetAllAsync(asOf?, ct)` | Every **known** constant's effective snapshot as of a date — including never-overridden ones via their compiled default. Fetches every non-deleted row (not just currently-effective ones) so a future-dated-only override can still be flagged (`ConstantSnapshot.HasFutureOverride`) rather than looking untouched. |
| `DeleteAsync(name, date, ct)` | Soft-deletes one row, matched by exact `(Name, Date)`. |
| `DeleteAsync(id, ct)` | Soft-deletes one row by `Id` — the admin API's actual path (round-tripping an exact instant through a URL for delete is needless fragility once precision is finer than a day). |

`ConstantSnapshot` (`Kandra.Application.Abstractions/Constants/ConstantSnapshot.cs`):

```csharp
public sealed record ConstantSnapshot(string Name, string ValueJson, bool IsOverridden, DateTime? SetAt, Guid? Id = null, bool HasFutureOverride = false);
```

## 6. The admin UI — engine-level, already generic, discovers new constants automatically

This is the part that makes a constant a one-file addition. Every piece below already exists, is registered
once at the engine level, and reads `IConstantRegistry`/`ConstantValueKind` — **none of it is touched when a
new constant is added**:

- **`Kandra.Forms/Constants/`** — `ConstantSummaryDto` (list row: `Name`, `ValueKind`, `Id?`, `ValueJson`,
  `IsOverridden`, `SetAt?`, `HasFutureOverride`), `ConstantHistoryEntryDto`, `SetConstantValueDto`,
  `UpdateConstantValueDto`, `ConstantValueKind` enum (`String`/`Integer`/`Decimal`/`Boolean`/`DateTime`/`Other`
  — resolved server-side by inspecting `ConstantRegistryEntry.ValueType`, never crosses the wire as a raw
  `Type`).
- **`Kandra.Api.Lib/Controllers/V1/ConstantsController.cs`** — hand-written (like `SettingsController`/
  `ChartOfAccountsController`, not generated — there's no per-type CRUD here, just a handful of endpoints
  wrapping `IConstantsManageService`): `GET /api/v1/Constants` (all snapshots), `GET
  /api/v1/Constants/{name}/history`, `POST /api/v1/Constants/{name}` (add a dated value), `PUT
  /api/v1/Constants/{id:guid}` (edit in place), `DELETE /api/v1/Constants/{id:guid}`. Wraps/unwraps the
  `{"Value": ...}` envelope so it never leaks onto the API surface — a client always sees/sends the bare value's
  JSON.
- **`Kandra.ClientLib.Common/ApiClients/IConstantsApiClient.cs`** + **`Services/Crud/{I,}ConstantsClientService.cs`**
  — standard Refit-client + thin-wrapper pattern.
- **`PolicyBuilder.cs`** — `"ManageConstants"` policy (claim-based:
  `CheckPermissions(ctx, "Constant", "*", "View")` + `RequireRole("admin","user")`).
- **`Kandra.ClientLib.MudBlazor/Pages/Admin/Constants/{List,ConstantEditDialog,ConstantHistoryDialog}.razor`** —
  plain hand-written pages, wired into `NavMenu.razor`'s hardcoded Admin group (next to Users/Roles) — **not**
  through the per-configuration `Interface` navigation tree, so `add-interface-node` is irrelevant here.
  `List.razor` calls `IConstantsClientService.GetAllAsync()` once on load and renders one row per registry
  entry, showing `Name` **raw, unlocalized** — there is no `[Caption]`-style attribute for a constant's display
  name, so pick a `Name` value you're fine showing verbatim to an admin. `ConstantEditDialog.razor` switches its
  input control purely on `ValueKind` (`MudSwitch` for `Boolean`, `MudNumericField` for `Integer`/`Decimal`, a
  date+time picker pair for `DateTime`, a plain text field for `String`/`Other`) — a brand-new constant of any
  of these five kinds gets a working editor with zero UI code written.

All of this together is why the SKILL.md's step 4 has "nothing to build" — the previous version of this skill
(before this rewrite) still described the admin UI as not existing ("service exists, no admin UI"); that is no
longer true and this reference reflects the shipped shape.

## 7. Localization — zero new keys needed

Every string label the admin UI shows (`"Constants"`, `"Constant_Value"`, `"Constant_Overridden"`,
`"Constant_EffectiveDate"`, ...) is a resx key already declared once in the **engine-level**
`Kandra.Localization/Kandra{,.uk,.ru}.resx` — not `PersonalAccounting.Localization`. A new constant needs zero resx
edits in any locale file, in sharp contrast to Dictionaries/Documents/Reports (each of which needs 5+ new keys
per entity across 3 locale files — see `create-dictionary` reference.md §8). The only user-visible string tied
to your new constant at all is its own `Name` (e.g. `"HomeCurrency"`), shown verbatim, unlocalized, in the list.

## 8. Worked examples

Both real, current, and structurally identical — a constant genuinely doesn't vary in shape the way a
dictionary/document does (no flat-vs-hierarchical, no with-vs-without-lines split):

### `HomeCurrencyConstant`

`PersonalAccounting.Application/Constants/HomeCurrencyConstant.cs` — `Constant<string>("UAH")`, `Name => "HomeCurrency"`.
Read from `RateImportBehavior.OnSubmitAsync` (`PersonalAccounting.Application/Documents/RateImport.cs`):

```csharp
var homeCurrencyConstant = await constants.GetAsync<HomeCurrencyConstant>(entity.Date, cancellationToken);
var homeCurrencyCode = homeCurrencyConstant.Value;
```

### `DefaultPriceTypeConstant`

`PersonalAccounting.Application/Constants/DefaultPriceTypeConstant.cs` — `Constant<string>("RETAIL")`,
`Name => "DefaultPriceType"`. Read from `PriceListReportBehavior` (`PersonalAccounting.Application/Reports/PriceListReport.cs`)
as a convenience default for a report filter, deliberately tolerant of the lookup coming back empty (the form
validator, not this lookup, is what actually enforces the field is set before the report runs):

```csharp
// Convenience default only - the user can still change it, and the form validator already
// requires PriceTypeId non-empty before running the report, so a missing/renamed "RETAIL" row
// just leaves this unset rather than blocking the report.
var defaultPriceTypeCode = (await constants.GetAsync<DefaultPriceTypeConstant>(filters.DateAsOf, cancellationToken)).Value;
```

Read the full two files directly under those paths for the exact current text — this reference and `data/`
already reflect them, but the repo copy is the tiebreaker if anything ever drifts.

## Hand-maintained files

A real, grounded audit — every shared/engine file that participates in the constant path, and whether adding a
new constant requires touching it:

| File | Touched when adding a new constant? | Why |
|---|---|---|
| `{Config}.Enums/PersonalAccountingTypeIds/PersonalAccountingTypeIds.{Name}.cs` | **Yes — a new sibling file, never an edit to `PersonalAccountingTypeIds.cs`.** | Mints the fresh `TypeId` GUID, centralized the same way as every Document/Dictionary entity. |
| `{Config}.Application/Constants/{Name}Constant.cs` | **Yes — the constant class itself.** | The other new file. Together with the `PersonalAccountingTypeIds` entry above, this is the whole addition — everything else below is pre-existing, engine-level, and generic. |
| `{Config}.Application/Generated.Net/Kandra.Generators.Constants/GeneratedConstantRegistry.g.cs` | No (regenerated automatically on build) | Never hand-edit — `dotnet build` regenerates it from every `[KandraConstant]` class in the project. |
| `{Config}.Application/ConfigureServices.cs` | No | `services.AddSingleton<IConstantRegistry, GeneratedConstantRegistry>()` is a fixed, one-time line — already there before your first constant existed. |
| `Sys_Constants` table / EF migrations | No | Engine-level, schema-static, mapped once in `PlatformDbContext`. A new constant is new rows keyed by `Name`, not a schema change. |
| `Kandra.Api.Lib/Controllers/V1/ConstantsController.cs` | No | Engine-level, generic over `IConstantRegistry`/`IConstantsManageService` — already handles every constant, present and future. |
| `Kandra.ClientLib.Common/ApiClients/IConstantsApiClient.cs`, `Services/Crud/ConstantsClientService.cs` | No | Same — one Refit client/service for the whole `/api/v1/Constants` surface, not per-constant. |
| `Kandra.ClientLib.MudBlazor/Pages/Admin/Constants/*.razor` | No | Reads the list from the API and renders per-`ValueKind`, not per-constant-name. |
| `NavMenu.razor`'s Admin group / `PolicyBuilder.cs`'s `"ManageConstants"` policy | No | One nav entry and one policy cover every constant collectively — not re-added per constant. |
| `Kandra.Localization/Kandra*.resx` (engine-level) | No | Every label the admin UI shows is a fixed, already-declared engine string — a constant's own `Name` is shown raw, unlocalized. |
| `{Config}.Localization/*.resx` (config-level) | No | Nothing here ever references a constant — contrast with Dictionaries/Documents, which need 5+ keys per entity here. |
| Consumer code (a behavior/report that reads the new constant via `IConstantsService.GetAsync<{Name}Constant>()`) | Only if you're actually wiring a new consumer | Not part of *declaring* the constant — a constant can exist, be manageable via the admin UI, and simply not be read by anything yet. |

Compare this table to `create-dictionary`'s equivalent (six real hand-maintained rows: type-id constant file,
numbering-bucket constant file, three-locale resx edits, Refit client interface, Blazor UI pages, nav entry) —
a constant shares the type-id-constant-file row with a dictionary but needs none of the other five (no
numbering, no resx, no client, no UI, no nav — all engine-generic). That gap is the entire point of this
reference's opening paragraph.

## If this reference goes stale

This document is a snapshot, not a live view. If the code disagrees with something here (a compile error citing
a signature that doesn't match, a missing member, a diagnostic code that doesn't exist), trust the code — then
update this file so the next person doesn't hit the same surprise. The fastest way to re-verify a specific
claim: grep the type/service name under `src/KandraCore/Kandra.Application.Abstractions/Constants/`,
`Kandra.Application/Services/Constants*.cs`, `Kandra.Forms/Constants/`,
`Kandra.Api.Lib/Controllers/V1/ConstantsController.cs`, `Kandra.Attributes/Constants/`, or
`Kandra.Generators.Constants/` — every type named above lives in exactly one of those locations.
