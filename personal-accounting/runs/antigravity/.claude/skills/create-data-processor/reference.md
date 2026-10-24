# Data processor engine — reference

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

Self-contained explanation of every engine type/attribute `SKILL.md` walks through — written so you don't need
to go read `src/KandraCore/*` source to use it. Exact signatures as of the last time this was verified against the
engine; if a build error contradicts something here, the code wins (see "If this reference goes stale" at the
bottom). Ready-to-copy `.cs` skeletons matching everything below live in [`data/`](data/).

## What a data processor actually is

A transient, non-persisted batch-operation form: input parameters (possibly none), an **Execute** button, and a
result. Root `CLAUDE.md` calls it "not persisted" — concretely, that means **there is no Domain-plane class at
all**. Every other entity kind (Dictionary, Document) has a `*Base` EF entity in `{Config}.Domain`; a Report has
none either but still has a print-data Dto shaped like a query result. A data processor has neither — it's the
one entity kind that genuinely starts at the Forms plane and never touches EF Core, a table, or a type identity.

**A data processor spans two class-library projects, same shape as a Report**: `{Config}.Forms/DataProcessors/
{Name}.cs` (input Dto + result Dto + Validator — the plane the Blazor WASM client also references, same hard
rule as every other entity kind: never let it pull in `.Application`/`.Domain`/`.Persistence`) and
`{Config}.Application/DataProcessors/{Name}.cs` (the Behavior — server-only). No Mapper, unlike a Report or
Dictionary — there's no persisted entity on the other side of a mapping.

