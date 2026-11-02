---
name: create-handler
description: Add a new remote handler (an independent, keyed backend endpoint for a mid-edit server round-trip - price lookup, discount recalc, payment-state check) to a Kandra configuration. Use when the user asks for a mid-edit lookup, a field that recomputes from the server as the user types/selects, or any "call the server, get a value back, merge it into the in-progress edit" screen.
---

# Creating a handler

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code - this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

A handler is a standalone backend RPC endpoint - **not** a document extension, not tied to any entity kind. It
answers a request made while a DTO is still being edited (it may never be saved), outside any transaction, before
Save/Submit (`docs/kandra-document-handlers-design.md` §1-§2). Typical uses: looking up a price/discount for a line as
the user picks an item, checking payment state against an external system. If what you need instead is a lifecycle hook
around an already-persisted entity, you want a Document/Dictionary Behavior's `OnNew`/`OnSave`/`OnSubmit` hooks, not a
handler - see `create-document`.

**This skill is the server half only:** the request/response Dtos, the marker, the behavior and the client interface.
The *client* half - when the call is made, how the request is filled from the form and how the response is applied - is
plain code in the form's UI behavior: inject `IHandlerApiClient<TIn,TOut>` into the behavior (or a helper it composes) and
call it. See [`create-ui-behavior`](../create-ui-behavior/SKILL.md). If what you need never calls the server at all (a row
total, a document sum, a derived field), you do not need a handler - it is just a method of the UI behavior.

This recipe is written against **this repo's one reference configuration, `PersonalAccounting`**; substitute your own
configuration's root project names if you're working in a different one. Concrete, currently-working examples:
`PriceLookupHandler` (`PersonalAccounting.Forms/Handlers/PriceLookup.cs`, `PersonalAccounting.Application/Handlers/PriceLookup.cs`), called
by the `LinePrices` helper (`PersonalAccounting.Forms/Common/ItemLines.cs`) from `WaybillUiBehavior`/`InvoiceUiBehavior`, and
`CounterpartyBankAccountPickHandler`, called straight from `BankPaymentUiBehavior`. Read one end-to-end before starting, and
read `docs/kandra-document-handlers-design.md` for the design rationale. A handler is reusable across document types by
design - check whether an existing handler already does what you need (matching request/response shape, different call
sites) before writing a new one; the Waybill/Invoice price lookup is one handler serving both.

## 1. Request/response Dtos + marker class (`{Config}.Forms/Handlers/{Name}.cs`)

```csharp
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Forms.Handlers;

namespace {Config}.Forms.Handlers;

public sealed class {Name}RequestDto
{
    // whatever the server needs to compute the answer - keep it minimal, not a whole document
}

public sealed class {Name}ResponseDto
{
    // whatever the caller needs back
}

[KandraHandler(Name = "{Name}")]
[PermissionsFrom(typeof({SourceDto}), ApplicationAction.{Action})]
public sealed class {Name}Handler : IHandler<{Name}RequestDto, {Name}ResponseDto>;
```

- `Name` on `[KandraHandler]` must be **globally unique** across the whole system - the generator rejects duplicates
  (`KANDRAGEN009`-`011`).
- `IHandler<TIn,TOut>` is a pure marker with no members, and the marker class carries **no client logic**: no
  `FillRequestFrom`/`ApplyResponseTo`/`HandleAsync`. Filling the request and applying the response are the caller's code
  (the UI behavior), where the guard paths and the sequencing around the call also live.
- `[PermissionsFrom(typeof(SourceDto), ApplicationAction.Action)]` is optional but is how a handler borrows an existing
  entity's permission instead of standing up its own claim - repeatable (`AllowMultiple = true`) if the check should
  require more than one (the combination is AND). If the handler is reused across documents and no single permission fits
  (the price lookup), omit it: the base `[Authorize]` on every handler controller still applies.
- If a direction genuinely carries no payload, use `Kandra.Forms.Handlers.EmptyHandlerDto` instead of inventing an empty
  class.

