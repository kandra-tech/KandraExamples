---
name: create-data-processor
description: Add a new data processor (a transient, non-persisted batch-operation form — imports, period close, recalculation, and similar one-shot actions) to a Kandra configuration. Use when the user asks to add a new import, batch operation, admin action, or "run this and get a result" screen.
---

# Creating a data processor

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

A data processor has a form (input parameters, possibly none beyond an Execute button) and a behavior, but is
**never persisted itself** — see [`reference.md`](reference.md) for the full conceptual explanation (why there's
no Domain-plane class at all, the `IDataProcessorBehavior<,>`/`IRegisterWritingDataProcessorBehavior<,>`
contracts, and three things this skill previously got wrong that are now corrected and verified against current
engine source — the Blazor UI is generated, DI registration is generated, and the Execute permission is
auto-derived). This recipe is written against **this repo's one reference configuration, `PersonalAccounting`**;
substitute your own configuration's root project names if you're working in a different one.

Copy-paste `.cs` skeletons for every step below live in [`data/`](data/) (`data/README.md` explains the
`{{Placeholder}}` tokens) — start from those rather than a blank file.

**A data processor spans two class-library projects, same as a Report — no Domain-plane entity, no Mapper.** The
input Dto + result Dto + Validator (the one plane the Blazor WASM client also sees) live in `{Config}.Forms`, the
Behavior lives in `{Config}.Application`. `reference.md`'s intro explains why there's nothing to persist and
what replaces `OnSaveAsync`.