**What replaces `OnSaveAsync`**: `IDataProcessorBehavior<TInput,TResult>` is deliberately the simplest behavior
contract in the engine — no `Load`/`Save`/`Delete`, because there's nothing to persist. Just two hooks:
`OnNewAsync(TInput input, CancellationToken)` (produce/default a fresh input — mirrors every other kind's "new"
endpoint) and `OnProcessAsync(TInput input, CancellationToken) : Task<TResult>` (do the work, return a result —
**this is the actual "run" entry point**, invoked by the generated page's Execute button). There is no
`OnOpenAsync`/`OnSubmitAsync` either — a processor doesn't "open" or "submit," it just runs once per click.

**The Blazor UI is fully generated, same as a Report — genuinely no hand-written `.razor` file for a new data
processor.** `Kandra.Generators.Ui.Blazor`'s `DataProcessorUiEmitter` is a real, working generator (not a stub)
emitting one sealed page class per processor. `PersonalAccounting`'s only data processor (`ImportNbuRates`) already went
through this cutover — its old hand-written page lives only at
`PersonalAccounting.Client.MudBlazor/_HandWrittenReference/DataProcessor/ImportNbuRates/ImportNbuRates.razor` (reference
only, excluded from compilation); `Pages/DataProcessors/` is empty. **This is a real correction to an earlier
version of this skill**, which described copying a hand-written page as a required step — verified directly
against the current generator and the current (empty) `Pages/DataProcessors/` folder, not assumed from either
this skill's own prior text or from how Reports work.

**DI registration is also fully generated — this skill previously described it as manual, which is also stale
and wrong, verified against the actual generated output.** `[KandraDataProcessorBehavior(typeof(YourDto))]` on
the Behavior is genuinely all that's needed: `Kandra.Generators.Di.Application`'s `ApplicationDiEmitter` emits
`services.AddDataProcessor<YourBehavior, YourDto, YourResult>()` (or `AddRegisterWritingDataProcessor<...>()`
for the register-writing opt-in, §6) into `GeneratedPersonalAccountingApplicationDiExtensions.g.cs`, confirmed present
for `ImportNbuRatesBehavior` in the real generated file. **The only genuinely manual DI line for `ImportNbuRates`
specifically is `services.AddNbuRates()`** — that's the *plugin's own* composite registration (`create-plugin`'s
territory), not anything about the data-processor mechanics; a data processor that consumes no plugin needs zero
manual DI at all. The comment already sitting in `PersonalAccounting.Application/ConfigureServices.cs` says this
outright: *"ImportNbuRatesBehavior itself is registered as a data processor by
AddGeneratedPersonalAccountingApplicationEntities."*

**A data processor also gets a fully auto-derived Execute permission claim + policy — unlike a Report, which is
deliberately excluded from this** (`create-report` reference.md §8). `Kandra.Generators.Di.Application`'s
`EntityClaimsEmitter` seeds one `O|{ObjectName}|Execute` claim per `[KandraDataProcessorBehavior]`/
`[KandraRegisterWritingDataProcessorBehavior]`-attributed class it discovers, and
`Kandra.Generators.ClientLib`'s `PolicyEmitter` generates a matching client-side `"Execute{ObjectName}"`
authorization policy — both derive `ObjectName` the same way, by stripping a trailing `Dto` off the input Dto's
simple name (`ImportNbuRatesDto` → `"ImportNbuRates"`). **This is why `IDataProcessorBehavior<,>.ObjectName`
isn't free-form the way a Report's is** — `DataProcessorService<TInput,TResult>.Authorize()` (server-side)
literally calls `authorizationChecker.Authorize(new DataProcessorAuthorizationPoint { ObjectName =
behavior.ObjectName })`, so if your Behavior's `ObjectName` doesn't match the Dto-derived convention exactly,
the seeded claim and the runtime check disagree and the processor becomes permanently unauthorized for everyone
— not a cosmetic mismatch like Report's, a real bug. Always set `ObjectName` to the input Dto's simple name
minus `Dto`, nothing else (§5/§8).

**A third thing comes for free, not documented anywhere else in this repo's skills**: every discovered
`[KandraDataProcessorBehavior]`/`RegisterWritingDataProcessorBehavior` also gets one entry in a generated
`IDataProcessorRegistry` (`Kandra.Generators.DataProcessorRegistry`, a distinct generator from the three above)
— a compile-time, reflection-free `Name`/`InputDtoType`/`ResultDtoType`/`BehaviorType`/`Execute` delegate tuple
that the Job Scheduler's processor picker reads to let an admin schedule *any* data processor to run
periodically, with zero extra code. You don't need to do anything for this — it's automatic the moment
`[KandraDataProcessorBehavior]` exists — but it's worth knowing a new processor is schedulable "for free" the
moment it's built.

### Hand-maintained files

| What | File | Why it's manual |
|---|---|---|
| Input Dto + result Dto + Validator | New file `{Config}.Forms/DataProcessors/{Name}.cs` | Core Forms-plane file — hand-written for every entity kind, no declarative composition model. |
| Behavior | New file `{Config}.Application/DataProcessors/{Name}.cs` | Same, Application-plane. The one file with actual business logic. |
| Localization keys | Edit to `{Config}.Localization/*.resx` (English required, uk/ru optional) | The processor's own name (`[KandraDataProcessorForm(Name=...)]`'s value, e.g. `"ImportNbuRates"`) is a resx key that does double duty — it's both the generated page's title and (via `add-interface-node`) its nav Entity node's default caption — plus every `[Caption]` key on every input/result field. §9. |
| Refit client interface | New file `{Config}.ClientLib.Common/ApiClients/I{Name}ApiClient.cs` | Thin — same "declaration only, Refit's own generator implements the body" gap as every other entity kind's client interface. §7. |
| Nav entry | Edit to `{Config}.Application/Navigation/*Interfaces.cs` | Not derived automatically — `add-interface-node`. Unlike Report, a DataProcessor `Entity` node **does** get an auto-derived permission check (§8) — nothing extra to configure there, just the one line adding the node itself. |

Explicitly **not** needed, unlike a Dictionary/Document: no Domain-plane class at all (not just no type-id
constant — no class whatsoever), no numbering bucket, no EF migration for the processor itself (unless it also
creates documents/entities of a type that doesn't exist yet — a separate `create-document`/`create-dictionary`
concern), no Blazor UI page (generated), no manual DI registration, no manual claim/policy wiring, and no Mapper
(nothing to map to/from — a Report at least maps a repository row to a line Dto; a data processor's `OnProcessAsync`
returns its `TResult` directly).

## 1. Attribute vocabulary

### On the input Dto (`IDataProcessorInputDto`, class-level)