## 2. Behavior (`{Config}.Application/Handlers/{Name}.cs`)

```csharp
using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Attributes.Application;
using {Config}.Forms.Handlers;

namespace {Config}.Application.Handlers;

[KandraHandlerBehavior(typeof({Name}Handler))]
public sealed class {Name}Behavior(/* constructor-inject whatever reader services you need */, IAuthorizationChecker authorizationChecker)
    : IHandlerBehavior<{Name}RequestDto, {Name}ResponseDto>
{
    public async Task<{Name}ResponseDto> HandleAsync({Name}RequestDto request, CancellationToken cancellationToken)
    {
        authorizationChecker.AuthorizeFrom(typeof({Name}Handler)); // only if [PermissionsFrom] is present
        // do the actual lookup - constructor-inject ordinary reader services directly
        // (IInfoRegisterReader<T>, IEntityRepository<T>, ...); handlers are read-only by convention and
        // never receive an ISubmitScope - that type only gates register *writer* resolution inside a
        // document's OnSubmitAsync/OnUnsubmitAsync/OnDeleteAsync
        return new {Name}ResponseDto { /* ... */ };
    }
}
```

`TIn`/`TOut`/`Name` are all deduced from the marker via `[KandraHandlerBehavior(typeof({Name}Handler))]` - never
duplicated on the behavior attribute itself.

## 3. Client interface (`{Config}.ClientLib.Common/ApiClients/I{Name}ApiClient.cs`)

```csharp
using Kandra.Forms.ApiClients;
using {Config}.Forms.Handlers;

namespace {Config}.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/handlers/{Name}")]
public interface I{Name}ApiClient : IHandlerApiClient<{Name}RequestDto, {Name}ResponseDto>
{
}
```

This closed interface is **hand-written**, same as every other kind's closed client (`IWaybillsApiClient`, etc.) -
Refit's own compile-time generator can't see a source-generated interface, so only the DI registration for it is
generated, never the interface itself.

## 4. What's generated automatically - don't hand-write these

- The controller (`Kandra.Generators.Api`'s `HandlerControllerEmitter`) - one per discovered `[KandraHandler]`, route
  `api/v{version}/handlers/{Name}`, single `[HttpPatch]` action, resolves the behavior via `[FromKeyedServices("{Name}")]`.
- Server-side keyed DI registration (`Kandra.Generators.Di.Application`) - emits
  `services.AddHandler<{Name}Behavior, {Name}RequestDto, {Name}ResponseDto>("{Name}");` into the configuration's generated
  application-entities block.
- Client-side DI registration (`Kandra.Generators.ClientLib`) - registers your hand-written `I{Name}ApiClient` **and
  bridges it to the generic `IHandlerApiClient<{Name}RequestDto, {Name}ResponseDto>`**, so a UI behavior asks for the
  generic interface and never needs the closed interface's name.

Generated pages do **not** inject the client or call the handler - that is the UI behavior's job. Inspect the emitted
output under `Generated.Net/` (per the `inspect-generated-output` skill) rather than trusting the build succeeded -
confirm the controller and both DI registrations exist for your handler's `Name`.

## 5. Call it from the form (the client half)

In the form's UI behavior, or in a plain helper class it composes (register helpers that hold per-form state as
`Transient`), inject the generic client and call it:

```csharp
public sealed class {Source}UiBehavior(IHandlerApiClient<{Name}RequestDto, {Name}ResponseDto> lookup) : DocumentUiBehavior<{Source}Dto>
{
    public async Task OnSomethingChanged(CancellationToken cancellationToken)
    {
        // guard empty/no-op inputs first and return without calling the server - clear stale values on an empty key
        // field (see LinePrices / BankPaymentUiBehavior.OnCounterpartyChanged for the shape of these guards)
        var response = await lookup.HandleAsync(new {Name}RequestDto { /* from Record */ }, cancellationToken);
        Record.SomeField = response.Value;   // mutate the existing Record/line in place - never reassign it
    }
}
```

