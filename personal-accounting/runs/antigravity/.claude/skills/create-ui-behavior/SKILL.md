---
name: create-ui-behavior
description: Add or change the UI logic of a form in a Kandra configuration - what happens when a field is edited, which fields/tabs/sections/grids are shown, what runs when the form opens or before it saves. Use when the user asks for a client-side recalculation (row sum, document total), a dependent field that clears or fills, a field that hides for some values, a server lookup from a form, or anything else a form does on its own while it is open.
---

# Creating a UI behavior

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code - this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

A **UI behavior** is the one class that holds all UI logic of one form - the way a 1C form module does. Every
form-bearing Dto (Document, Dictionary, a Report's filters Dto, a DataProcessor's input Dto) names **one** behavior with
`[UiBehavior(typeof(...))]`; the behavior's instance methods are then named by attributes on the Dto's properties. To see
what the Waybill form does, read `WaybillDto` and `WaybillUiBehavior` in `PersonalAccounting.Forms/Documents/Waybill.cs` - nothing
else.

It is **not** the place for server-side lifecycle hooks around a persisted entity (`OnNew`/`OnSave`/`OnSubmit` - those are
a Document/Dictionary *Behavior*, see `create-document`), and it never touches the database itself: anything only the
server knows is a remote handler it calls (see [`create-handler`](../create-handler/SKILL.md)).

## 1. Name the behavior on the Dto, write it next to the Dto

```csharp
using Kandra.Attributes.Layout;
using Kandra.Forms.UiBehaviors;

[UiBehavior(typeof(WaybillUiBehavior))]
public class WaybillDto : DocumentDto
{
    [OnChange(nameof(WaybillUiBehavior.OnPriceTypeChanged))]
    public Guid PriceTypeId { get; set; }

    [OnLineRemove(nameof(WaybillUiBehavior.OnLineRemoved))]
    public ICollection<WaybillLineDto> Lines { get; set; } = [];
}

public class WaybillLineDto : IPricedItemLine           // a line Dto has NO [UiBehavior] of its own
{
    [OnChange(nameof(WaybillUiBehavior.OnItemChanged))]
    public Guid ItemId { get; set; }

    [Visible(nameof(WaybillUiBehavior.ShowSerialNumber))]
    public string? SerialNumber { get; set; }
}

public sealed class WaybillUiBehavior(ItemInfoCache items, LinePrices prices) : DocumentUiBehavior<WaybillDto>
{
    public async Task OnItemChanged(WaybillLineDto line, CancellationToken cancellationToken)
    {
        await items.LookupAsync(line.ItemId, cancellationToken);
        if (!items.CarriesBatchData(line.ItemId)) line.SerialNumber = null;
        await prices.RefreshAsync(Record.PriceTypeId, Record.Lines, line, cancellationToken);
        RecalcLine(line);
    }

    public bool ShowSerialNumber(WaybillLineDto line) => items.CarriesBatchData(line.ItemId);
    // ...
}
```

- **The class lives in the same file as the Dto** (in `{Config}.Forms`, which the client references). It must be
  `public`, non-abstract, and derive from the base of the Dto's kind **with `TDto` = the Dto that carries the attribute**:
  `DocumentUiBehavior<TDto>`, `DictionaryUiBehavior<TDto>`, `ReportUiBehavior<TDto>` (the filters Dto),
  `DataProcessorUiBehavior<TDto>` (the input Dto). Anything else is `KANDRAUI029`.
- **Hierarchical dictionary:** declare `[UiBehavior]` on the abstract root Dto, with `TDto` = the root. The folder form
  and the leaf form each get their own instance; `Record` is typed as the root, so cast when a method needs the leaf's own
  members. On a folder/leaf subclass it is `KANDRAUI029`.
- **Client-safe:** `Kandra.Forms` is shared by every UI stack (Blazor today, Avalonia later). A behavior must not reference
  MudBlazor or ASP.NET Core components.
- The behavior reads and mutates the document through the inherited **`Record`** - the Dto is never a method parameter.

## 2. Which attribute names which method, and the signatures