| Attribute | Namespace | Signature | Meaning |
|---|---|---|---|
| `[KandraDataProcessorForm]` | `.Application` | `Name` (string, **required**), `ResultDtoType` (Type), `ValidatorType` (Type) — inherited from `KandraFormAttribute` | `Name` drives the controller class name, the API route (`/api/v1/processors/{Name}`), and the Refit client name — set explicitly, never derived by convention (irregular cases like this one don't follow a simple pluralization rule, per the attribute's own doc comment). **`ValidatorType` is not optional in practice** — see the callout below. |
| `[ExecuteFormTitle(value, isLiteral=false)]` | `.Naming` | inherited from `VisibleNameAttribute` | Declared by convention on every data-processor Dto — **currently unconsumed by any generator**, verified against `DataProcessorUiEmitter.cs` directly (same stale-doc-comment trap `create-report` reference.md §1 documents for Reports — the real title is `Loc[policyStem]`, §6). Keep adding it for consistency/forward-compat, don't expect it to control anything today. |
| `[Description]` | BCL (`System.ComponentModel`) | class or property | Semantic explanation of what the Dto/field means and how it's used — not a UI label (`[Caption]` covers that). Reflected by `Kandra.Mcp`'s `EntityFieldMetadataBuilder` and surfaced through the MCP server's `describe_entity_type`/`list_entity_types` tools as an AI agent's primary way to learn what an entity means. **Mandatory on both the input Dto class and the result Dto class**; add on a property whenever its purpose isn't obvious from its name/caption alone. Unlike `[Caption]` on the result Dto, `[Description]` is read independently — worth adding even on an uncaptioned technical-id property (see `ImportNbuRatesResult.RateImportId`, §4). |

**`ValidatorType` must be set explicitly, or the generated page silently never appears.** Unlike a Report (whose
generated page renders filter fields directly, no client-side validation wrapper), a data processor's generated
page wraps the input Dto in a validated `Form<TInput,TValidator>` — so `DataProcessorUiEmitter` needs to resolve
a concrete validator type at generator time. If `ValidatorType` is omitted, `inputUi.ValidatorTypeFullyQualifiedName`
comes back `null` and the emitter returns early — **no page is generated at all, no error, no diagnostic** (the
same "skip rather than emit broken code" precedent used elsewhere in this generator family). `ImportNbuRatesDto`
sets it: `[KandraDataProcessorForm(Name = "ImportNbuRates", ValidatorType = typeof(ImportNbuRatesValidator),
ResultDtoType = typeof(ImportNbuRatesResult))]` — copy that shape, don't omit `ValidatorType` the way a Report
Dto can get away with omitting it.

### On the result Dto (`IDataProcessorResultDto`, property-level)

| Attribute | Namespace | Meaning |
|---|---|---|
| `[Caption("...")]` | `.Naming` | **The only thing that makes a result property visible at all.** `UiFieldDiscovery.TryBuildResultFormModel`/`AnalyzeResultFields` only surfaces a property that explicitly carries `[Caption]` — a property without one (a technical id the caller has no use for on screen, e.g. `ImportNbuRatesResult.RateImportId`) is silently skipped, not rendered with a fallback caption. This is the opposite default from an input Dto's own fields, which render even uncaptioned. An unsupported CLR kind is skipped the same way, silently. |
| `[Order(n)]` | `Kandra.Attributes` (root namespace) | Result-panel field order — declaration order otherwise, same convention as everywhere else `[Order]` is used. |

No `[ReportViewColumn]`/`[Aggregate]`/grid vocabulary here — a data processor's result panel is a plain
read-only field list (`page.Form<TResult>(Result, readOnly: true, ...)`), never a grid, even if `TResult` logically
represents "one row." If a processor's result is naturally tabular, consider whether it should really create
documents (§4) and let the caller view them normally, rather than trying to render a grid here.

### On the Behavior (Application plane)

| Attribute | Namespace | Signature | Meaning |
|---|---|---|---|
| `[KandraDataProcessorBehavior(formDtoType)]` | `.Application` | `(Type)` ctor, inherited from `KandraBehaviorAttribute` | Links the Behavior to its input Dto. The engine determines whether it's a plain or register-writing processor purely from **which interface the class implements** (`IDataProcessorBehavior<,>` vs. `IRegisterWritingDataProcessorBehavior<,>`, §6) — the attribute itself is identical either way, there's no separate attribute for the register-writing flavor. |

## 2. Base interfaces

```csharp
// Kandra.Forms.DataProcessors.IDataProcessorInputDto / IDataProcessorResultDto — markers only, no members
public interface IDataProcessorInputDto { }
public interface IDataProcessorResultDto { }

// Kandra.Application.Abstractions.Behaviors.IDataProcessorBehavior<TInput,TResult>
public interface IDataProcessorBehavior<TInput, TResult>
    where TInput : class, new()
{
    string ObjectName { get; }   // MUST equal the input Dto's simple name minus "Dto" - see §8, this is enforced at runtime

    ValueTask OnNewAsync(TInput input, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    Task<TResult> OnProcessAsync(TInput input, CancellationToken cancellationToken);  // the "run" entry point
}
```