and point a property at it with `[OnChange(nameof({Source}UiBehavior.OnSomethingChanged))]`. Mutating `Record` in place
(never replacing it) keeps in-flight UI-only state: selected table row, `EditContext` validation state, edits to fields
the method didn't touch. Data the *form* needs but the document must not carry (an item's category that decides which
line cells show) goes into the behavior's or helper's private state, not onto the Dto (`ItemInfoCache` is the worked
example). Full rules - signatures, `CancellationToken`, helpers, DI lifetimes, testing - in
[`create-ui-behavior`](../create-ui-behavior/SKILL.md).

## 6. No EF migration needed

Handlers are never persisted - nothing to migrate for the handler itself. (If it reads a register/dictionary that doesn't
exist yet, that's a separate concern with its own migration.)

## 7. Verify

`dotnet build Kandra.slnx` (check the generated `{Name}HandlerController.g.cs` exists and the client DI registration is
present), `dotnet test Kandra.slnx`, then exercise it from the browser: trigger the field change, confirm the round-trip
populates the expected value(s), and confirm the empty/no-op guard paths clear stale values without hitting the server
(check network requests). If `[PermissionsFrom]` is present, also confirm an unauthorized caller is rejected -
`HandlerControllerBase`'s base `[Authorize]` and the `[PermissionsFrom]` check are separate and both apply.

## Pick handlers: the server returns candidates, the user chooses

Some handlers do not compute one value to apply - they list what the user may choose from (a counterparty's bank accounts, the unpaid
documents of a counterparty, the batches in stock). That is still an ordinary handler (sections 1-5); only the response and the
client step differ:

1. **Response** derives `PickResponseDto<TCandidate>` (`Kandra.Forms.Picking`) - a concrete, named class, not the generic type:
   `public sealed class {Name}PickResponseDto : PickResponseDto<{Name}CandidateDto>;`. The behavior fills `response.Candidates`.
2. **Candidate Dto** is small and flat, with display text already resolved (a bank *name*, not an id). Its dialog columns are the
   properties carrying `[GridColumn(Order = n, WidthPercent = n)]`, headed by `[Caption("Key")]`; nothing is generated or hand-written. Implement
   `IPickCandidate` (`bool IsDefault`) when one candidate can be the preferred one.
3. **Client**: the form's UI behavior injects `IHandlerApiClient<TIn,TOut>` and `IPickService`, calls the handler and hands the response to the
   service, then copies what it needs from the chosen candidate into its record (a **copy**, not a reference - the document keeps the requisites
   it was made with):

```csharp
var response = await accounts.HandleAsync(new {Name}PickRequestDto { OwnerId = Record.OwnerId }, ct);

// After another field changed: no dialog unless the choice is ambiguous (one candidate, or exactly one IsDefault; none = silent null).
Record.Iban = (await picker.PickAsync(response, cancellationToken: ct))?.Iban;

// An explicit button: always the dialog.
if (await picker.PickAsync(response, new PickOptions { Mode = PickMode.Always }, ct) is { } chosen) { Record.Iban = chosen.Iban; }

// Several at once (e.g. unpaid documents into settlement lines): checkboxes + Ok.
foreach (var row in await picker.PickManyAsync(response, ct: ct)) { Record.Lines.Add(...); }
```

4. **The button** is `[OnClick(nameof({Name}UiBehavior.OnPick), Icon = "AccountBalance", Tooltip = "ResxKey")]` on the plain `string` field
   the value goes into; the method is a normal behavior method (`()`, or `(TLine line)` on a line field).

Worked example: `CounterpartyBankAccountPickHandler` (`PersonalAccounting.Forms/Handlers/CounterpartyBankAccountPick.cs`, server side in
`PersonalAccounting.Application/Handlers/CounterpartyBankAccountPick.cs`), used by `BankPaymentUiBehavior`/`BankReceiptUiBehavior` (`OnCounterpartyChanged`,
`OnPickIban`). A test double for `IPickService` should apply `PickAuto.TryResolve` for the silent cases (see `BankUiBehaviorTests`).