| Attribute | On | Method shape |
|---|---|---|
| `[OnChange(nameof(B.M))]` | a root field | `()` |
| `[OnChange(nameof(B.M))]` | a field of a table part's line Dto | `(TLine line)` |
| `[Visible(nameof(B.M))]` | a root field, or a table-part collection property (hides the whole grid) | `()`, returns `bool` |
| `[Visible(nameof(B.M))]` | a field of a line Dto (hides that cell in the rows the method rejects) | `(TLine line)`, returns `bool` |
| `[FormTab("K", Visible = nameof(B.M))]`, `[FormSection("K", Visible = nameof(B.M))]` | the Dto class | `()`, returns `bool` |
| `[OnLineRemove(nameof(B.M))]` | a table-part collection property (`[TablePart]` or `Lines`) | `(TLine line)` - the removed row |
| `[OnLineAdd(nameof(B.M))]` | a table-part collection property (`[TablePart]` or `Lines`) | `(TLine line)` - the row the Add button just appended (it is already in the collection, so the method can look at its siblings) |
| `[OnClick(nameof(B.M), Icon = "...", Tooltip = "Key")]` | a plain `string` text field, root or line | `()` / `(TLine line)` - an icon button at the end of the field was pressed; typically a pick (see [`create-handler`](../create-handler/SKILL.md), "Pick handlers"). Not shown on a read-only form |

- Instance methods only, `public`, not generic. There are **no static rules**.
- `[OnChange]` and `[OnLineRemove]` methods may add a **trailing `CancellationToken`** and return `void`, `Task` or
  `ValueTask` (the generated form awaits the async ones). `[Visible]` methods are synchronous and return `bool`.
- `TLine` is that table part's line type (a base type or interface of it is accepted too). A line Dto is not tied to one
  parent: **the method is looked up on the behavior of whichever root owns the table part**. A line Dto shared by two
  documents needs the method on both behaviors (otherwise `KANDRAUI027` for the one that lacks it).
- **Exactly one `[OnChange]` per property.** The method does the sequencing in plain C# - no attribute order to depend on:
  item picked -> look it up -> clear the serial -> fetch the price -> recompute the row -> recompute the total.
- **Only a user edit fires `[OnChange]`.** Assigning a property from code - including from another behavior method - does
  not. A method that changes a dependent value must call the dependent logic itself (`RecalcLine(line)` above).
- Build errors: `KANDRAUI026` (a method is named but the Dto - or the owning root of a line Dto - has no `[UiBehavior]`),
  `KANDRAUI027` (no such method on the behavior), `KANDRAUI028` (wrong signature - the message lists the accepted
  shapes), `KANDRAUI029` (invalid behavior type), `KANDRAUI030` (warning: `[UiBehavior]` on a line Dto, ignored).

## 3. Lifecycle hooks (virtual methods, not attributes)

Override on the kind's base class:

| Hook | Kinds | Runs |
|---|---|---|
| `OnOpenAsync(CancellationToken)` | all | once per record, after `Record` is set - for a new record and an existing one alike |
| `OnBeforeSaveAsync(CancellationToken)` | Dictionary, Document | after the form validated, right before the save/submit is sent. It does **not** cancel the save - rejecting a record is the validator's job |
| `OnBeforeRunAsync(CancellationToken)` | Report | before each run of the report |
| `OnBeforeExecuteAsync(CancellationToken)` | DataProcessor | after the input validated, before the processor executes |

`WaybillUiBehavior.OnBeforeSaveAsync` recomputes the total once more as a safety net. If the form's record instance
changes, `Record` is set again and `OnOpenAsync` runs again.

**Worked example - prefetch on open (`GoodsReceiptUiBehavior`).** Visibility rules read data the form does not hold yet:
a service item hides its serial number and expiration date, but an opened document has not looked its items up. Fetch
them once, in `OnOpenAsync`:

```csharp
public override ValueTask OnOpenAsync(CancellationToken cancellationToken) =>
    items.PrefetchAsync(Record.Lines?.Select(l => l.ItemId), cancellationToken);
```

- **One request for the whole document.** The helper takes the list, drops duplicates, empty ids and items it already
  holds, and sends nothing when none is left (`ItemInfoCache.LookupAsync(IEnumerable<Guid>, ...)`). The single-item
  lookup calls it, so there is one code path.
- **Read only.** The hook looks things up and never changes `Record`: a hidden cell keeps its value, a read-only View page
  stays read-only and an opened document does not look edited. Clearing a value belongs to the user's `[OnChange]`.
- **Fail open.** `OnOpenAsync` is awaited from the form's parameter-set step, so an exception from it breaks the page. The
  prefetch only refines what is shown, so `PrefetchAsync` swallows a cancellation (the form closed first) and any other
  failure; the form opens with the cells shown, as before. A lookup the user triggers (`OnItemChanged`) is not wrapped and
  still throws. A behavior has no UI seam to report the failure; keep any such wrapper to prefetches like this one.
- The form re-renders by itself when the awaited hook finishes, so the cells change without user input.

## 4. State, dependencies and helpers