`OnNewAsync` returns `ValueTask` (not `Task`) — the one difference in shape from `IReportBehavior<,>.OnNewAsync`,
which returns `Task`; both exist purely to seed sensible defaults server-side (`ImportNbuRatesBehavior.OnNewAsync`
sets `input.Date = DateTime.UtcNow.Date`).

## 3. Client/server service and API client contracts

```csharp
// Kandra.Application.Services.DataProcessors.IDataProcessorService<TInput,TResult> — server-side
public interface IDataProcessorService<TInput, TResult> where TInput : class, new()
{
    Task<TInput> NewAsync(CancellationToken cancellationToken);
    Task<TResult> ProcessAsync(TInput input, CancellationToken cancellationToken);
}

// Kandra.ClientLib.Common.Services.Crud.IDataProcessorService<TInput,TResult> — client-side, what the
// generated page actually injects as `ProcessorsService`
public interface IDataProcessorService<TInput, TResult>
{
    Task<TInput> NewAsync();
    Task<TResult> ProcessAsync(TInput input);
}

// Kandra.ClientLib.Common.ApiClients.IDataProcessorApiClient<TInput,TResult> — data-processor controllers
// inherit ControllerBase directly (not EntityControllerBase), so this stands alone, same shape as Report's.
public interface IDataProcessorApiClient<TInput, TResult> : IKandraProtectedApiClient
    where TInput : class, IDataProcessorInputDto
    where TResult : class, IDataProcessorResultDto
{
    [Get("new")]     Task<TInput> NewAsync(CancellationToken cancellationToken = default);
    [Post("execute")] Task<TResult> ExecuteAsync([Body] TInput body, CancellationToken cancellationToken = default);
}
```

The server-side `DataProcessorService<TInput,TResult>` implementation is where authorization actually happens
(§8) — both `NewAsync` and `ProcessAsync` call a private `Authorize()` first, and `ProcessAsync` also runs
`ValidationHelper.ValidateAsync(input, validator, cancellationToken)` before calling into the Behavior's
`OnProcessAsync` — server-side validation is enforced regardless of whether the generated client page's own
`Form<TInput,TValidator>` wrapper (§6) already checked it.

## 4. Worked example: `ImportNbuRates`

Full, current, working source — `PersonalAccounting.Forms/DataProcessors/ImportNbuRates.cs` +
`PersonalAccounting.Application/DataProcessors/ImportNbuRates.cs`. Read both end to end; this section calls out the
non-obvious parts. Its real `[Description]` content (both Dtos, every field including the uncaptioned
`RateImportId`) is worth reading as a model for how much detail is useful — a sentence or two per field, not a
paragraph.