**The Blazor UI needs no hand-written step — verified against the current generator, not assumed.**
`Kandra.Generators.Ui.Blazor`'s `DataProcessorUiEmitter` is real and emits the whole page. `PersonalAccounting`'s only
data processor's old hand-written page already lives at `_HandWrittenReference/DataProcessor/ImportNbuRates/
ImportNbuRates.razor` (reference only) and `Pages/DataProcessors/` is empty — confirmed by listing the folder
directly, not inferred from how Reports work.

Concrete, currently-working example: `ImportNbuRates` (`PersonalAccounting.Forms/DataProcessors/ImportNbuRates.cs`,
`PersonalAccounting.Application/DataProcessors/ImportNbuRates.cs`) — read both end to end. **It is not "plugin-hosted"** —
both files are completely ordinary PersonalAccounting-native files, in exactly the location step 1/2 below describe; the
only thing that actually lives in `src/Plugins/UA/Rates/Kandra.Plugins.UA.Rates.Nbu/` is the external API client
(`INbuRatesClient`) the Behavior constructor-injects, same as it injects any other service. `reference.md` §4 has
the full walkthrough and the explicit correction if you came in expecting otherwise. It also doubles as the
reference for [`create-plugin`](../../../Plugins/.claude/skills/create-plugin/SKILL.md)'s "wire into a
configuration" step, since it's the plugin-consuming example.

## 1. Input Dto + result Dto + Validator (`{Config}.Forms/DataProcessors/{Name}.cs`)

Add `[Help("configuration/data-processors/{name}")]` to the form's Dto when the documentation site has a page for it: the generated pages then show a (?) button next to the title that opens that page in the user's language (a relative path is appended to `Kandra:Client:HelpUrl`/`HelpUrls`; an absolute http(s) URL is used as-is). No attribute, no button. On a hierarchical dictionary it goes on the abstract root Dto.

Copy `data/Forms.cs`, fill in placeholders. `reference.md` §1 has every attribute's exact signature
(`[KandraDataProcessorForm]`, `[ExecuteFormTitle]` — declared by convention but currently unconsumed, §1's own
callout) and the one easy-to-miss requirement: **`ValidatorType` must be set explicitly on
`[KandraDataProcessorForm(...)]`**, or the generated page silently never appears (no build error, no
diagnostic — `reference.md` §1 explains exactly why this differs from a Report Dto, which can get away without
one). A processor with no input fields at all is a legitimate, common shape — don't invent a placeholder field
just to have one; the generated page still renders with just an Execute button.

The result Dto has the **opposite** default from the input Dto: only a property explicitly carrying `[Caption]`
is ever rendered on the result panel (`reference.md` §1) — a technical id you don't want shown (like a created
entity's own `Guid`) should deliberately carry no `[Caption]` at all, not `[Caption]` plus some "hide me" flag.

Also fill in the real `[Description]` on both Dto classes (mandatory on each) and on any field whose meaning
isn't obvious from its name/caption alone — including an uncaptioned result field, since `[Description]` is
read independently of `[Caption]` (`reference.md` §1). This is what the MCP server's `describe_entity_type`/
`list_entity_types` tools surface to an AI agent, `[Caption]` is only a UI label, not an explanation.

## 2. Behavior (`{Config}.Application/DataProcessors/{Name}.cs`)

Copy `data/Application.cs`, fill in placeholders. Everyday services (`IDocumentCrudService<TDto,TQueryDto>`,
`IEntityRepository<TEntity>`, a plugin's client interface, ...) are constructor-injected normally — a data
processor Behavior is not restricted the way a document Behavior's register access is. `OnProcessAsync` is the
actual "run" entry point (there's no `OnSaveAsync` — nothing persists, `reference.md`'s intro). The recommended
pattern: call a plugin/external service for data if needed, then use the ordinary CRUD service to create/insert/
submit real documents, exactly like a human user would through the UI — this is what keeps register writes
flowing through the normal posting pipeline.

To tell someone the run finished (especially one scheduled to run unattended), inject `INotificationService` and call
`NotifyCallerAsync` — see [`send-notifications`](../send-notifications/SKILL.md) (it also covers the `IUnitOfWork.SaveAsync`
that a processor needs afterwards).

**`ObjectName` must equal the input Dto's simple name minus `Dto`, exactly** — e.g. `ImportNbuRatesDto` →
`ObjectName => "ImportNbuRates"`. This is not a style preference: `reference.md` §8 shows this is read at
runtime by the server-side authorization check, and the auto-generated Execute claim/policy (step 4 below) are
seeded from the *Dto name*, not from this string — a mismatch here silently makes the processor permanently
unauthorized for everyone.

## 3. Only if it needs to write registers directly (rare)

Most processors should create/submit documents instead (step 2). If a processor is a genuine administrative
operation that must bypass documents entirely (period close, recompute, data fix-up), **implement**
`IRegisterWritingDataProcessorBehavior<TInput,TResult>` instead of `IDataProcessorBehavior<TInput,TResult>` — an
interface to implement, not a base class to derive from (`reference.md` §6 has the exact contract and corrects
this from an earlier version of this skill). Only that interface's `OnProcessAsync` is handed an `ISubmitScope`
— `registers.GetWriter<TWriter>()` is still the only legal way to touch a writer, same rule as a document
Behavior. There is no existing `PersonalAccounting` consumer of this path yet — treat it as validated-but-unproven.

## 4. DI registration, Execute permission, and the Job Scheduler registry — all generated, nothing to do

`[KandraDataProcessorBehavior(typeof({Name}Dto))]` on the Behavior (step 2) is genuinely all that's needed —
**this is a correction from an earlier version of this skill, which claimed this step was manual; it is not**,
verified directly against the generated `GeneratedPersonalAccountingApplicationDiExtensions.g.cs`, which contains the
`AddDataProcessor<ImportNbuRatesBehavior, ...>()` call today. `Kandra.Generators.Di.Application`'s `EntityClaimsEmitter` also
auto-seeds a matching `Execute{Name}` permission claim server-side, and `Kandra.Generators.ClientLib`'s
`PolicyEmitter` generates the matching client-side policy — both from the same attribute (`reference.md` §8 —
unlike a Report, a DataProcessor genuinely gets this for free), and a separate generator adds the processor to a
`IDataProcessorRegistry` the Job Scheduler's processor picker reads, making it schedulable with zero extra code
(`reference.md`'s intro). **Schedulable is not scheduled**: the configuration cannot ship a schedule. An admin creates
one under Admin > Scheduler (`/admin/scheduler/jobs`: the processor, its input, the trigger, the user it runs as). A spec
that asks for "a daily job" gets the processor, a test of `OnProcessAsync`, and a README line saying how to schedule it.

The Refit client's prefix is **`/api/v1/processors/{Name}`** (`data/ClientLib.cs`), not `dataprocessors`. A wrong prefix
builds and starts fine; the page then fails with "An error occurred deserializing the response", because the unknown
`/api/...` path falls through to the WASM host's `index.html`. **If you're wiring a plugin alongside the processor**, that plugin's own composite
registration (e.g. `services.AddYourPlugin()`) is the one real manual DI line — see `create-plugin` step 5 — but
it has nothing to do with the data-processor mechanics itself.

## 5. No EF migration needed

Data processors are never persisted — nothing to migrate for the processor itself. (If it creates documents of a
type that doesn't exist yet, that's a separate `create-document` concern with its own migration.)

## 6. Blazor UI — generated, nothing to write; nav entry — one hand-edit

Nothing to write for the page itself (see the intro above and `reference.md` §5 for exactly what's read from
which attribute, the generated route/policy/title, and a stale doc comment on `DataProcessorPageBase.Policy`
worth knowing about before you go looking for a `"...Behavior"`-suffixed policy string that doesn't exist). The
processor still needs a nav entry to be reachable from the sidebar — use
[`add-interface-node`](../add-interface-node/SKILL.md): `InterfaceNodeDefinition.Entity(typeof({Name}Dto))` in
`PersonalAccounting.Application/Navigation/PersonalAccountingInterfaces.cs`. **Unlike a Report, a DataProcessor `Entity` node gets
a real auto-derived permission check** (step 4's claim/policy) — nothing extra to configure for that, just add
the node (`reference.md` §8 has the full mechanism if you want to confirm rather than trust).

## 7. Refit client interface (hand-written declaration — no generator for this piece yet)

Copy `data/ClientLib.cs` to `{Config}.ClientLib.Common/ApiClients/I{Name}ApiClient.cs`, fill in placeholders.
`[PathPrefix]` must match `[KandraDataProcessorForm(Name = "...")]`'s value exactly. The body is empty —
`IDataProcessorApiClient<TInput,TResult>` already declares the full `NewAsync`/`ExecuteAsync` set; Refit's own
source generator implements it at compile time. Everything past the declaration — bridging it to the generic
interface, and registering the `IDataProcessorService<,>` wrapper the generated page actually injects — is
generated by `Kandra.Generators.ClientLib`; `reference.md` §7 has the exact emitted DI calls.

## 8. Localization

Add the processor's own `[KandraDataProcessorForm(Name = "...")]` value (e.g. `"ImportNbuRates"`) plus every
`[Caption]` key referenced by its input/result fields to your configuration's `*.Localization` project's
resx files (English required, uk/ru optional; see [`localize-configuration`](../localize-configuration/SKILL.md)). That one processor-name key does double duty — it's both the generated page's title and
its nav Entity node's default caption (`reference.md` §5/§8/§9). `"NotAuthorized"` is already an engine-level
key; `"Execute"` is **not** — check your configuration's own resx already has it (`PersonalAccounting`'s does, from
`ImportNbuRates`) before assuming it's free.

## A file field: consume it, or promote it

A `[FilePicker]` field is a `Guid?` blob reference to a fresh upload, and a fresh upload is **ephemeral**: it expires
after the upload TTL unless something attaches it to an owner. Two patterns, pick by what the file is for:

- **Consume** it: read it once and let it expire. `ImportData` does this (`IBlobService.OpenReadAsync`, nothing
  more). Right for an import, where the file is only an input.
- **Promote** it: keep it permanently. `CompanySettings` does this for the company logo. Validate the bytes first
  (the file's own PNG/JPEG signature and a size cap, never the client's content type or file name), then attach it with
  `IBlobService.SyncEntityReferencesAsync(ownerTypeId, ownerId, ids, ct, entityTag)`, which clears `ExpiresAt`. Pass
  **every id that must stay**, because an id left out is released: for a history-aware constant that is every logo ever
  set (from `IConstantsManageService.GetHistoryAsync`) plus the new one, so a reprint of an older document still finds
  the logo valid on its date. A rejected *new* id (unknown, tombstoned, attached elsewhere) is an error to show the
  user; a rejected older id is not. Write the blob reference before the other fields, so a stale upload does not leave
  the form half saved, and write the constant only after the sync succeeded.

`CompanySettings` is also the pattern for a settings form over several constants: `OnNewAsync` fills it from the
values valid now, `OnProcessAsync` writes only the values that changed, as new dated values from an effective-date
field (first on the form). The input Dto's `[Description]` should say it is the intended editor of the logo constant.

## 9. Verify

`dotnet build PersonalAccounting.slnx`, then inspect
`*.Client.MudBlazor/Generated.Net/Kandra.Generators.Ui.Blazor/Kandra.Generators.UiFormGenerator/{Name}DtoProcessor.g.cs`
directly if the generated page doesn't look right (per the engine's `inspect-generated-output` skill) — note the
filename carries the `Dto` suffix, unlike a Report's generated file. Also check
`GeneratedPersonalAccountingApplicationDiExtensions.g.cs` for the `AddDataProcessor<>()`/`AddRegisterWritingDataProcessor<>()`
call and `GeneratedPersonalAccountingEntityClaimsBuilder.g.cs` for the seeded `Execute{Name}` claim if authorization
doesn't seem to be taking effect. `dotnet test PersonalAccounting.slnx`, then run the processor live in the browser: it
appears in the sidebar where you added it (step 6), Execute is blocked for a user without the claim, a
successful run shows the expected result fields, and any side effects (documents created, registers moved) are
correct.

## If something here doesn't match the engine

`reference.md` is a point-in-time snapshot of engine source, not a live view — if a build error contradicts a
signature it or these templates show, the code wins (this includes doc comments inside the engine itself, at
least one of which — `DataProcessorPageBase.Policy` — is confirmed stale, see `reference.md` §5). Re-verify by
grepping the type/attribute name under `src/KandraCore/Kandra.Attributes/Application/KandraDataProcessor*.cs`,
`Kandra.Forms/DataProcessors/`, `Kandra.Application.Abstractions/Behaviors/`, `Kandra.Application/Services/
DataProcessors/`, `Kandra.ClientLib.Common/{ApiClients,Services/Crud}/`, `Kandra.ClientLib.MudBlazor/Shared/
Pages/DataProcessorPageBase.cs`, or `Kandra.Generators.Ui.Blazor/DataProcessorUiEmitter.cs`, then fix
`reference.md`/`data/` so the next person doesn't hit the same surprise.