- **State is private fields of the behavior.** One instance per open form: an item's attributes a lookup fetched, a UI
  toggle - they die with the form and are never persisted or sent anywhere. Nothing that must survive Save belongs there -
  put that on the Dto.
- **Dependencies come from client DI through the constructor.** The engine creates the behavior with
  `ActivatorUtilities`; the behavior itself is **not registered**. Never resolve a behavior as a scoped service - in
  Blazor WebAssembly "scoped" is effectively application-wide, so two open forms would share one instance.
- **Shared logic is a plain helper class** (`ItemInfoCache`, `LinePrices` in `PersonalAccounting.Forms/Common`), composed in the
  constructor. Register helpers that **hold per-form state as `Transient`** in the configuration's client services
  (`PersonalAccounting.ClientLib.Common/ClientWmsServicesExtensions.cs`) so each form gets its own copy - a `Singleton` or scoped
  registration would leak one form's cache into another. Give helpers a small interface over the lines
  (`IItemLine`, `IPricedItemLine`, implemented by the line Dtos) so one helper serves Waybill, Invoice and GoodsReceipt.
- **A server lookup** is a remote handler: inject `IHandlerApiClient<TIn,TOut>` (`create-handler`) into the behavior or a
  helper, fill the request from `Record`, call it, apply the response. It is just an `await`.
- `IDisposable`/`IAsyncDisposable` on a behavior is honoured: it is disposed with the form.
- Keep guard paths in helpers exactly as deliberate as the original (`LinePrices`: an empty price type zeroes every line, an
  empty item zeroes that line, a missing price clears to 0; `ItemInfoCache`: an empty or already-cached item skips the
  round-trip).

## 5. Threading rule

Do **not** use `ConfigureAwait(false)` in a behavior or in any helper it calls, and change `Record` and its lines only on
the calling context. Blazor WebAssembly is single-threaded so nothing breaks there today, but a UI stack with a dedicated
UI thread (Avalonia) would see bound data mutated from a background thread.

## 6. Validators never depend on the behavior

A validator is shared by client and server and must not reference the behavior. When a validator must skip a field the form
hides, it states its own condition (`RuleFor(x => x.CounterpartyId).NotEmpty().When(x => x.Operation is ToSupplier or CustomerRefund)`).
A **hidden field keeps its value** - the generator never clears it. If switching the field that decides visibility must also
empty something (a grid, a dependent selector), the `[OnChange]` method of that field does it (`BankPaymentUiBehavior.OnOperationChanged`).

## 7. Nothing to wire by hand

The Blazor UI generator reads the attributes and emits the form: each form gets a `Behavior` member (and a `BehaviorToken`
for methods that take a `CancellationToken`), `[OnChange]` becomes an `onChanged:` callback on the field's editor,
`[OnLineRemove]` an `onLineRemoved:` callback on the grid, `[Visible]`/`Visible =` an `if (Behavior.M(...))` around the
editor, grid, cell, section or tab. Read the generated `{Dto}Form.g.cs` under the client project's `Generated.Net/` to see
exactly what each attribute became. A Dto with no `[UiBehavior]` and no `[Visible]` is generated exactly as before.

A hand-written page (login, dashboards, `Pages/Dev`) has no generated wiring - create the behavior yourself with
`UiBehaviorHost<TDto,TBehavior>` (`Kandra.ClientLib.Common.Ui`) or just call your helpers directly.

## 8. Test it without a browser

A behavior is a plain class: construct it with fakes, hand it a `Record`, call the methods. There is no component test host.

```csharp
var behavior = new BankPaymentUiBehavior(fakeIbanClient);
((IUiBehaviorRecordTarget<BankPaymentDto>)behavior).AttachRecord(new BankPaymentDto { Operation = BankPaymentOperation.CustomerRefund });
behavior.OnOperationChanged();
```

`PersonalAccounting.Tests/Documents/InventoryUiBehaviorTests.cs` and `BankUiBehaviorTests.cs` are the worked examples
(a fake `IHandlerApiClient<,>`, `Record` attached through `IUiBehaviorRecordTarget<TDto>`, assertions on the Dto).
`UiBehaviorHost<,>` itself - creation, `SetRecordAsync`, token, disposal - is covered by the engine's own tests.

## 9. No EF migration needed

A UI behavior is never persisted and never touches the server - nothing to migrate.

## 10. Verify

`dotnet build Kandra.slnx` (the `KANDRAUI026`-`030` errors above are the generator checking your names and signatures),
`dotnet test Kandra.slnx`, then exercise the form in the browser: edit the field, confirm the dependent values update, the
hidden fields hide, and that **opening an existing document from the list does not hide its data** (a helper that has not
looked an item up yet must default to "show").