**Input Dto** (`ImportNbuRatesDto`):
```csharp
[KandraDataProcessorForm(Name = "ImportNbuRates", ValidatorType = typeof(ImportNbuRatesValidator), ResultDtoType = typeof(ImportNbuRatesResult))]
[ExecuteFormTitle("ImportNbuRates")]
public class ImportNbuRatesDto : IDataProcessorInputDto
{
    [Caption("Date")] [DateEditor(DateEditorKind.Date)]
    public DateTime? Date { get; set; }        // null = "today", defaulted by OnNewAsync

    [Caption("Button_Submit")]
    public bool Submit { get; set; } = true;   // insert the created document active/submitted, not draft
}
```
Two ordinary input fields, one of which reuses `[DateEditor]` (the same editor vocabulary every other entity
kind's fields use — `create-dictionary`/`create-report` reference.md have the full editor attribute list, it
isn't repeated here since nothing about it is data-processor-specific).

**Result Dto** (`ImportNbuRatesResult`): `RateImportId` (`Guid`, deliberately **no** `[Caption]` — a technical id
the result panel shouldn't show, mirrors how a Report's un-attributed FK id column never becomes a grid column
either) alongside `Code`/`RatesImported`, both `[Order]`+`[Caption]`, which do render.

**Validator** (`ImportNbuRatesValidator : AbstractValidator<ImportNbuRatesDto>`) — one rule,
`RuleFor(x => x.Date).LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1)).When(x => x.Date.HasValue)`. Takes
`IStringLocalizer` in its constructor for the message — same FluentValidation shape as every other entity kind's
validator, mandatory even though this processor's input is nearly trivial.

**Behavior** (`ImportNbuRatesBehavior`):
```csharp
[KandraDataProcessorBehavior(typeof(ImportNbuRatesDto))]
public class ImportNbuRatesBehavior(
    INbuRatesClient nbuClient,                                       // the plugin's own client interface
    IDocumentCrudService<RateImportDto, RateImportQueryDto> rateImports,  // ordinary engine CRUD service
    IEntityRepository<Currency> currencies)                          // ordinary engine repository
    : IDataProcessorBehavior<ImportNbuRatesDto, ImportNbuRatesResult>
{
    public string ObjectName => "ImportNbuRates";   // MUST equal the Dto-name-derived convention, see §8

    public ValueTask OnNewAsync(ImportNbuRatesDto input, CancellationToken cancellationToken)
    {
        input.Date = DateTime.UtcNow.Date;
        return ValueTask.CompletedTask;
    }

    public async Task<ImportNbuRatesResult> OnProcessAsync(ImportNbuRatesDto input, CancellationToken cancellationToken)
    {
        var date = DateOnly.FromDateTime((input.Date ?? DateTime.UtcNow).Date);
        var nbuRates = await nbuClient.GetRatesAsync(date, cancellationToken);   // call the plugin

        var (_, currencyRows) = await currencies.GetExtendedAsync(q => q, 1, int.MaxValue, true, cancellationToken);
        var currencyByCode = currencyRows.ToDictionary(c => c.Code, c => c.Id, StringComparer.OrdinalIgnoreCase);

        // Ordinary document create/insert path - exactly what a human user would do through the UI.
        var dto = await rateImports.NewAsync(date.ToDateTime(TimeOnly.MinValue), null, cancellationToken);
        dto.IsActive = input.Submit;
        dto.Lines = nbuRates.Where(r => currencyByCode.ContainsKey(r.CurrencyCode))
            .Select(r => new RateImportLineDto { CurrencyId = currencyByCode[r.CurrencyCode], Rate = r.Rate })
            .ToList();
        var created = await rateImports.InsertAsync(dto, cancellationToken);

        return new ImportNbuRatesResult { RateImportId = created.Id, Code = dto.Code, RatesImported = dto.Lines.Count };
    }
}
```

**Constructor-injecting the plugin's `INbuRatesClient` alongside ordinary engine services is the whole pattern**
— a data processor Behavior is not restricted the way a document Behavior's register access is (no `ISubmitScope`
gate for the plain `IDataProcessorBehavior<,>` interface, §6). `IDocumentCrudService<RateImportDto,
RateImportQueryDto>.NewAsync(...)`/`.InsertAsync(...)` is the same document create/insert path a human user's
click on "Add" + "Save" would take — going through it (rather than constructing a `RateImport` entity directly)
is what keeps the normal posting pipeline (register writes, audit trail) intact. This is the recommended,
indirect way for a data processor to affect registers; §6 covers the direct alternative.

### A note on "plugin-hosted," corrected against current code

**`ImportNbuRates` (the Dto/Behavior pair) is not plugin-hosted — it is a completely ordinary, PersonalAccounting-native
data processor, in exactly the two files this section points at, same as every other entity kind.** The only
thing that lives in the plugin project (`src/Plugins/UA/Rates/Kandra.Plugins.UA.Rates.Nbu/`) is the external API
client — `INbuRatesClient`/`NbuRatesClient`/`NbuRate`/`NbuRateResponse`/`ServiceCollectionExtensions.AddNbuRates()`
— a plain, engine-free .NET library with no `Kandra.*` reference at all (`src/Plugins/CLAUDE.md`'s tier model).
There is no `ImportNbuRates.cs` anywhere under `src/Plugins/`. If you were expecting the Dto/Behavior themselves
to live in the plugin, that's the one thing worth correcting explicitly: **a PersonalAccounting-native data processor's
files already live exactly where this section says** — `{Config}.Forms/DataProcessors/{Name}.cs` +
`{Config}.Application/DataProcessors/{Name}.cs`, full stop, whether or not it happens to consume a plugin. What
a plugin *does* change is only the Behavior's constructor-injected dependencies (a plugin's client interface
instead of, or alongside, ordinary engine services) — nothing about where the processor's own files live, how
it's attributed, or how it's wired. See `create-plugin`'s own step 5 ("Wire it into a configuration") for the
inverse write-up of this same fact, from the plugin author's side.

## 5. The generated UI

