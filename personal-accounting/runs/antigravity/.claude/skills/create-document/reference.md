# Document engine — reference

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

Self-contained explanation of every engine type/attribute `SKILL.md` walks through — written so you don't need
to go read `src/KandraCore/*` source to use it. Exact signatures as of the last time this was verified against the
engine; if a build error contradicts something here, the code wins (see "If this reference goes stale" at the
bottom). Ready-to-copy `.cs` skeletons matching everything below live in [`data/`](data/) — pull the
`*.WithLines.cs` pair for a normal line-bearing document, the `*.Lineless.cs` pair for a header-only one like
`AccountTransaction`.

## What a Document actually is

The full-featured entity kind: persisted, has a form + grid, **tabular line sections** (most documents; not
mandatory — see §1's lineless case), and **posts/submits** — a document has an `IsActive` (posted) flag toggled
by explicit Submit/Unsubmit actions, and only `OnSubmitAsync`/`OnUnsubmitAsync`/`OnDeleteAsync` are allowed to
write register movements or chart-of-accounts ledger transactions (via `ISubmitScope`, §6) — never `OnSaveAsync`
or any other hook. That submit/unsubmit lifecycle plus the write-gating is the entire dividing line against a
Dictionary (`create-dictionary`'s own reference explains the reverse side of this line: "no posting, no register
writes" is what makes something a Dictionary instead).

A document is implemented across the same **three separate class-library projects** every entity kind in the
engine uses (see `create-dictionary`'s reference for the full plane-split rationale — Domain/Forms/Application,
same rules, same client/server boundary):

| Piece | Project | Base type | Carries |
|---|---|---|---|
| `*Base` entity (+ optional line class) | `{Config}.Domain` | `DocumentBase` | Pure EF model, zero UI/auth attributes. |
| `*Dto` (+ optional line Dto) | `{Config}.Forms` | `DocumentDto` | The attribute-rich UI/API contract the generators read — **also drives the generated Blazor UI**, see §12. |
| `*Behavior` | `{Config}.Application` | implements `IDocumentBehavior<T>` | Lifecycle hooks **plus** register/ledger writes (only inside the submit-scoped hooks). Mandatory even as a near-no-op. |
| `*Validator` | `{Config}.Forms` | `DocumentValidator<TDto>` | FluentValidation rules. Mandatory even if it just calls `base(...)`. |

Everything in that table is generator-discovered from attributes alone — nothing about *these four pieces* is
ever manually registered ("Nothing else to register by hand" in `SKILL.md`). Documents differ from dictionaries
in one major way worth knowing up front: **the Blazor UI (List/Add/Update/View/Form pages) is 100% generated
for a document — there is nothing to hand-write.** This is a real, comparatively recent capability
(`Kandra.Generators.Ui.Blazor.DocumentUiEmitter`) — an older skill/handoff/doc that talks about copying
`Pages/Document/GoodsReceipt/*.razor` is describing a state that no longer exists (those hand-written pages
were moved to `_HandWrittenReference/Document/GoodsReceipt/*.razor` when the generator went live and are kept
only as a historical reference, never copied from). See §12 for the full mechanism.

### Hand-maintained files (no generator, as of this writing)

Every one of these is either a **new file you add** (never an edit to a file someone else's document also
touches — the two split-partial-class conventions below exist for exactly this reason) or a real edit to an
existing shared file that the four generator-discovered pieces above don't by themselves take care of:

| What | File | Why it's manual |
|---|---|---|
| Type identity constant | New file `{Config}.Enums/PersonalAccountingTypeIds/PersonalAccountingTypeIds.{Name}.cs` — `PersonalAccountingTypeIds.cs` itself is never edited, see §7 | No generator mints/tracks GUIDs. Same convention as dictionaries; `PersonalAccountingTypeIds` already holds one entry per existing document. |
| Numbering bucket constant | New file `{Config}.Domain/Numbering/NumberingBuckets.{Name}.cs` — `NumberingBuckets.cs` itself is never edited, see §8 | Same — a bucket name is just a string the behavior chooses, nothing discovers it automatically. |
| Localization keys | Edit to `{Config}.Localization/*.resx` (English required, uk/ru optional) | Every `[Caption]`/title/nav resx key the Dto references, **plus** three keys the generated List/Add/Update pages read directly rather than through an attribute (`Add{Name}`, `{Name}AddedSuccessfully`, `{Name}UpdatedSuccessfully`) — see §9. |
| Refit client interface | New file `{Config}.ClientLib.Common/ApiClients/I{PluralName}ApiClient.cs` | Refit's generator implements the interface body, but nothing generates the interface *declaration* itself yet. See §11. |
| Nav entry | Edit to `{Config}.Application/Navigation/*Interfaces.cs` | Not derived from the Dto automatically — the node itself (`InterfaceNodeDefinition.Entity(typeof({Name}Dto))`) is one line, but it has to be inserted into the existing tree literal by hand. See `add-interface-node`. |

**Not in this table, unlike dictionaries: Blazor UI pages.** A document's List/Add/Update/View/Form pages,
its Submit/Unsubmit buttons, its Links/Register-Transactions/Account-Transactions View tabs, and its Print
button (gated automatically on whether the Dto carries `[SupportedPrintForm]`) are all emitted by
`Kandra.Generators.Ui.Blazor.DocumentUiEmitter` straight from the Dto's attributes — see §12. Skipping every
row in the table above still compiles and passes DI validation, but leaves the feature invisible or broken in
the browser (a missing resx key renders the raw key name; a missing nav entry means no sidebar link exists; a
missing Refit interface means the generated pages have no client service to call — they still compile, they
just have nothing to inject).

## 1. Domain entity base classes

### `DocumentBase`

`src/KandraCore/Kandra.Domain/Entities/Documents/DocumentBase.cs`

```csharp
public abstract class DocumentBase : ChangesInfo, IDocumentEntity
{
    public Guid Id { get; set; }
    public Guid DocumentTypeId { get; protected set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public virtual string Code { get; set; }
    public bool IsActive { get; set; }

    protected DocumentBase(Guid typeId) => DocumentTypeId = typeId;
}
```

- `ChangesInfo` supplies the same audit columns as `DictionaryBase` (`Created`/`Modified`/`CreatorUserId`/
  `ModifierUserId`/`IsDeleted`) — never redeclare them.
- `DocumentTypeId` is `protected set` — only settable through the constructor, which every concrete document
  must call as `base(TypeId)` (its own static `TypeId`, §7). `Date`/`IsActive` are engine-managed lifecycle
  fields — `IsActive` is flipped by the generated Submit/Unsubmit endpoints, never assign it directly in a
  behavior. `Code` is `virtual`, same "only `override` if this document's format genuinely needs its own
  `[MaxLength]`/`[Caption]`" rule as `DictionaryBase.Code` — but note `DocumentBase.Code` itself carries **no**
  `[MaxLength]` at all (unlike `DictionaryBase.Code`'s `[MaxLength(64)]`), so most documents should just inherit
  it unmodified and let the column stay unconstrained-length.
- No `Name` property — a document identifies itself by `Code` + `Date`, not a display name.

### `IEntityWithRows<TSelf>` and the `[KandraTablePart]` generator

`src/KandraCore/Kandra.Domain/Commons/IEntityWithRows.cs`:

```csharp
public interface IEntityWithRows<TSelf> : IRootEntity
{
    static abstract IEnumerable<Expression<Func<TSelf, object>>> RowNavigations { get; }
    void RemoveOrphans(TSelf fetched, IRowRemover remover);
}
```

**Every** document entity implements `IEntityWithRows<TSelf>` — this is mandatory infrastructure regardless of
whether the document actually has line collections
(`Kandra.Generators.Di.Persistence.PersistenceDiEmitter` unconditionally calls
`RegisterEntityWithRowsRepositories<T>()` for every `[KandraDocumentEntity]`-attributed type). What differs is
*how* `RowNavigations`/`RemoveOrphans` get implemented:

**With at least one `[KandraTablePart]` property** (the common case) — `Kandra.Generators.Equality.
TablePartGenerator` finds every property on the class attributed `[KandraTablePart]` and emits both members for
you, into a `Generated.Net/` partial-class file. You never write `RowNavigations`/`RemoveOrphans` by hand in
this case — just declare the class `partial` and mark the line-collection property:

```csharp
[KandraTablePart(TableName = "Doc_{Name}Lines")]
public IList<{Name}Line> Lines { get; set; } = new List<{Name}Line>();
```

emits (verbatim shape, for one `Lines` property):

```csharp
// <auto-generated/>
public partial class {Name}
{
    public static IEnumerable<Expression<Func<{Name}, object>>> RowNavigations { get; } = [x => x.Lines];

    public void RemoveOrphans({Name} fetched, IRowRemover remover)
    {
        remover.RemoveOrphans(Lines, fetched.Lines);
    }
}
```

A document may declare more than one `[KandraTablePart]` property (multiple table parts) — every one is
included in the generated `RowNavigations`/`RemoveOrphans` automatically, no extra wiring. Mark the matching Dto collections `[TablePart]` (§2) so the form renders a grid for each.

**With zero `[KandraTablePart]` properties** (a lineless document, e.g. `AccountTransaction`) — the generator
finds nothing to attach to and emits nothing. You must hand-implement both members as trivial no-ops yourself,
directly in the entity's own file:

```csharp
public static IEnumerable<Expression<Func<AccountTransaction, object>>> RowNavigations => [];
public void RemoveOrphans(AccountTransaction fetched, IRowRemover remover) { }
```

`[KandraTablePart(TableName = "...")]` (`Kandra.Attributes.Entities`, property-only) is also what tells
`Kandra.Generators.EfConfig` to map the line collection as a child table via the line type's own generated
`IEntityTypeConfiguration<T>` — the `TableName` there names the *line* table, distinct from the document's own
`[KandraDocumentEntity(TableName = ...)]`.

### Line type — no mandatory base class beyond `IEntityRow`

`src/KandraCore/Kandra.Domain/Commons/IEntityRow.cs`:

```csharp
public interface IEntityRow
{
    public Guid Id { get; set; }
}
```

That's the *only* engine-level requirement for a line class — a bare `Guid Id`. `PersonalAccounting` additionally
defines its own convenience interface, `PersonalAccounting.Domain.Commons.IOrderLine : IEntityRow` (`ItemId`/`Quantity`/
`Price`) for the common "order-shaped line" case (`GoodsReceiptLine`, `WaybillLine`) — this is a **configuration
convention, not an engine contract**. A line that doesn't fit that shape just implements `IEntityRow` directly
or a different domain-specific interface (`TransferLine` implements `IEntityRow` plus
`IInventoryAllocationSourceWithSerialNumber`, since a warehouse-to-warehouse move has no `Price`). Decide
per-document whether `IOrderLine` fits; don't force it onto a line that isn't really an order line. Every line
class also carries `[KandraGeneratedEquality]` (same mechanism as the document itself, §2) and a back-reference
FK + `[JsonIgnore]` nav property to its parent, by convention (see `GoodsReceiptLine.GoodsReceipt`).

## 2. Attribute vocabulary

All namespaces are under `Kandra.Attributes.*`. Grouped by what they mark; the shared base attribute classes
(`KandraEntityAttribute`, `KandraFormAttribute`, `KandraBehaviorAttribute`) are the same ones `create-dictionary`
documents — not repeated here beyond what's document-specific.

### Entity-side (Domain plane, `Kandra.Attributes.Entities`)

| Attribute | Target | Signature | Meaning |
|---|---|---|---|
| `[KandraDocumentEntity]` | class | inherited `TableName` (string?), `SkipGeneration` | Marks the entity as a document for `Kandra.Generators.EfConfig`/`.Di.Persistence`/`.Di.Application` discovery. |
| `[KandraTablePart]` | property | no params, `TableName` (string?) | Marks a line-collection property as a table-part — see §1. |
| `[KandraGeneratedEquality]` | class (must be `partial`), repeatable per file | no params | Generates `Equals`/`GetHashCode`/`ToString` over the class's own declared properties. Put it on **both** the document class and every line class. |
| `[TypeId(guidLiteralString)]` | class | `(string)` ctor | The Dto-side counterpart of the entity's `static Guid TypeId` — same value. Required on every document Dto (§7). |

### Form-side (Forms plane)

| Attribute | Namespace | Target | Signature | Meaning |
|---|---|---|---|---|
| `[KandraDocumentForm]` | `.Application` | class | `Name` (string, **required**, inherited), `ValidatorType` (Type, inherited), `QueryDtoType` (Type, own), `SkipGeneration` (inherited) | Marks the Dto as a document form. Same "`Name` is never convention-derived, always set it" rule as `[KandraDictionaryForm]`. |
| `[EditFormTitle]` / `[AddFormTitle]` / `[ViewFormTitle]` / `[ListFormTitle]` / `[NavigationName]` | `.Naming` | class | `(string value, bool isLiteral = false)` | Same resx-key-by-default convention as dictionaries — see §9. |
| `[Help]` | `.Naming` | class (inherited) | `(string pathOrUrl)` | Links the form to its documentation page: generated pages show a (?) button next to the title. A relative path is appended to the language's base from `Kandra:Client:HelpUrl`/`HelpUrls`; an absolute http(s) URL is used as-is; `#anchor`/`?query` kept. One per form (List/Add/Update/View share it); on a hierarchical dictionary put it on the abstract root Dto. Empty or non-http(s) value: `KANDRAUI032`. |
| `[Caption]` | `.Naming` | property | `(string value, bool isLiteral = false)` | Field/column display label. |
| `[ListFormColumn]` | `.Layout` | property (document header only) | `Sortable`/`Filterable` (bool, default `true`) | Column in the top-level document list/grid. |
| `[GridColumn]` | `.Layout` | property (**line Dto only**) | `Order` (int), `Visible`/`Sortable`/`Filterable` (bool, default `true`), `WidthPercent` (int) | Column in the embedded line grid inside the document form — **distinct from `[ListFormColumn]`**, which a line Dto never carries. This is the attribute every dictionary skips (dictionaries have no line grids). |
| `[TablePart]` | `.Layout` | property (document header Dto, `ICollection<TLine>`) | `AddCaption` (string, resx key of the "add row" button, default `AddLine`) | Marks a tabular-section collection — any property name, one grid per marked property in declaration order (a document can have several). Dto-side counterpart of the entity's `[KandraTablePart]`. With none marked, a single collection named `Lines` still works. `[Caption]` on it is the grid heading; `[OnLineRemove(nameof(B.Method))]` on it runs the UI behavior's method when a row is removed from that part. An unmarked collection of a class that isn't named `Lines` renders no grid and warns `KANDRAUI011`. |
| `[FormTab(key, isLiteral = false)]` / `[FormSection(key, isLiteral = false)]` | `.Layout` | class, repeatable | `Order` (both), `TabKey` + `Columns` (section) | Optional header layout: tabs and titled sections (resx-key captions; `isLiteral: true` skips localization). Declared on the Dto class. |
| `[TabRef(key)]` / `[SectionRef(key)]` | `.Layout` | property | `(string)` ctor | Places a header field on a tab / in a section. `[TabRef]` also works on a `[TablePart]` collection, putting that grid on a tab (a grid never sits in a section). Unplaced fields, the Date/Code header and unplaced grids stay on an implicit first tab (engine `General` key, shown only when there are other tabs). Same layout rules, resx keys and `KANDRAUI015`–`018` warnings as a dictionary form — see the `create-dictionary` skill reference. |
| `[UiBehavior(typeof(MyUiBehavior))]` | `.Layout` | class (document header Dto) | `(Type)` ctor | Names the document's **UI behavior** (`DocumentUiBehavior<TDto>` with `TDto` = the header Dto, in the same file): all UI logic of the form - recalculations, dependent fields, visibility, lookups, hooks that run when the form opens or before it saves. A **line Dto has none**: methods named on its properties are looked up on the behavior of the document that owns the table part. See the `create-ui-behavior` skill. |
| `[OnChange(nameof(B.Method))]` | `.Layout` | property (header field or line Dto field) | `(string)` ctor | Runs the behavior's instance method when the user edits the field: `()` on a header field, `(TLine line)` on a line field; optional trailing `CancellationToken`; `void`/`Task`/`ValueTask`. Exactly one per property - the method sequences the follow-up steps (look up, clear, recompute row, recompute total) itself. |
| `[OnLineRemove(nameof(B.Method))]` | `.Layout` | property (`[TablePart]`/`Lines` collection) | `(string)` ctor | Runs `Method(TLine removedLine[, CancellationToken])` after a row is removed from that part - typically the total recompute. |
| `[Visible(nameof(B.Method))]` | `.Layout` | property (header field, `[TablePart]` collection, line field) | `(string)` ctor | Shows it only while the behavior's `bool` method is `true`. Header field / `[TablePart]` (hides the whole grid): `()`. **Line field:** `(TLine line)` hides that cell in the rows the method rejects. |
| `Visible = nameof(B.Method)` | `.Layout` | named property of `[FormTab]` / `[FormSection]` | `string?` | `()` method of the behavior; the tab / section shows only while it is `true`. |
| `[NotEditable]` | `.Layout` | class or property | no params | Marks non-editable in generated UI — always paired with `[Calculated]` on a computed total/sum field. Named "NotEditable" specifically to avoid colliding with `System.ComponentModel.ReadOnlyAttribute`. |
| `[Calculated]` | `.Layout` | property | no params | Marks a property as computed rather than user-entered (e.g. line `Sum`, header `Total`). |
| `[Search(sourceDtoType)]` / `[Dropdown(sourceDtoType)]` | `.Editors` | property | see `create-dictionary` reference.md §2 | Same FK-picker attributes, used identically on document header/line fields (e.g. `GoodsReceiptDto.CounterpartyId`, `GoodsReceiptLineDto.ItemId`). |
| `[Required]` | BCL | property | — | Same nullability-drives-required convention as everywhere else in the engine. |
| `[Description]` | BCL (`System.ComponentModel`) | class or property (header Dto **and** line Dto) | `(string)` ctor | Semantic explanation of what the Dto/field means and how it's used — not a UI label (`[Caption]` covers that). Reflected by `Kandra.Mcp`'s `EntityFieldMetadataBuilder` and surfaced through the MCP server's `describe_entity_type`/`list_entity_types` tools as an AI agent's primary way to learn what an entity means; a line Dto shows up in that same schema via `DescribeTabularLines`, so it needs its own class-level `[Description]` too, not just the header's. **Mandatory on every Dto class (header and line)**; add on a property whenever its purpose isn't obvious from its name/caption alone. |

**UI behavior** works on a document form as in the `create-dictionary` reference ("UI behavior"): `[UiBehavior(typeof(XxxUiBehavior))]` on the header Dto, the behavior in the same file deriving from
`DocumentUiBehavior<XxxDto>`, its public instance methods named by `[OnChange]`/`[Visible]`/`[OnLineRemove]`/`Visible =`, `KANDRAUI026`-`030`. Document-specific: one behavior serves the header **and** all its lines/table
parts (a line Dto has no behavior of its own, and its `(TLine)` methods live on the owning document's behavior), `OnBeforeSaveAsync` is the place for a defensive recompute of a total right before a save or submit, a
`[TablePart]` carries `[Visible]` to hide the whole grid - so a document with **two grids over different referenced document types** (e.g. `WaybillSettlements` / `GoodsReceiptSettlements`, one entity table split in the
mapper) shows only the grid the header's `Operation` calls for - and, because a hidden grid keeps its rows, the `[OnChange]` method of the field that decides it empties the grids the new operation hides (the
validator would otherwise reject rows nobody can see; the validator states its own condition and never references the behavior). Shared logic (an item cache, a price helper) is a plain helper class composed in
the behavior's constructor over a small line interface (`IItemLine`/`IPricedItemLine`) and registered **Transient**. See `GoodsReceiptDto`/`WaybillDto`/`TransferDto`/`BankPaymentDto` with their `*UiBehavior` classes
for worked examples: a remote handler's answer lands in `ItemInfoCache`, `ShowSerialNumber(line)` hides the serial-number / expiration-date cells for a service, `OnItemChanged(line)` empties them.

Every attribute above targets a class/property that physically lives in `{Config}.Forms` — the Dto, the line
Dto. `[KandraDocumentForm]`'s own C# namespace happens to be `Kandra.Attributes.Application` (same for every
`Kandra*Form` attribute — that's just where the attribute *type itself* is declared in the engine), which is
easy to misread as "this is an Application-plane concern." It isn't: what matters is what it's *applied to*,
and `[KandraDocumentForm]` is applied to the Dto, so it's a Forms-plane fact.

### Behavior-side (Application plane)

The one attribute that genuinely does belong to the Application plane — it decorates the *Behavior* class,
which lives in `{Config}.Application`, never `{Config}.Forms`:

| Attribute | Namespace | Target | Signature | Meaning |
|---|---|---|---|---|
| `[KandraDocumentBehavior(formDtoType)]` | `.Application` | class (the Behavior, in `{Config}.Application`) | `(Type)` ctor | Links a Behavior class to its Dto. The engine reflects the behavior's own closed `IDocumentBehavior<TEntity>` generic to find the entity type. |

### Document-specific cross-cutting attributes (each has its own dedicated skill — summarized here only so
you recognize them; don't reach for these unless the document actually needs the behavior)

| Attribute | Namespace | Applied to (plane) | Meaning | See |
|---|---|---|---|---|
| `[SupportedPrintForm(formType)]` / `[SupportedExportFormats(...)]` | `.Printable` | Dto class (Forms) | Declares a PDF/CSV print form for the document; gates the generated View/Update page's Print button on/off automatically (§12) — omit both entirely for a document with no print form, don't pass `onPrint: null` anywhere yourself. | `create-print-form` |
| `[UiBehavior(behaviorType)]` + `[OnChange]`/`[OnLineRemove]`/`[Visible]` | `.Layout` | Dto class or property (Forms) | Names the document's UI behavior and wires its methods to field/row events - a client-side recompute (row sum, document total), a server round-trip through an injected `IHandlerApiClient<,>`, visibility. See the rows above. | `create-ui-behavior`, `create-handler` |
| `[SourceFor(targetDtoType)]` | `.Application` | Dto class (Forms) — e.g. `InvoiceDto`, not the Behavior, despite the `.Application` namespace | Declares this document as a valid "source" to prefill a new instance of `targetDtoType` via the View page's generated "Create {Type}" button — feeds `sourceDocumentId` into the target's `OnNewAsync` (§5). Repeatable. | Document Links — no dedicated skill yet; see `Invoice`→`Waybill` for the real example. |
| `[RegisterTransactions(registerRowType)]` | `.Application` | Behavior class (Application) | Repeatable. Purely declarative metadata for the register-transactions viewer generator — doesn't itself cause any register writes, just documents which register row type(s) this document's `OnSubmitAsync` touches. | `create-register` |
| `[AccountTransactions]` | `.Application` | Behavior class (Application) | Class-level marker, no data. Same declarative role as `[RegisterTransactions]` but for the chart-of-accounts posting subsystem. | `post-to-accounting` |
| `[AccountDialog]` / `[SubcontoEditor(...)]` | `.Editors` | Dto property (Forms) | Account-code + subconto picker fields, for a document where the account/subconto are a runtime user choice rather than fixed in code (e.g. `AccountTransaction`). | `add-account-subconto-field` |

Note the split within this table: `[SourceFor]` decorates the **Dto** despite sharing the `.Application`
C# namespace with `[RegisterTransactions]`/`[AccountTransactions]`, which decorate the **Behavior** — the
namespace a `Kandra.Attributes.*` type is declared under is not a reliable signal for which plane it's applied
to; only the "Applied to" column (or, ultimately, checking a real `[Attribute]` usage in source) tells you that.

## 3. Dto base classes

`src/KandraCore/Kandra.Forms/Documents/DocumentDto.cs`:

```csharp
public class DocumentDto : IDocumentDto
{
    [Required] [Caption("Id")] public Guid Id { get; set; }

    [Required] [Caption("Date")] [DateEditor(DateEditorKind.Date)] [ListFormColumn(Filterable = false)]
    public DateTime Date { get; set; }

    [Required] [Caption("Code")] [ListFormColumn]
    public virtual string Code { get; set; }

    [Required] [ListFormColumn]
    public bool IsActive { get; set; }

    public int? Version { get; set; }
}
```

`src/KandraCore/Kandra.Forms/Querying/DocumentQueryDto.cs`:

```csharp
public class DocumentQueryDto : PagedQueryDto, IDocumentQueryDto
{
    public DateRangeDto? DateRange { get; set; }

    /// Set on a Lookup call to resolve exactly one row (ignores Filters/Sort/paging); null on
    /// the ordinary /query grid call.
    public Guid? Id { get; set; }
}
```

(`PagedQueryDto` — same shared grandparent as the dictionary QueryDto, `Page`/`PageSize`/`Filters`/`Sort`.) A
concrete `{Name}QueryDto : DocumentQueryDto { }` is almost always an empty subclass — `DateRange`/`Id` already
cover the standard document list/lookup filtering; only add fields here if this document needs an extra
list-page filter beyond date range.

## 4. Validators

`DocumentValidator<T>` (`src/KandraCore/Kandra.Validators/Documents/DocumentValidator.cs`):

```csharp
public abstract class DocumentValidator<T> : AbstractValidator<T> where T : DocumentDto
{
    protected DocumentValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage(localizer["CodeRequired"]);
    }
}
```

Note this base only covers `Code` — unlike `DictionaryValidator<T>`, there's no `Name` to validate. Every field
requiredness beyond `Code` (header FKs, `Lines` non-empty, per-line quantity > 0, ...) is the concrete
`{Name}Validator`'s own job — see `GoodsReceiptValidator` in §10 for the typical shape, including
`RuleForEach(x => x.Lines).Must(...)` for line-level rules.

`{Name}QueryDtoValidator : PagedQueryDtoValidator<{Name}QueryDto>;` — same one-line pattern as dictionaries,
lives in the Application-plane file (next to the Behavior/Mapper), not the Forms-plane file.

## 5. Behavior contract

`src/KandraCore/Kandra.Application.Abstractions/Behaviors/IDocumentBehavior.cs`:

```csharp
public interface IDocumentBehavior<TEntity> where TEntity : class, IDocumentEntity
{
    ValueTask OnNewAsync(TEntity entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask OnOpenAsync(TEntity entity, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask OnSaveAsync(TEntity entity, bool isInsert, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask OnSubmitAsync(TEntity entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask OnUnsubmitAsync(TEntity entity, ISubmitScope registers, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask<PrintRenderOutput?> OnPrintAsync(TEntity entity, ExportFormat format, string? layoutTag, ILookupRepository lookup, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone, CancellationToken cancellationToken) =>
        new((PrintRenderOutput?)null);
}
```

Every hook defaults to a no-op via C# default interface implementation — override only what this document
actually needs, same "no-op is valid, the class+attribute must still exist" rule as dictionaries.

- **`OnNewAsync(entity, date, sourceDocumentId, ct)`** — three things happen here typically: default `Date` to
  `date ?? DateTime.UtcNow`, assign `Code` via the numbering system (§8), and — only if this document declared
  itself a target of `[SourceFor]` on some other Dto — resolve `sourceDocumentId` through
  `IDocumentReaderService` (pattern-matched to whatever concrete source type is expected, no-op gracefully if it
  doesn't match) to prefill fields from the source document. `sourceDocumentId` is null for an ordinary,
  unseeded new document — always handle that case.
- **`OnSaveAsync(entity, isInsert, ct)`** — fires before persistence on both insert and update. The right place
  for **server-authoritative recalculation** of anything a client might have left blank or computed wrong (line
  `Sum = Quantity * Price`, header `Total = Lines.Sum(l => l.Sum)`) — never trust a client-submitted calculated
  value. This is a plain lifecycle hook, not submit-scoped — never write a register or post to accounting here.
- **`OnSubmitAsync`/`OnUnsubmitAsync`/`OnDeleteAsync`** — the only three hooks that receive `ISubmitScope
  registers` and are therefore the only three allowed to write registers/post to accounting. See §6.
  `OnUnsubmitAsync` should exactly reverse `OnSubmitAsync` (usually just `UnpostAsync`, which the register
  engine implements as movement negation, and `IPostingService.UnpostAsync`). `OnDeleteAsync` handles "delete an
  already-unposted document" — the engine blocks deleting a still-posted (`IsActive`) document until it's
  unsubmitted first.
- **`OnPrintAsync`** — only meaningful if the Dto carries `[SupportedPrintForm]` (see `create-print-form`); the
  default "no print form" `null` return is correct for any document that doesn't print. Display names for
  header/line FKs (Counterparty, Warehouse, Item, ...) are resolved through `ILookupRepository`, not the
  entity's own nav properties — the CRUD service's plain `GetAsync` only loads `[KandraTablePart]` row
  navigations (see `EntityWithRowsRepository.IncludeForGet`), never arbitrary FK nav properties.

## 6. `ISubmitScope` — the only legal way to write a register or post to accounting

`src/KandraCore/Kandra.Application.Abstractions/Services/ISubmitScope.cs`:

```csharp
public interface ISubmitScope
{
    TWriter GetWriter<TWriter>() where TWriter : class, IRegisterWriter;
    TReader GetReader<TReader>() where TReader : class, IRegisterReader;
    IPostingService GetPostingService();
}
```

Register writer interfaces (`IBalanceRegisterWriter<,>`, `ITurnoverRegisterWriter<>`,
`IDocumentInfoRegisterWriter<>`) and `IPostingService` are registered as **keyed** DI services — none of them
can be constructor-injected anywhere in the codebase. `ISubmitScope` (handed to you as a parameter on
`OnSubmitAsync`/`OnUnsubmitAsync`/`OnDeleteAsync` only) is the sole resolver. `GetReader<TReader>()` is a
convenience — readers are ordinary, ungated scoped services (unlike writers), it just lets submit-hook code
reach them through the same handle without a separate constructor injection (see `Transfer`'s
`OnSubmitAsync`, which reads its own prior movements via `registers.GetReader<ITurnoverReader<...>>()` before
writing new ones).

The typical `OnSubmitAsync` shape for an inventory-moving document (see `GoodsReceipt` in §10 for the full
version):

```csharp
var writer = registers.GetWriter<IBalanceRegisterWriter<InventoryMovement, InventoryBalance>>();
var movements = /* build one InventoryMovement per line */;
await writer.PostAsync(entity.Id, entity.Date, movements, cancellationToken);
```

Swap `IBalanceRegisterWriter<InventoryMovement, InventoryBalance>` for whichever register(s) this document
actually posts to — if the register itself doesn't exist yet, see `create-register`. If this document should
also post debit/credit ledger transactions to the chart-of-accounts subsystem (a second, independent accounting
engine — never built on top of the register engine, don't try to unify them), see `post-to-accounting`; the same
`registers` parameter also gives you `registers.GetPostingService()`.

If a document doesn't move a register or post to accounting at all, it's still fine to leave
`OnSubmitAsync`/`OnUnsubmitAsync` as their default no-op — just don't skip declaring the behavior class itself,
and don't add `[RegisterTransactions]`/`[AccountTransactions]` if there's nothing to declare.

## 7. Identity: `PersonalAccountingTypeIds` convention (documents use the exact same mechanism as dictionaries)

Every document implements `IEntityWithTypeId` (directly, or transitively through `ISubconto` — see below) the
same way every dictionary does — `create-dictionary` reference.md §6 has the full explanation of why type
identities are centralized in `{Config}.Enums.PersonalAccountingTypeIds` and why that class is `partial`/one-constant-per-file.
**Documents already participate in that same split** — `PersonalAccountingTypeIds/` currently holds one file per existing
document (`PersonalAccountingTypeIds.GoodsReceipt.cs`, `PersonalAccountingTypeIds.Waybill.cs`, `PersonalAccountingTypeIds.Transfer.cs`,
`PersonalAccountingTypeIds.AccountTransaction.cs`, ...) alongside the dictionary entries — nothing document-specific to redo
here, just add one more sibling file the same way:

```csharp
// PersonalAccounting.Enums/PersonalAccountingTypeIds/PersonalAccountingTypeIds.Supplier.cs — a whole new file, not an edit to PersonalAccountingTypeIds.cs
namespace PersonalAccounting.Enums;

public static partial class PersonalAccountingTypeIds
{
    public const string Supplier = "‹freshly generated GUID›";
}
```

```csharp
// Domain entity
public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.{Name});
public {Name}() : base(TypeId) { }   // documents pass TypeId through the DocumentBase constructor

// Forms Dto
[TypeId(PersonalAccountingTypeIds.{Name})]
public class {Name}Dto : DocumentDto { ... }
```

**`ISubconto` vs plain `IEntityWithTypeId`** — decide per-document, same as dictionaries:

```csharp
// src/KandraCore/Kandra.Domain/Commons/ISubconto.cs
public interface ISubconto : IRootEntity, IEntityWithTypeId { }
```

Implement `ISubconto` on the document entity only if the document itself is a valid posting/analytical
dimension — typically a "settlement document" reference on an account's subconto slot (`GoodsReceipt` and
`Waybill` both do this — see their own doc comments, `postingService.AsOf(63).With<Counterparty,
GoodsReceipt>(...)`). A purely internal document like `Transfer` (warehouse-to-warehouse, no counterparty) just
implements bare `IEntityWithTypeId` instead. `AccountTransaction` implements neither implicitly through
`ISubconto`-shaped reasoning — its own doc comment explains why: it's the universal *manual* posting document,
not meant to be referenced as a dimension itself.

Generate the GUID fresh — never reuse an existing constant's value.

## 8. Numbering

Same `ISimpleNumberingSystem`/`NumberingBuckets` mechanism as dictionaries (`create-dictionary` reference.md
§7 has the full interface and the split-partial-file convention — `NumberingBuckets` already holds one file per
existing document, same as `PersonalAccountingTypeIds`, nothing to redo). The one document-specific convention worth calling
out: **every current document bucket key is year-suffixed**, so numbers reset per calendar year without needing
a separate bucket per year:

```csharp
public async ValueTask OnNewAsync({Name} entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
{
    entity.Date = date ?? DateTime.UtcNow;
    var yy = entity.Date.ToString("yy");
    entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.{Name}}:{yy}", raw => $"XX.{yy}{raw:D5}",
        requestorTag: "{Name}:OnNew", cancellationToken: cancellationToken);
}
```

`NumberingBuckets.{Name}` (e.g. `"GoodsReceipt"`) is just the bucket's stable *name* — the `:{yy}` suffix
appended at call time is what actually partitions the sequence by year; the bucket file itself never encodes
the year. `XX` is a 2-3 letter document-code prefix (`GR`/`TR`/`WB`/`AT`, ...) — pick a short, unique one, it's
purely cosmetic (embedded in the formatted `Code`, not tracked anywhere else). This pattern is a `PersonalAccounting`
convention, not an engine requirement — a document could use a plain non-year-suffixed bucket the way most
dictionaries do; year-suffixing just happens to be what every current document does.

## 9. Localization chain

Same `LocalizationChain`/three-resx-file mechanism as `create-dictionary` reference.md §8 — read that for the
underlying mechanism. The **set of keys** a document needs is larger than a dictionary's:

- One key per `[EditFormTitle]`/`[AddFormTitle]`/`[ViewFormTitle]`/`[ListFormTitle]`/`[NavigationName]` value
  and every `[Caption]` — same as dictionaries.
- One key per FluentValidation `.WithMessage(localizer["..."])` call in the Validator.
- **Three keys the generated List/Add/Update pages read directly, not through any Dto attribute**:
  `Add{Name}` (the List page's add-button label — a missing one renders as the raw, upper-cased key),
  `{Name}AddedSuccessfully` and `{Name}UpdatedSuccessfully` (the success snackbar text after Submit/Save — see
  `GoodsReceiptDtoAdd.g.cs`'s `Loc["GoodsReceiptAddedSuccessfully"]`). Easy to miss because nothing in the Dto
  references them — they're baked into `DocumentUiEmitter`'s emitted code by convention off the Dto's simple
  name, so they must exist under exactly that key shape.

Missing any key still compiles and passes DI validation — it just renders the raw key string at runtime instead
of localized text. Add all of the above to the English locale file (`PersonalAccounting.resx`; `.uk.resx`/`.ru.resx` optional).
Never pass `isLiteral: true` to dodge this.

## 10. Worked examples

Full, current, working source — copy the matching skeleton from [`data/`](data/), then adapt field-by-field.
Both examples' real `[Description]` content (header, every non-obvious field, and every line Dto/field) is
worth reading as a model for how much detail is useful — a sentence or two per field, not a paragraph.

### With lines: `GoodsReceipt`

A complete, typical document: header FKs, an order-shaped line collection, server-recomputed totals, a client
handler wired for live UI feedback, one register write plus one accounting post in `OnSubmitAsync`, a print
form, and `ISubconto`.

- **Domain** (`PersonalAccounting.Domain/Entities/Documents/GoodsReceipt.cs`) — `GoodsReceipt : DocumentBase,
  IEntityWithRows<GoodsReceipt>, ISubconto`, `TypeId = new(PersonalAccountingTypeIds.GoodsReceipt)`, `CounterpartyId`/
  `ToWarehouseId` FKs (`[JsonIgnore]` nav props), `[Precision(14,2)] Total`, one
  `[KandraTablePart(TableName = "Doc_GoodsReceiptLines")] IList<GoodsReceiptLine> Lines`.
  `GoodsReceiptLine : IOrderLine` carries `ItemId`/`Quantity`/`Price`/`Sum` (all `[Precision]`-attributed) plus
  two lot-tracking fields (`ExpirationDate`, `SerialNumber`) fed into the inventory register's `Details` —
  domain-specific, not a general document convention.
- **Forms** (`PersonalAccounting.Forms/Documents/GoodsReceipt.cs`) — `[TypeId(PersonalAccountingTypeIds.GoodsReceipt)]
  [KandraDocumentForm(Name = "GoodsReceipts", ...)] [SupportedExportFormats(ExportFormat.Pdf)]
  [SupportedPrintForm(typeof(GoodsReceiptPrintForm))] ... [UiBehavior(typeof(GoodsReceiptUiBehavior))]` on the class
  (the `GoodsReceiptUiBehavior` class sits in the same file); `CounterpartyId`/`ToWarehouseId` (`[Search]`/`[Dropdown]`),
  `Total` (`[NotEditable][Calculated][ListFormColumn(Filterable = false)]`), `Lines`
  (`ICollection<GoodsReceiptLineDto>`). `GoodsReceiptLineDto` uses `[GridColumn(Order = N, WidthPercent = ...)]`
  per field, `Sum` is `[NotEditable][Calculated]`. `GoodsReceiptQueryDto : DocumentQueryDto { }` (empty).
  `GoodsReceiptValidator` checks both FKs non-empty, `Lines` non-empty, and
  `RuleForEach(x => x.Lines).Must(l => l.Quantity > 0)`. The file also carries the `[PrintForm]`-attributed
  `GoodsReceiptPrintForm : DocumentPrintForm`/`GoodsReceiptPrintLine` classes (roles and types, no sections) — see
  `create-print-form`, not reproduced here.
- **Application** (`PersonalAccounting.Application/Documents/GoodsReceipt.cs`) —
  `[KandraDocumentBehavior(typeof(GoodsReceiptDto))] [RegisterTransactions(typeof(InventoryMovement))]
  [AccountTransactions]` on `GoodsReceiptBehavior`. `OnSaveAsync` recomputes `line.Sum`/`entity.Total`
  server-side (never trust the client) — note this happens **in addition to** the live recompute the Dto's
  `GoodsReceiptUiBehavior` does in the browser; the client behavior is UX-only, `OnSaveAsync` is the
  authoritative one. `OnNewAsync` numbers via the year-suffixed bucket pattern (§8). `OnSubmitAsync` resolves
  each line's costing method, builds one `InventoryMovement` per line, writes it via
  `registers.GetWriter<IBalanceRegisterWriter<InventoryMovement, InventoryBalance>>().PostAsync(...)`, then
  builds one `PostingTransaction` per line and posts it via `registers.GetPostingService().PostAsync(...)` — two
  separate writes in the same submit scope, one register, one ledger. `OnUnsubmitAsync`/`OnDeleteAsync` mirror
  both. `OnPrintAsync` resolves display names via `ILookupRepository` (never the entity's own nav properties —
  they're not loaded by the CRUD service's plain `GetAsync`) and calls `await renderer.RenderAsync(printForm, ...)`, which reads the company letterhead as of the document's date
  (the synchronous `Render` has none), filling the party blocks through the configuration's `IPrintPartyService`.
  `[Mapper] GoodsReceiptMapper : IEntityMapper<GoodsReceipt, GoodsReceiptDto>` declares the header pair plus a
  private `ToDto`/`ToEntity` pair for `GoodsReceiptLine`/`GoodsReceiptLineDto`, which Mapperly reuses for `Lines`.

Read the full three files directly under those paths for the exact current text.

### Lineless edge case: `AccountTransaction`

The universal manual posting document — no line collection at all, `IEntityWithRows<AccountTransaction>` is
hand-implemented as a no-op (§1), posts directly to the chart-of-accounts subsystem with user-picked
account/subconto values rather than known-at-compile-time ones, and is deliberately **not** `ISubconto` (not
meant to be referenced as a posting dimension itself — same reasoning `CostingMethodSetting` gives for the same
choice). Worth reading in full if you're building a header-only document — the shape differs from `GoodsReceipt`
in exactly three ways: no `[KandraTablePart]` property (so `RowNavigations`/`RemoveOrphans` are hand-written
empty), no `Lines`/line Dto/line Mapper entry anywhere across all three files, and `OnSubmitAsync` posts a
single `PostingTransaction` built from the Dto's own `SourceAccountCode`/`DestinationAccountCode` +
`Subconto1..5` fields (`[AccountDialog]`/`[SubcontoEditor]` — see `add-account-subconto-field`) instead of one
per line.

### Other real documents worth a glance (not walked through here)

`Waybill` (issue side, mirrors `GoodsReceipt`'s shape with movements flowing out instead of in) and `Transfer`
(both warehouses mandatory, two inventory legs per line — an expense leg via
`IInventoryAllocationService.ReallocateAsync` plus a mirrored receipt leg, real costing-method-combination
validation) are both meaningfully more complex than `GoodsReceipt` — read them only if you're building
something with genuinely two-sided inventory movement; `GoodsReceipt`'s single-leg `OnSubmitAsync` is the right
starting template for most new documents.

## 11. Client Refit interface

`src/KandraCore/Kandra.ClientLib.Common/ApiClients/IDocumentApiClient.cs` — the common contract every document
client interface inherits (through `IEntityApiClient<>`, same as dictionaries, see `create-dictionary`
reference.md §10 for that base):

```csharp
public interface IDocumentApiClient<DTO, PAGES_DTO, QUERY_DTO> : IEntityApiClient<DTO, PAGES_DTO, QUERY_DTO>
    where DTO : class, IDocumentDto
    where PAGES_DTO : class, IPagedResult<DTO>
    where QUERY_DTO : class, IDocumentQueryDto
{
    [Get("new")] Task<DTO> NewAsync(Guid? sourceDocumentId = null, CancellationToken cancellationToken = default);
    [Post("lookup")] Task<PagedResult<DocumentLookupItemDto>> LookupAsync([Body] QUERY_DTO? dto = null, CancellationToken cancellationToken = default);
    [Put("submit/{id}")] Task SubmitAsync(Guid id, CancellationToken cancellationToken = default);
    [Put("unsubmit/{id}")] Task UnsubmitAsync(Guid id, CancellationToken cancellationToken = default);
    [Post("print/{id}")] Task<HttpResponseMessage> PrintAsync(Guid id, [Body] DocumentExportRequest request, CancellationToken cancellationToken = default);
    [Get("registertransactions/{id}")] Task<DocumentRegisterTransactionsDto> GetRegisterTransactionsAsync(Guid id, CancellationToken cancellationToken = default);
    [Get("accounttransactions/{id}")] Task<DocumentAccountTransactionsDto> GetAccountTransactionsAsync(Guid id, CancellationToken cancellationToken = default);
    [Get("links/{id}")] Task<DocumentLinksDto> GetLinksAsync(Guid id, CancellationToken cancellationToken = default);
}
```

`NewAsync(sourceDocumentId)` — the client-side counterpart of `OnNewAsync`'s own `sourceDocumentId` parameter
(§5/§2's `[SourceFor]`). `SubmitAsync`/`UnsubmitAsync` are the posting-lifecycle actions the generated
View/Update pages call. `GetRegisterTransactionsAsync`/`GetAccountTransactionsAsync`/`GetLinksAsync` back the
View page's three always-present tabs (§12) — every document gets all three regardless of whether it declared
`[RegisterTransactions]`/`[AccountTransactions]`/`[SourceFor]` (an undeclared one just returns an empty result).

The concrete interface is thin, same as a dictionary's:

```csharp
[PathPrefix("/api/v1/documents/GoodsReceipts")]
public interface IGoodsReceiptsApiClient : IDocumentApiClient<GoodsReceiptDto, PagedResult<GoodsReceiptDto>, GoodsReceiptQueryDto>
{
}
```

`[PathPrefix]` must match the `Name` passed to `[KandraDocumentForm(Name = "...")]` exactly. Refit's own
generator implements the body at compile time — you only write the declaration. Same design-rule memory note
as `create-dictionary`'s §10 applies (no default-`null` bodies beyond the inherited set, real DTO marker-
interface constraints, extend `IKandraApiClient`/`IKandraProtectedApiClient` transitively, prefer
`IReadOnlyCollection<T>` for read-only returns) if you ever add a method beyond the inherited set.

## 12. Generated Blazor UI — nothing to hand-write

**As of this writing, every document's List/Add/Update/View/Form pages are 100% generated** by
`Kandra.Generators.Ui.Blazor.DocumentUiEmitter`, straight from the Dto's own attributes — this is the single
biggest difference from `create-dictionary`, where UI is still hand-written. Confirm this yourself at any time:
`PersonalAccounting.Client.MudBlazor/Pages/Document/{Name}/` is an **empty directory** for every real document in this
repo (kept only so the folder exists in source control; nothing lives in it), while
`PersonalAccounting.Client.MudBlazor/Generated.Net/Kandra.Generators.Ui.Blazor/Kandra.Generators.UiFormGenerator/
{Name}DtoAdd.g.cs` (+ `Update`/`View`/`List`/`Form`) is where the real, compiled page classes live. **Do not
copy `_HandWrittenReference/Document/{Name}/*.razor` into a new document's page folder** — those `.razor` files
are the pre-generator implementation, kept only as a historical reference per the "retire, don't delete"
convention, never a template to copy from for a new document.

What the generator derives automatically, with zero extra input beyond what §2/§10 already has you write:

- **Route + policy** — `/document/{camelCaseName}/{create|update/{id}|view/{id}}`, authorization policy names
  (`Insert{Name}`/`Update{Name}`/`View{Name}`) — same `EntityRouteConventions` convention documented in
  `src/KandraCore/CLAUDE.md`'s Interface-navigation section, fully derived from the Dto type, nothing to configure.
- **Form fields + line grid** — every `[Caption]`ed header property and, one embedded line grid per `[TablePart]` collection
  (or the legacy `Lines` collection) driven by each line Dto property's `[GridColumn]`.
- **Submit/Unsubmit buttons, success snackbar text** (`Loc["{Name}AddedSuccessfully"]`/
  `Loc["{Name}UpdatedSuccessfully"]`, §9), **client-side UI-behavior wiring** from the Dto's `[UiBehavior]`
  (`[OnChange]`/`[OnLineRemove]`/`[Visible]` become calls on the form's `Behavior`, the lifecycle hooks run when the
  form opens and before it saves; a server round-trip is a remote handler the behavior calls through an injected
  `IHandlerApiClient<,>` — see `create-ui-behavior`/`create-handler`).
- **Print button** — gated automatically on `[SupportedPrintForm]` presence: a document with one gets
  `onPrint: PrintRecord`; a document without gets `onPrint: null` in the generated code, not merely a hidden
  button — clicking a present-but-null print action would otherwise silently 404 at the server (`DocumentControllerBase
  .InternalPrintAsync` returns 404 when `OnPrintAsync` returns null) with no visible client-side error, which is
  exactly the failure mode this auto-gating exists to prevent.
- **View page's three tabs, always present**: Register Transactions, Account Transactions, and Links — every
  document gets all three regardless of whether it declared `[RegisterTransactions]`/`[AccountTransactions]`/
  `[SourceFor]`; an undeclared one just renders empty. The Links tab's "Create {Type}" button menu is populated
  from whatever `[SourceFor]` entries exist on *other* Dtos pointing at this one.

If a build error or a running page's behavior doesn't match what this section describes, the generator itself
(`src/KandraCore/Kandra.Generators.Ui.Blazor/DocumentUiEmitter.cs`) is the ground truth — re-read it rather than
assuming a hand-edit is needed; hand-editing a `Generated.Net/*.g.cs` file is never the fix (overwritten on
every build).

## If this reference goes stale

This document is a snapshot, not a live view. If the code disagrees with something here (a compile error citing
a signature that doesn't match, a missing member, a `Pages/Document/{Name}/` folder that unexpectedly has real
files in it again), trust the code — then update this file so the next person doesn't hit the same surprise.
The fastest way to re-verify a specific claim: grep the type/attribute name under `src/KandraCore/Kandra.Attributes/`,
`Kandra.Domain/`, `Kandra.Forms/`, `Kandra.Application.Abstractions/`, `Kandra.Validators/`,
`Kandra.ClientLib.Common/ApiClients/`, or `Kandra.Generators.Ui.Blazor/` — every type named above lives in
exactly one of those seven projects.