`Kandra.Generators.Ui.Blazor.DataProcessorUiEmitter` emits exactly one `spc.AddSource` call per data processor —
unlike `FlatDictionaryUiEmitter`/`DocumentUiEmitter`'s five (List/Add/Update/View/Form), a data processor has no
List/Add/Update/View distinction, so one generated class is the whole page: a sealed subclass of
`DataProcessorPageBase<TInput,TResult,TValidator>` (`Kandra.ClientLib.MudBlazor.Shared.Pages`) into
`{Config}.Client.MudBlazor/Generated.Net/Kandra.Generators.Ui.Blazor/Kandra.Generators.UiFormGenerator/
{DtoSimpleName}Processor.g.cs` — e.g. `ImportNbuRatesDtoProcessor.g.cs` (note: `{DtoSimpleName}`, the whole class
name **including** the `Dto` suffix, unlike a Report's generated filename which uses `form.Name` and therefore
never carries `Dto` at all — a real, easy-to-mistype difference between the two).

What's read from the attributes in §1, concretely:

- **Route**: `/dataprocessor/{stem}`, where `stem` = `UiCodeGenHelpers.RouteStem(form.DtoSimpleName)` — the Dto's
  own class name, trailing `Dto` stripped, camelCased (e.g. `ImportNbuRatesDto` → `/dataprocessor/importNbuRates`).
  This is derived from the **Dto's class name**, not `[KandraDataProcessorForm(Name=...)]`'s value directly — in
  practice the two always agree by convention (the Dto is always named `{Name}Dto`), but they're resolved through
  different code paths, worth knowing if you ever see them diverge.
- **`Policy`**: `"Execute{policyStem}"`, `policyStem` = the same Dto-stem convention, e.g.
  `"ExecuteImportNbuRates"`. **`DataProcessorPageBase.Policy`'s own XML doc comment is stale and describes the
  wrong string** — it says the convention is `"Execute{Name}Behavior"` (e.g. `"ExecuteImportNbuRatesBehavior"`).
  That is not what's emitted today, and the emitter's own code comment explains why: a stray `"Behavior"` suffix
  here previously referenced a policy that was never registered, throwing
  `InvalidOperationException("Policy named: '...' was not found")` the moment anyone navigated to a data
  processor page — a real, previously-shipped bug, now fixed by dropping the suffix. **Trust `DataProcessorUiEmitter`'s
  emitted string (`Execute{policyStem}`, no `Behavior`) over `DataProcessorPageBase`'s doc comment** — matching
  `create-report` reference.md §1's "don't trust an attribute's own doc comment" lesson, here on a base class's
  doc comment instead.
- **`Title`**: `Loc[policyStem]` — e.g. `Loc["ImportNbuRates"]`, not derived from `[ExecuteFormTitle]` (§1). The
  same resx key doubles as the nav Entity node's default caption, same mechanism `create-report` reference.md §6
  documents for Reports (`EntityRouteConventions` — §8 below has the DataProcessor-specific derivation).
- **Input fields**: `inputUi.OwnFields`, in declared order, rendered inside a validated `Form<TInput,TValidator>`
  — every field editor attribute (`[Dropdown]`/`[Search]`/`[MultiSelect]`/`[DateEditor]`/...) works exactly as it
  does for a Dictionary/Document/Report input field; nothing data-processor-specific about field rendering itself.
- **Result fields**: `resultUi.OwnFields` (only `[Caption]`-carrying result properties, §1) rendered read-only,
  only once `Result is not null` (i.e., only after a successful Execute).

**Client-side page lifecycle** (`DataProcessorPageBase<TInput,TResult,TValidator>`): `OnInitializedAsync` calls
`ProcessorsService.NewAsync()` (hits the Behavior's `OnNewAsync`) to seed `Input`. `BuildRenderTree` guards
`Input is null` — **this guard already existed correctly before Reports needed the identical fix** (see
`create-report` reference.md §6 / `feedback_report_generator_gotchas` memory — `ReportPageBase` originally
lacked this exact guard and threw on the render that happens before `OnInitializedAsync`'s task resolves;
`DataProcessorPageBase` is the base class that already had it right, and was the pattern Reports had to catch up
to, not the other way around). Clicking Execute calls `HandleExecute` → `ProcessorsService.ProcessAsync(Input)`,
setting `WaitingOperation` around the call (drives the Execute button's own busy state) and `Result` on success;
the result panel (`RenderResult`) is wrapped in its own `if (Result is not null)` check, separate from the
`Input is null` guard above.

## 6. Writing registers directly (rare) — an interface to implement, not a class to derive from

Most processors should create/submit documents instead (§4) — that's what keeps register writes flowing through
the ordinary posting pipeline (audit trail, reversal-on-unsubmit). If a processor is a genuine administrative
operation that must bypass documents entirely (period close, recompute, data fix-up), **implement**
`IRegisterWritingDataProcessorBehavior<TInput,TResult>` instead of `IDataProcessorBehavior<TInput,TResult>` —
this is an interface, not an abstract base class to derive from (a subtlety worth being precise about, since an
earlier version of this skill said "derive from"):

```csharp
// Kandra.Application.Abstractions.Behaviors.IRegisterWritingDataProcessorBehavior<TInput,TResult>
public interface IRegisterWritingDataProcessorBehavior<TInput, TResult>
    where TInput : class, new()
{
    string ObjectName { get; }
    Task OnNewAsync(TInput input, CancellationToken cancellationToken) => Task.CompletedTask;   // Task, not ValueTask - the one shape difference from IDataProcessorBehavior<,>
    Task<TResult> OnProcessAsync(TInput input, ISubmitScope registers, CancellationToken cancellationToken);
}
```

Only this interface's `OnProcessAsync` is handed an `ISubmitScope` — same rule as a document Behavior's
`OnSubmitAsync`: `registers.GetWriter<TWriter>()` is still the only legal way to touch a register writer
anywhere in the codebase (`create-register` reference.md §5 has the full contract). **The choice of interface is
the capability — no separate attribute.** `[KandraDataProcessorBehavior(typeof(YourDto))]` is identical either
way; `BehaviorDiscovery` (the shared generator-discovery logic every `Kandra.Generators.*` project consumes)
determines `BehaviorKind.DataProcessor` vs. `BehaviorKind.RegisterWritingDataProcessor` purely by which of the
two interfaces your class's closed generic implements, and `ApplicationDiEmitter` emits
`AddDataProcessor<...>()` or `AddRegisterWritingDataProcessor<...>()` accordingly — same auto-generation as the
plain case (see the intro above; nothing here is the manual-registration exception this skill previously,
incorrectly, described). Server-side authorization also works identically
(`RegisterWritingDataProcessorService<TInput,TResult>.Authorize()` calls the same
`DataProcessorAuthorizationPoint { ObjectName = behavior.ObjectName }` pattern, §8).

There is no existing `PersonalAccounting` consumer of this path — treat it as validated-but-unproven infrastructure (same
caveat `create-register` reference.md gives Turnover/independent-Info registers) and double-check the current
shape of `IRegisterWritingDataProcessorBehavior<,>` before relying on it for something load-bearing.

## 7. Client Refit interface — hand-written declaration, generated implementation + service

```csharp
[PathPrefix("/api/v1/processors/ImportNbuRates")]
public interface IImportNbuRatesApiClient : IDataProcessorApiClient<ImportNbuRatesDto, ImportNbuRatesResult>
{
}
```

`[PathPrefix]` must match `[KandraDataProcessorForm(Name = "...")]`'s value exactly — same rule, same
`/api/v{version}/processors/{controller}` route convention the generated `ImportNbuRatesController.g.cs` (in
`{Config}.WebApi/Generated.Net/Kandra.Generators.Api/.../ImportNbuRatesController.g.cs`, namespace
`{Config}.WebApi.Controllers.V1.DataProcessors`) actually serves. The body is empty — `IDataProcessorApiClient<
TInput,TResult>` already declares `NewAsync`/`ExecuteAsync`, Refit's own source generator implements it at
compile time. Everything past the declaration is generated by `Kandra.Generators.ClientLib`'s `ClientEmitter`,
same mechanism `create-report` reference.md §7 documents for Reports:

```csharp
services.AddScoped<IDataProcessorApiClient<TInput, TResult>>(p => p.GetRequiredService<IYourApiClient>());
services.AddScoped<IDataProcessorService<TInput, TResult>, DataProcessorService<TInput, TResult>>();
```

(bridging your hand-written closed Refit interface to the generic one, and registering the client-side
`Kandra.ClientLib.Common.Services.Crud.IDataProcessorService<TInput,TResult>`/`DataProcessorService<TInput,TResult>`
wrapper `DataProcessorPageBase.ProcessorsService` actually injects — same "the emitter's internal constant names
aren't the real runtime type names" caveat `create-report` reference.md §7 already flags for its own analogous
pair). Never hand-write either registration.

## 8. Nav entry, and why DataProcessor *is* covered by auto-derived visibility (unlike Report)

`add-interface-node`'s `EntityRouteConventions.Resolve` (`Kandra.Application.Abstractions.Navigation`) handles a
DataProcessor Dto like this:

```csharp
if (dtoType.GetCustomAttribute<KandraDataProcessorFormAttribute>() is { } dataProcessorAttr)
{
    var stem = RouteStem(dtoType.Name);
    var policyStem = PolicyStem(dtoType.Name);
    var visibility = InterfaceNodeVisibility.Claim(ApplicationArea.DataProcessor, policyStem, ApplicationAction.Execute);
    return new Result($"/dataprocessor/{stem}", visibility, dataProcessorAttr.Name, false, "CloudDownload");
}
```

Unlike Report (`Visibility` always `null`, `create-report` reference.md §8), a DataProcessor `Entity` node **does**
get a real `Claim(ApplicationArea.DataProcessor, policyStem, ApplicationAction.Execute)` — which is possible
precisely because §8's auto-generated Execute claim (`EntityClaimsEmitter`) and policy (`PolicyEmitter`) exist for
DataProcessors and not for Reports. Adding the node is a one-liner in `PersonalAccounting.Application/Navigation/
PersonalAccountingInterfaces.cs`, same as any other entity kind:

```csharp
InterfaceNodeDefinition.Folder("Nav_DataProcessors", "Build",
    InterfaceNodeDefinition.Entity(typeof(ImportNbuRatesDto))),
```

`caption` defaults to `dataProcessorAttr.Name` (the `[KandraDataProcessorForm(Name=...)]` value, e.g.
`"ImportNbuRates"`) with no `[NavigationName]` fallback attempt — same double-duty resx key as the page's own
`Title` (§5).

## 9. Localization

Every `[Caption]` key on every input/result field, plus the processor's own `[KandraDataProcessorForm(Name=...)]`
value itself (§5/§8's double-duty title/nav-caption key), needs an entry in the `{Config}.Localization`
resx files (English required, uk/ru optional) — same rule and same "missing key silently renders the raw key name" failure mode as every
other entity kind's `[Caption]` keys. Two generic UI-chrome keys the generated page also needs are worth knowing
about explicitly: `"NotAuthorized"` is an **engine-level** key already in `Kandra.Localization` (don't redeclare
it), but **`"Execute"` is not** — it only exists in `PersonalAccounting.resx` at the configuration level (verified: no
`Kandra.Localization/*.resx` file defines it). A brand-new configuration with no data processors yet would need
to add `"Execute"` itself; `PersonalAccounting` already has it from `ImportNbuRates`, so adding a second processor there
needs no new generic key, only the processor-specific ones.

## If this reference goes stale

This document is a snapshot, not a live view — and it already caught two real, live discrepancies worth staying
alert to the pattern of: (1) this skill's own prior text claiming DI registration and the Blazor UI were manual
steps, both now generated (verified against `ApplicationDiEmitter`'s and `DataProcessorUiEmitter`'s actual
source and the actual generated output files, not assumed from how an earlier engine version worked); (2)
`DataProcessorPageBase.Policy`'s own XML doc comment describing a policy-name convention (`"Execute{Name}Behavior"`)
that a real, already-fixed bug proves is wrong (`DataProcessorUiEmitter` emits `"Execute{policyStem}"`, no
`Behavior` suffix). If the code disagrees with something here, trust the actual generator/attribute source over
both this file and any doc comment inside the engine itself, then fix whichever was wrong. Re-verify by grepping
the type/attribute name under `src/KandraCore/Kandra.Attributes/Application/KandraDataProcessor*.cs`,
`Kandra.Forms/DataProcessors/`, `Kandra.Application.Abstractions/Behaviors/{I,IRegisterWriting}DataProcessorBehavior.cs`,
`Kandra.Application/Services/DataProcessors/`, `Kandra.ClientLib.Common/{ApiClients,Services/Crud}/`,
`Kandra.ClientLib.MudBlazor/Shared/Pages/DataProcessorPageBase.cs`, `Kandra.Generators.Ui.Blazor/
DataProcessorUiEmitter.cs`, `Kandra.Generators.Di.Application/{ApplicationDiEmitter,EntityClaimsEmitter}.cs`,
`Kandra.Generators.ClientLib/{Clients/ClientEmitter,Policies/PolicyEmitter}.cs`, or
`Kandra.Generators.DataProcessorRegistry/` — every type named above lives in exactly one of those. Root
`CLAUDE.md`'s own note that `docs/kandra-architecture.md`'s early sections are aspirational applies especially
hard to its own §2.5: the doc's `DataProcessorForm`/`DataProcessorBehavior<TData,TForm>`/`IRegisterScope`/
`FileUpload`/`[Subsystem]` vocabulary doesn't exist anywhere in the real code — trust this reference and the
types named above instead.
