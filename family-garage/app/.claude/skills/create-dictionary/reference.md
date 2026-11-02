# Dictionary engine — reference

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

Self-contained explanation of every engine type/attribute `SKILL.md` walks through — written so you don't need
to go read `src/KandraCore/*` source to use it. Exact signatures as of the last time this was verified against the
engine; if a build error contradicts something here, the code wins (see "If this reference goes stale" at the
bottom). Ready-to-copy `.cs` skeletons matching everything below live in [`data/`](data/) — pull the flat pair
for a normal dictionary, the hierarchical trio for a folder/leaf tree.

## What a Dictionary actually is

Reference/master data: persisted, has a form + grid, optional hierarchy, **no** posting, **no** register writes,
**no** tabular line sections. That last point is the real dividing line against a Document — if what you're
building needs child rows (order lines, movement lines), it's a Document, not a Dictionary, even if it also
happens to be "mostly reference data" (see `create-document`).

A dictionary is implemented across **three separate class-library projects** — this is the same three-project
split every entity kind in the engine uses (Documents, Reports, DataProcessors all follow it too), not a
dictionary-specific thing:

- **`{Config}.Domain`** — the EF-mapped entity itself (`*Base`). Zero UI/auth/API attributes. This project is
  what a background job, importer, or migration script would reference — it has no idea a web API or a browser
  exists.
- **`{Config}.Forms`** — the `*Dto` + `*Validator` pair: the attribute-rich contract the source generators read
  to emit the controller, the OpenAPI schema, and the client's model-binding/validation behavior. **This is the
  one plane the Blazor WASM client also references** — it must never pull in `.Application`/`.Domain`/
  `.Persistence` (root `CLAUDE.md`'s hard rule), or the client build breaks by dragging EF Core into the
  browser.
- **`{Config}.Application`** — the `*Behavior` + `*Mapper` pair: server-only lifecycle logic (numbering, blob
  sync, ...) and the Mapperly mapper (`IEntityMapper<TEntity, TDto>`, a hand-written `[Mapper]` partial class)
  translating between the Domain entity and the Forms Dto. Never referenced by the client.

Concretely, that's exactly four files split across those three projects, always in this shape:

| Piece | Project | Base type | Carries |
|---|---|---|---|
| `*Base` entity | `{Config}.Domain` | `DictionaryBase` / `HierarchicalDictionaryBase` | Pure EF model. Zero UI/auth attributes — safe to reuse from jobs/importers. |
| `*Dto` | `{Config}.Forms` | `DictionaryDto` / `HierarchicalDictionaryItemDto` | The attribute-rich UI/API contract the generators read. |
| `*Behavior` | `{Config}.Application` | implements `IFlatDictionaryBehavior<T>` / `IHierarchicalDictionaryBehavior<T>` | Lifecycle hooks (numbering, blob sync, ...). Mandatory even as a no-op. |
| `*Validator` | `{Config}.Forms` | `DictionaryValidator<TDto>` | FluentValidation rules. Mandatory even if it just calls `base(...)`. |

Everything in that table is generator-discovered from attributes alone — nothing about *these four files* is
ever manually registered (§"Nothing else to register by hand" in `SKILL.md`). But a working, usable dictionary
needs more than these four files, and none of the rest is generated yet — see "Hand-maintained files" below and
`SKILL.md`'s checklist for the full list.

### Hand-maintained files (no generator, as of this writing)

Every one of these is either a **new file you add** (never an edit to a file someone else's dictionary also
touches — that's the whole point of the two split-partial-class conventions below) or a real edit to an
existing shared file, that the four-file core above does not by itself take care of:

| What | File | Why it's manual |
|---|---|---|
| Type identity constant | New file `{Config}.Enums/FamilyGarageTypeIds/FamilyGarageTypeIds.{Name}.cs` (or your configuration's equivalent) — `FamilyGarageTypeIds.cs` itself is never edited, see §6 | No generator mints/tracks GUIDs. |
| Numbering bucket constant | New file `{Config}.Domain/Numbering/NumberingBuckets.{Name}.cs` — `NumberingBuckets.cs` itself is never edited, see §7 | Same — a bucket name is just a string the behavior chooses, nothing discovers it automatically. |
| Localization keys | Edit to `{Config}.Localization/*.resx` (English required, uk/ru optional) | Every `[Caption]`/title/nav resx key referenced by the Dto must exist in all three locale files, or it silently renders as the raw key. See §8. |
| Refit client interface | New file `{Config}.ClientLib.Common/ApiClients/I{PluralName}ApiClient.cs` | Refit's generator implements the interface body, but nothing generates the interface *declaration* itself yet. See §10. |
| Blazor UI pages | **None to write** | The list/add/edit/view pages are generated from the Dto (`Kandra.Generators.Ui.Blazor`) into the client project's `Generated.Net/`. |
| Nav entry | Edit to `{Config}.Application/Navigation/*Interfaces.cs` (or hand-edited `NavMenu.razor`, if this configuration predates the Interface-tree nav model) | Not derived from the Dto automatically — see `add-interface-node`. |

None of this is optional for a dictionary the UI can actually use — skipping any row above still compiles and
passes DI validation, but leaves the feature invisible or broken in the browser (a missing resx key renders the
raw key name; a missing nav entry means no sidebar link exists; a missing Refit interface means the Blazor page
you copy from an existing one won't have a client service to call).

## 1. Domain entity base classes

### `DictionaryBase` (flat)

`src/KandraCore/Kandra.Domain/Entities/Dictionaries/DictionaryBase.cs`

```csharp
public class DictionaryBase : ChangesInfo, IDictionaryEntity
{
    public Guid Id { get; set; }
    [MaxLength(64)]  public virtual string Code { get; set; }
    [MaxLength(256)] public virtual string Name { get; set; }
}
```

- `ChangesInfo` supplies the audit columns (`Created`/`Modified`/`CreatorUserId`/`ModifierUserId`/`IsDeleted`)
  — never redeclare them. They never appear on a Dto, so the mapper's `ToEntity` (`RequiredMappingStrategy.Source`) leaves them at
  their defaults (see Counterparty in §9).
- `Code`/`Name` are `virtual` specifically so a subclass can `override` with its own `[MaxLength]` — most
  dictionaries should just inherit both unmodified. Reach for `[KandraMaxLengthOverride]` instead of an
  `override` when *only* the schema constraint differs and no Dto-side attribute needs to change (see
  `Currency` in §9) — an `override` is for when the Dto side also needs its own `[Caption]`.

### `HierarchicalDictionaryBase` (hierarchical)

`src/KandraCore/Kandra.Domain/Entities/Dictionaries/HierarchicalDictionaryBase.cs`

```csharp
public abstract class HierarchicalDictionaryBase : DictionaryBase, IHierarchicalItem
{
    public Guid? ParentId { get; set; }
    public int Depth { get; set; }
    [MaxLength(250)] public string Path { get; set; } = default!;
}
```

`abstract` — always subclassed into a **TPH tree of exactly three classes**: an abstract root (extends this
directly, carries `[KandraDictionaryEntity]`), a `[KandraDictionaryFolder]`-marked concrete folder subclass,
and a plain concrete leaf subclass (the one that gets `ISubconto` if postable). `Depth`/`Path` are engine-
managed — the hierarchical CRUD service maintains them; a Dto never carries them (see `HierarchicalDictionaryItemDto`
in §3), so the mapper leaves them alone too.

## 2. Attribute vocabulary

All namespaces are under `Kandra.Attributes.*`. Grouped by what they mark.

### Entity-side (Domain plane, `Kandra.Attributes.Entities`)

| Attribute | Target | Signature | Meaning |
|---|---|---|---|
| `[KandraDictionaryEntity]` | class | `TableName` (string?), `SkipGeneration` | Marks the entity as a dictionary for `Kandra.Generators.EfConfig`/`.Di.Persistence` discovery. On the abstract root for hierarchical, on the class itself for flat. |
| `[KandraGeneratedEquality]` | class (must be `partial`) | no params | Generates `Equals`/`GetHashCode`/`ToString` over the class's *own* declared properties (not inherited). Business logic stays 100% hand-written in the same file. |
| `[KandraDictionaryFolder]` | class | no params | Marks the TPH root's folder subclass. Exactly one other concrete sibling (the leaf) must exist — more/fewer is a generator error. |
| `[KandraMaxLengthOverride(propertyName, length)]` | class, repeatable | `(string, int)` ctor | Escape hatch for a per-subclass `[MaxLength]` override on an *inherited* property, without a full `override` redeclaration (see Currency, §9). |
| `[TypeId(guidLiteralString)]` | class or enum | `(string)` ctor, parses to `Guid` | The **Dto**-side counterpart of the entity's `static Guid TypeId` (`IEntityWithTypeId`) — same value, so the type identity is discoverable from either plane. Required on every dictionary Dto. |

### Form-side (Forms plane)

| Attribute | Namespace | Target | Signature | Meaning |
|---|---|---|---|---|
| `[KandraDictionaryForm]` | `.Application` | class | `Name` (string, **required**), `ValidatorType` (Type), `QueryDtoType` (Type), inherited `SkipGeneration` | Marks the Dto as a dictionary form. `Name` drives the controller class name, route segment, and Refit client name — **not** convention-derived, always set it explicitly. |
| `[EditFormTitle]` / `[AddFormTitle]` / `[ViewFormTitle]` / `[ListFormTitle]` | `.Naming` | class | `(string value, bool isLiteral = false)` | Whole-phrase captions for the four generated pages. `value` is a resx key by default; `isLiteral: true` skips localization (**don't** use this to dodge adding a resx key — see §5). |
| `[Help]` | `.Naming` | class (inherited) | `(string pathOrUrl)` | Links the form to its documentation page: generated pages show a (?) button next to the title. A relative path is appended to the language's base from `Kandra:Client:HelpUrl`/`HelpUrls`; an absolute http(s) URL is used as-is; `#anchor`/`?query` kept. One per form (List/Add/Update/View share it); on a hierarchical dictionary put it on the abstract root Dto. Empty or non-http(s) value: `KANDRAUI032`. |
| `[NavigationName]` | `.Naming` | class | `(string value, bool isLiteral = false)` | Sidebar nav label — same resx-key-by-default rule. |
| `[Caption]` | `.Naming` | property | `(string value, bool isLiteral = false)` | Field/column display label. Named "Caption" (not "DisplayName") to avoid colliding with the BCL's own `DisplayNameAttribute`. |
| `[Description]` | BCL (`System.ComponentModel`) | class or property | `(string)` ctor | Semantic explanation of what the Dto/field means and how it's used — not a UI label (`[Caption]` covers that). Reflected by `Kandra.Mcp`'s `EntityFieldMetadataBuilder` and surfaced through the MCP server's `describe_entity_type`/`list_entity_types` tools as an AI agent's primary way to learn what an entity means. **Mandatory on every Dto class**; add on a property whenever its purpose isn't obvious from its name/caption alone. `Inherited = true` by default, so a hierarchical leaf Dto picks up the abstract root's class-level `[Description]` automatically unless it declares its own override (see §9 `Counterparty`). |
| `[FormTab(key, isLiteral = false)]` | `.Layout` | class, repeatable | `(string, bool)` ctor, has `Order` | Declares one tab. `key` doubles as both the tab's `[TabRef]`-matching key *and* its own resx caption key (`isLiteral: true` skips localization). Optional: a `[TabRef]` naming no `[FormTab]` still makes a tab, captioned by its key and placed after the declared ones. On a hierarchical dictionary declare it on the leaf (or folder) Dto — the root's and each subclass's declarations are merged, and a tab with nothing on it for one form (the folder's, say) is left out. |
| `[TabRef(key)]` | `.Layout` | property | `(string)` ctor | Puts the field on the tab with that `key`. No caption/order of its own. Fields with no `[TabRef]` (and Code/Name) sit on an implicit first tab captioned by the engine's `General` key, which only appears once the form has another tab. |
| `[FormSection(key, isLiteral = false)]` | `.Layout` | class, repeatable | `(string, bool)` ctor, has `TabKey`, `Order`, `Columns` (default 1) | Declares a titled group of fields (same resx-key/literal rule as `[FormTab]`). `TabKey` puts it on a tab (omit for the implicit first tab), `Columns` lays its fields out in that many responsive columns. |
| `[SectionRef(key)]` | `.Layout` | property | `(string)` ctor | Puts the field in the section with that `key`. If the field has no `[TabRef]`, it lands on the section's `TabKey` tab. |
| `[UiBehavior(typeof(MyUiBehavior))]` | `.Layout` | class | `(Type)` ctor | Names the **one UI behavior class** of the form (any form-bearing Dto: dictionary - on a hierarchical one declare it on the abstract root -, document, report filters, data-processor input). The class lives in the same file, derives from `DictionaryUiBehavior<TDto>`/`DocumentUiBehavior<TDto>`/`ReportUiBehavior<TDto>`/`DataProcessorUiBehavior<TDto>` with `TDto` = this Dto, and holds all UI logic of the form. A line Dto has none. See "UI behavior" below. |
| `[OnChange(nameof(B.Method))]` | `.Layout` | property | `(string)` ctor | Runs the named instance method of the behavior when the **user** edits the field. Exactly one per property. A root field: `()`; a line Dto field: `(TLine line)`; optional trailing `CancellationToken`; returns `void`/`Task`/`ValueTask`. |
| `[OnLineRemove(nameof(B.Method))]` | `.Layout` | property (a `[TablePart]`/`Lines` collection) | `(string)` ctor | Runs `Method(TLine removedLine[, CancellationToken])` after a row is removed from that table part. |
| `[Visible(nameof(B.Method))]` | `.Layout` | property | `(string)` ctor | Shows the field (or, on a document, a `[TablePart]` grid) only while the behavior's `public bool Method()` returns `true`; on a line Dto field the method takes `(TLine line)` and hides the cell of the rows it rejects. |
| `Visible = nameof(B.Method)` | `.Layout` | named property of `[FormTab]` / `[FormSection]` | `string?` | Shows the tab / section only while the behavior's `public bool Method()` returns `true`. |

**How tabs and sections lay out** (Dictionary flat/hierarchical, Document header, Report filters and DataProcessor input all
honor them; a DataProcessor *result* Dto stays one flat list): a form that declares or references none renders exactly as
before. Otherwise, fields with no placement go on the implicit `General` tab, then each declared tab follows by `Order`
(ties: declaration order), then undeclared `[TabRef]` keys in first-use order. Inside a tab, unsectioned fields come first,
then each section by `Order`. Every tab stays mounted while hidden, so a required field on another tab still validates;
a failed Save marks the tabs holding an invalid field with an error icon and brings the first of them forward. Add a resx
key per tab and per section caption — a missing one renders the raw key, and `dotnet test`
(`GeneratedUiLocalizationKeysTests`) catches it. The build warns when it ignores or cannot place
something: `KANDRAUI015` (duplicate `[FormTab]`/`[FormSection]` key), `KANDRAUI016` (a field's `[TabRef]` disagrees with its
section's `TabKey`), `KANDRAUI017` (a declared tab/section no field references, so it renders nothing), `KANDRAUI018`
(`[SectionRef]` on a table part, or `[TabRef]`/`[SectionRef]` on a line field).

**UI behavior** (`[UiBehavior(typeof(MyUiBehavior))]` on a form-bearing Dto; every kind - dictionary, document, report filters, data-processor input): the one class that holds all UI logic of
the open form - what happens when a field is edited, which fields/grids/sections/tabs show, what runs when the form opens or is about to save. It lives in the same file as the Dto, derives from the base of the
Dto's kind (`DictionaryUiBehavior<TDto>` etc. in `Kandra.Forms.UiBehaviors`, `TDto` = the Dto - the abstract root for a hierarchical dictionary, whose folder and leaf forms each get their own instance), reads
and mutates the record through the inherited `Record`, and keeps the state of the open form in private fields (an item's attributes a lookup fetched, a UI toggle - never persisted, never serialized). Its
dependencies come from client DI through the constructor, one instance per open form. The attributes name its instance methods with `nameof`:

```csharp
[UiBehavior(typeof(PaymentUiBehavior))]
[FormSection("OtherOperation", Visible = nameof(PaymentUiBehavior.IsOther))]
public class PaymentDto : DocumentDto
{
    [OnChange(nameof(PaymentUiBehavior.OnOperationChanged))]
    public PaymentOperation Operation { get; set; }

    [Visible(nameof(PaymentUiBehavior.HasCounterparty))]
    public Guid? CounterpartyId { get; set; }

    [SectionRef("OtherOperation")]
    public string? CorrespondingAccountCode { get; set; }
}

public sealed class PaymentUiBehavior : DocumentUiBehavior<PaymentDto>
{
    public bool HasCounterparty() => Record.Operation is PaymentOperation.ToSupplier or PaymentOperation.Refund;
    public bool IsOther() => Record.Operation == PaymentOperation.Other;
    public void OnOperationChanged() { if (!HasCounterparty()) Record.CounterpartyId = null; }   // a hidden field keeps its value: clear it here if that matters
}
```

Signatures: a root field, tab, section or table part takes `()`; a line Dto field or a line event takes `(TLine line)`. `[OnChange]`/`[OnLineRemove]` methods may add a trailing `CancellationToken` (cancelled when the
form is disposed) and return `void`, `Task` or `ValueTask`; `[Visible]` methods are synchronous and return `bool`. Only a **user edit** fires `[OnChange]` - assigning a property from code does not, so a method
that changes a dependent value calls the dependent logic itself. Lifecycle hooks are virtual methods, not attributes: `OnOpenAsync` (all kinds, once per record), `OnBeforeSaveAsync` (dictionary, document),
`OnBeforeRunAsync` (report), `OnBeforeExecuteAsync` (data processor). Visibility is re-evaluated on every render, i.e. after every editor change, with no wiring; it applies to the Add/Update/Form and View
pages, **not** to print forms, list columns or export.

Things to know: a **hidden field keeps its value** - the generator never clears it, so clear it in the `[OnChange]` method of the field that decides visibility when a stale value does harm; a **validator
must not depend on the behavior** and states its own condition for a hidden field (`RuleFor(x => x.CounterpartyId).NotEmpty().When(x => x.Operation is ToSupplier or Refund)`), or a required hidden field
blocks Save with an error nobody can see; a tab or section whose fields are all hidden is **not** hidden automatically - give it its own `Visible =`; a composite account + `[SubcontoEditor]` group takes the
`[Visible]` of its account field; the state starts **empty** on every open, including editing an existing record, so a helper must treat "not looked up (yet)" as a sensible default (show, usually) rather than
hide data. A server lookup is a remote handler: inject `IHandlerApiClient<TIn,TOut>` into the behavior (`create-handler`). Never use `ConfigureAwait(false)` in a behavior or its helpers. Build errors:
`KANDRAUI026` (a method is named but the Dto - or the owning root of a line Dto - has no `[UiBehavior]`), `KANDRAUI027` (no such method), `KANDRAUI028` (wrong signature - the message lists the accepted shapes),
`KANDRAUI029` (the `[UiBehavior]` type is not a public non-abstract class deriving from the kind's base with `TDto` = this Dto, or `[UiBehavior]` sits on a hierarchical folder/leaf), `KANDRAUI030` (warning:
`[UiBehavior]` on a line Dto, ignored). Full recipe: the `create-ui-behavior` skill; the worked examples are `WaybillUiBehavior`, `GoodsReceiptUiBehavior`, `TransferUiBehavior`, `BankPaymentUiBehavior` in the
FamilyGarage reference configuration (with the Transient helpers `ItemInfoCache`/`LinePrices` they compose).

| `[ListFormColumn]` | `.Layout` | property | `Sortable` (bool, default `true`), `Filterable` (bool, default `true`) | Marks a property as a column in the top-level list/grid — distinct from `[GridColumn]`, which is for an embedded line grid (documents only). |
| `[Search(sourceDtoType)]` | `.Editors` | property | `(Type)` ctor, `NodeSelection` (`HierarchicalNodeSelection`, default `Leaves`), `RootFolderCode` (string?) | Autocomplete FK picker with server-side filtering, for large dictionaries. `sourceDtoType` is always a `*Dto`, never an entity type. `RootFolderCode` takes the target folder's stable `Code`, not a `Guid` — attribute arguments must be compile-time constants. |
| `[Dropdown(sourceDtoType)]` | `.Editors` | property | `(Type)` ctor | Plain `<select>`, loads the whole small dictionary up front. Same Dto-type rule as `[Search]`. |
| `[Required]` | BCL | property | — | Plain `System.ComponentModel.DataAnnotations.RequiredAttribute` — **no custom Kandra `[Required]` exists.** Per the root `CLAUDE.md` convention, nullability already drives required-vs-optional through client validation/OpenAPI/model binding; don't redundantly stack `[Required]` on an optional (nullable) property. |
| `[FilePicker(Extensions = "...")]` | `.Editors` | property (`Guid?`) | see `create-*`/blob skills | Blob-backed file reference — see `Branch.RegistrationDocumentId` in §7 and `project_blob_storage_filepicker_implemented` memory for the reconciliation pattern. Not a core dictionary concern, mentioned here only because `Branch` (the flat reference example) happens to use one. |

Every attribute above targets a class/property that physically lives in `{Config}.Forms` — the Dto, or a Dto
property. `[KandraDictionaryForm]`'s own C# namespace happens to be `Kandra.Attributes.Application` (same for
every `Kandra*Form` attribute — that's just where the attribute *type itself* is declared in the engine),
which is easy to misread as "this is an Application-plane concern." It isn't: what matters is what it's
*applied to*, and `[KandraDictionaryForm]` is applied to the Dto, so it's a Forms-plane fact.

### Behavior-side (Application plane)

The one attribute that genuinely does belong to the Application plane — it decorates the *Behavior* class,
which lives in `{Config}.Application`, never `{Config}.Forms`:

| Attribute | Namespace | Target | Signature | Meaning |
|---|---|---|---|---|
| `[KandraDictionaryBehavior(formDtoType)]` | `.Application` | class (the Behavior, in `{Config}.Application`) | `(Type)` ctor | Links a Behavior class to its Dto. The engine reflects the behavior's own closed `IDictionaryBehavior<TEntity>` generic to find the entity type — never duplicate that as a second Type argument. |

Attribute base classes, if you need to explain the contract rather than just use it: `KandraEntityAttribute`
(`TableName`, `SkipGeneration`), `KandraFormAttribute` (`Name`, `ValidatorType`, `SkipGeneration`),
`KandraBehaviorAttribute` (`FormDtoType`, ctor-only), `VisibleNameAttribute` (`Value`, `IsLiteral`, the shared
base of the caption/title family — deliberately no `[AttributeUsage]` of its own, each subclass sets its own
placement rules).

## 3. Dto base classes

`src/KandraCore/Kandra.Forms/Dictionaries/DictionaryDto.cs`:

```csharp
public class DictionaryDto : IDictionaryDto
{
    [Required] [Caption("Id")] public Guid Id { get; set; }
    [Required] [Caption("Code")] [ListFormColumn] public virtual string Code { get; set; }
    [Required] [Caption("Name")] [ListFormColumn] public virtual string Name { get; set; }
    public int? Version { get; set; }
}
```

`src/KandraCore/Kandra.Forms/Querying/DictionaryQueryDto.cs`:

```csharp
public class DictionaryQueryDto : PagedQueryDto, IDictionaryQueryDto
{
    public Guid? Id { get; set; }                    // Lookup: resolve exactly one row
    public IReadOnlyCollection<Guid>? Ids { get; set; } // Lookup: resolve a specific set (multi-select chips)
}
```

(`PagedQueryDto` — the shared grandparent — carries `Page`/`PageSize`/`Filters`/`Sort`.)

Hierarchical counterparts (`src/KandraCore/Kandra.Forms/{Dictionaries,Querying}/`):

```csharp
public abstract class HierarchicalDictionaryItemDto : DictionaryDto, IHierarchicalItemDto
{
    public Guid? ParentId { get; set; }
    // No Depth/Path — those stay entity-only and server-managed; never exposed to the client.
}

public class HierarchicalDictionaryQueryDto : DictionaryQueryDto
{
    public Guid? ParentId { get; set; }        // scopes the Explorer-style grid to one folder; null = root
    public HierarchicalLookupMode? Mode { get; set; }
}
```

No `[JsonPolymorphic]`/`[JsonDerivedType]` on the shared `HierarchicalDictionaryItemDto` base — System.Text.Json
resolves polymorphism against the *declared* type of whatever's actually being serialized (always the concrete
dictionary's own root Dto, e.g. `CounterpartyNodeDto`), so those attributes belong on each dictionary's own
closed folder/leaf pair instead (see §7 Counterparty) — putting them on the shared base would silently do
nothing.

## 4. Validators

`DictionaryValidator<T>` (`src/KandraCore/Kandra.Validators/Dictionaries/DictionaryValidator.cs`):

```csharp
public abstract class DictionaryValidator<T> : AbstractValidator<T> where T : DictionaryDto
{
    protected DictionaryValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage(localizer["CodeRequired"]);
        RuleFor(x => x.Name).NotEmpty().WithMessage(localizer["NameRequired"]);
    }
}
```

Base is FluentValidation's own `AbstractValidator<T>` — not a hand-rolled result type (the architecture doc's
illustrative example differs here; trust this). `CodeRequired`/`NameRequired` resolve from the engine-level
resx module (see §5) — don't redeclare them in your configuration's own resx.

`PagedQueryDtoValidator<T>` (`src/KandraCore/Kandra.Application/Validation/PagedQueryDtoValidator.cs`) exists so
every "empty" `*QueryDto` subclass still gets a real (not pointless) validator satisfying the "validators are
mandatory" rule:

```csharp
public abstract class PagedQueryDtoValidator<T> : AbstractValidator<T> where T : PagedQueryDto
{
    protected PagedQueryDtoValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue);
        RuleFor(x => x.PageSize).GreaterThan(0).When(x => x.PageSize.HasValue);
    }
}
```

Typical usage is a one-line subclass: `public class BranchQueryDtoValidator : PagedQueryDtoValidator<BranchQueryDto>;`

## 5. Behavior contracts

The shared base every dictionary behavior implicitly gets (`src/KandraCore/Kandra.Application.Abstractions/Behaviors/IDictionaryBehavior.cs`):

```csharp
public interface IDictionaryBehavior<TEntity> where TEntity : class, IDictionaryEntity
{
    ValueTask OnOpenAsync(TEntity entity, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask OnSaveAsync(TEntity entity, bool isInsert, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask OnSubmitAsync(TEntity entity, bool isInsert, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask OnDeleteAsync(Guid id, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
```

`OnSubmitAsync` exists on the shape but is essentially vestigial for dictionaries in practice — dictionaries
don't post/submit the way documents do; you'll almost never override it. Every hook has a `ValueTask.CompletedTask`
default via C# default interface implementation, so a behavior only needs to override what it actually uses.

Flat and hierarchical each add their own "new entity" hook(s) on top of the shared four:

```csharp
public interface IFlatDictionaryBehavior<TEntity> : IDictionaryBehavior<TEntity> where TEntity : class, IDictionaryEntity
{
    ValueTask OnNewAsync(TEntity entity, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

public interface IHierarchicalDictionaryBehavior<TEntity> : IDictionaryBehavior<TEntity> where TEntity : class, IDictionaryEntity
{
    ValueTask OnFolderNewAsync(TEntity entity, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    ValueTask OnLeafNewAsync(TEntity entity, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
```

A no-op behavior (nothing overridden) is completely valid — but the class and its `[KandraDictionaryBehavior]`
attribute must still exist; DI validation fails at startup without one per dictionary.

## 6. Identity: `IEntityWithTypeId`, `ISubconto`, and the `FamilyGarageTypeIds` convention

```csharp
// src/KandraCore/Kandra.Domain/Commons/IEntityWithTypeId.cs
public interface IEntityWithTypeId
{
    static abstract Guid TypeId { get; }   // C# 11 static abstract member
}

// src/KandraCore/Kandra.Domain/Commons/ISubconto.cs
public interface ISubconto : IRootEntity, IEntityWithTypeId
{
}
```

Implement `ISubconto` on the leaf/flat entity only if it's a valid posting/analytical dimension for the
chart-of-accounts subsystem (see `post-to-accounting`) — a folder node never is (structural grouping, not a
postable value), and most plain dictionaries aren't either. Every dictionary still implements bare
`IEntityWithTypeId` regardless, so its `TypeId` is discoverable the same way as every other document/dictionary.

**Don't inline a fresh `Guid` literal directly in the entity.** This configuration centralizes every type
identity in one class, `{Config}.Enums.FamilyGarageTypeIds` (`FamilyGarage.Enums.FamilyGarageTypeIds` in this repo), as `const
string` fields (must stay `const`, not `static readonly` — they're also used as `[TypeId(...)]` constructor
arguments, which require compile-time constants).

**`FamilyGarageTypeIds` is `partial`, and its main file (`FamilyGarageTypeIds/FamilyGarageTypeIds.cs`) declares zero members** — every
constant lives in its own sibling file under the same `FamilyGarageTypeIds/` folder, one `FamilyGarageTypeIds.{Name}.cs` per
constant (`FamilyGarageTypeIds.Branch.cs`, `FamilyGarageTypeIds.Counterparty.cs`, ...). This is deliberate: it means adding a new
dictionary/document/enum never means editing a shared file at all — you only ever *add* a new file, so two
people adding two different dictionaries in parallel never touch the same line, let alone the same file. **Add
a new constant by creating a new file, never by inserting a line into `FamilyGarageTypeIds.cs` itself**:

```csharp
// FamilyGarage.Enums/FamilyGarageTypeIds/FamilyGarageTypeIds.Supplier.cs — a whole new file, not an edit to FamilyGarageTypeIds.cs
namespace FamilyGarage.Enums;

public static partial class FamilyGarageTypeIds
{
    public const string Supplier = "‹freshly generated GUID›";
}
```

Then reference the same constant from both planes — this is the mechanism that keeps the Domain-side
`IEntityWithTypeId.TypeId` and the Forms-side `[TypeId]` in sync without duplicating the literal:

```csharp
// Domain entity
public static Guid TypeId { get; } = new(FamilyGarageTypeIds.Supplier);

// Forms Dto
[TypeId(FamilyGarageTypeIds.Supplier)]
public class SupplierDto : DictionaryDto { ... }
```

Generate the GUID fresh — never reuse an existing constant's value. For a **hierarchical** dictionary there is still
exactly one entry (one new file): the TypeId is declared once on the abstract root (`{Name}Node` in Domain,
`[TypeId]` on `{Name}NodeDto` in Forms) and inherited by the folder and leaf — never redeclared on either. Folder
vs leaf is a property of the row (the TPH discriminator, `is {Name}Folder`), not of the TypeId; one TypeId per
dictionary also means posted subconto values, change-log entries and `ChangeEvents` TypeId filters see one
identity for the whole tree, the same way 1C treats a catalog's groups and elements as one type.
`data/TypeIdConstant.cs` is the copy-paste template for one such file.

## 7. Numbering

```csharp
// src/KandraCore/Kandra.Application.Abstractions/Services/ISimpleNumberingSystem.cs
public interface ISimpleNumberingSystem
{
    Task<string> GetNextAsync(string bucket, Func<ulong, string> formatter,
        Guid? requestorId = null, string? requestorTag = null, CancellationToken cancellationToken = default);
    // + GetNextFromPoolAsync / LeaseToPoolAsync for the pooled-number case — rarely needed by a plain dictionary
}
```

Gaps tolerated, number visible immediately in `OnNewAsync`. Formatting is entirely the caller's job — the
service only guarantees an atomic, monotonically increasing raw `ulong` per bucket name (a bucket is an opaque
string, created lazily starting from 1 on first use).

**`{Config}.Domain.Numbering.NumberingBuckets` follows the exact same split-partial-file convention as
`FamilyGarageTypeIds` in §6** — its main file (`Numbering/NumberingBuckets.cs`) declares zero members, and every bucket
constant lives in its own sibling file, one `NumberingBuckets.{Name}.cs` per dictionary/document, in the same
`Numbering/` folder (no extra subfolder needed here — unlike `FamilyGarageTypeIds`, `Numbering/` already existed as a
dedicated folder before this convention was introduced). Add a new bucket by creating a new file, never by
inserting a line into `NumberingBuckets.cs` itself:

```csharp
// FamilyGarage.Domain/Numbering/NumberingBuckets.Supplier.cs — a whole new file, not an edit to NumberingBuckets.cs
namespace FamilyGarage.Domain.Numbering;

public static partial class NumberingBuckets
{
    public const string Supplier = "Supplier";
}
```

Usage in the Behavior is unchanged — the split is purely a file-organization detail, invisible to callers:

```csharp
entity.Code = await numbering.GetNextAsync(NumberingBuckets.Supplier, raw => $"SUP-{raw:D6}",
    requestorTag: "Supplier:OnNew", cancellationToken: cancellationToken);
```

A hierarchical dictionary typically uses **two separate buckets** (i.e. two new files), one for the folder kind
and one for the leaf kind (see Counterparty in §9) — folder and leaf codes usually want different
prefixes/widths. `data/NumberingBucketConstant.cs` is the copy-paste template for one such file.

Not every dictionary auto-numbers — `Currency` hand-types its code (see §9) and its `OnNewAsync` is a
deliberate no-op; skip creating a `NumberingBuckets.{Name}.cs` file entirely for that case. Decide this
per-dictionary; don't assume numbering is mandatory.

## 8. Localization chain

`LocalizationChain`: engine-level `src/KandraCore/Kandra.Localization/Kandra*.resx` supplies shared keys
(`CodeRequired`/`NameRequired`, used by `DictionaryValidator<T>` itself — **don't redeclare these in your
configuration's own resx**, the chain already resolves them). Config-level `{Config}.Localization/{Config}*.resx`
supplies every entity-specific key your new Dto's `[EditFormTitle]`/`[AddFormTitle]`/`[ViewFormTitle]`/
`[ListFormTitle]`/`[NavigationName]`/`[Caption]`/`[FormTab]` attributes reference. Most-recently-registered
module wins per key, falls through to earlier modules for anything it doesn't redeclare.

Add one `<data name="TheKey">` entry per referenced key to the English locale file
(`{Config}.resx`; the `.uk.resx`/`.ru.resx` files are optional, a missing key falls back to English) — a missing key just renders as the raw key name at runtime, not an
error, so it's easy to miss one locale. Example shape (from `FamilyGarage.resx`):

```xml
<data name="Nav_Branches" xml:space="preserve">
  <value>Branches</value>
</data>
```

The matching `.Designer.cs` is autogenerated by `dotnet build` from the `.resx` files — never hand-edit it.
Never pass `isLiteral: true` on a title/caption attribute just to skip adding a resx key — that's a shortcut
that produces an unlocalized string in every culture, not a real substitute.

## Tabular sections (table parts) on a dictionary

A dictionary can own rows - a counterparty's bank accounts - exactly the way a document owns lines.

- **Entity**: the row class implements `IEntityRow` (`Guid Id`, the owner's `Guid {Owner}Id` + navigation, the row's own fields); the owner declares
  `[KandraTablePart(TableName = "Dict_{Owner}{Rows}")] public IList<{Row}> {Rows}` and implements `IEntityWithRows<TSelf>` (the row navigations and
  `RemoveOrphans` are generated; the class must be `partial`). On a **hierarchical** dictionary the owner is the **leaf** class - a folder has
  no rows. Nothing else to write: the EF configuration of the row, the owner's relationship and the repository registration are generated.
- **Dto**: `[TablePart(AddCaption = "AddKey")] [Caption("Key")]` on an `ICollection<{Row}Dto>` property of the flat Dto, or of the **leaf** Dto
  (`[TabRef]` puts the grid on a tab); the row Dto has a `[Required] Guid Id` (a new row sends `Guid.Empty`) and `[GridColumn(Order, WidthPercent)]` on its fields.
- **Mapper**: Mapperly maps the collection once it has the row methods - add `private partial {Row}Dto ToDto({Row} line);` and
  `private partial {Row} ToEntity({Row}Dto dto);` to the dictionary's mapper (with `RequiredMappingStrategy.Target`/`Source`).
- **Validator**: `RuleForEach(x => x.{Rows}).ChildRules(...)` plus any whole-collection rule on the (leaf) Dto's validator.
- **Behavior of the form**: `[OnLineAdd]`/`[OnLineRemove]` on the collection and `[OnChange(nameof(B.M))]` with `(TLine line)` on a row field name methods of the
  dictionary's UI behavior (`Counterparty` keeps exactly one default account this way; see `CounterpartyUiBehavior`).
- Rows load only when one item is opened (`GetByIdAAsync`/update), never in list or lookup queries; the View page shows the grid read-only.
- Add a migration for all three providers (`dotnet kandra-migrate add <Name>`).

## 9. Worked examples

Full, current, working source for both shapes — copy the matching skeleton from [`data/`](data/), then adapt
field-by-field against whichever of these two is structurally closer to what you're building. Both examples
below also show real `[Description]` content worth reading as a model for how much detail is useful (a
sentence or two, not a full paragraph) — `Counterparty` in particular shows the root/folder split described
above (root's `[Description]` explains the whole entity + the folder/leaf distinction, the folder's own
`[Description]` overrides it with the narrower "organizational only" meaning).

### Flat: `Branch`

Minimal flat dictionary, auto-numbered, plus one `[FilePicker]` blob-backed field (not core to being a
dictionary — shown because it's what the real example happens to have):

- **Domain** (`FamilyGarage.Domain/Entities/Dictionaries/Branch.cs`) — `Branch : DictionaryBase, IEntityWithTypeId`,
  `TypeId = new(FamilyGarageTypeIds.Branch)`, a `Warehouses` nav collection (`[JsonIgnore]`), `RegistrationDocumentId`
  (`Guid?`).
- **Forms** (`FamilyGarage.Forms/Dictionaries/Branch.cs`) — `[TypeId(FamilyGarageTypeIds.Branch)] [KandraDictionaryForm(Name
  = "Branches", ...)] [EditFormTitle("EditBranch")] ... [NavigationName("Nav_Branches")]`, one
  `[Caption("RegistrationDocument")] [FilePicker(Extensions = "*.pdf")] public Guid? RegistrationDocumentId`.
- **Application** (`FamilyGarage.Application/Dictionaries/Branch.cs`) — `BranchBehavior(ISimpleNumberingSystem,
  IBlobService)`, `OnNewAsync` numbers via `NumberingBuckets.Branch`, `OnSaveAsync`/`OnDeleteAsync` sync the
  blob reference. `[Mapper] BranchMapper : IEntityMapper<Branch, BranchDto>` with two partial methods,
  `ToDto` (`[MapperRequiredMapping(RequiredMappingStrategy.Target)]`) and `ToEntity` (`...Source`).
  `BranchQueryDtoValidator : PagedQueryDtoValidator<BranchQueryDto>;`

Read the full three files directly under those paths for the exact current text — this reference and `data/`
already reflect them, but the repo copy is the tiebreaker if anything ever drifts.

### Hierarchical: `Counterparty`

Three-class TPH tree, one bucket per folder/leaf kind, `ISubconto` on the leaf only:

- **Domain** — abstract `CounterpartyNode : HierarchicalDictionaryBase, IEntityWithTypeId` carries `[KandraDictionaryEntity
  (TableName = "Dict_Counterparties")]` and the dictionary's one `TypeId`. `CounterpartyFolder : CounterpartyNode`
  carries `[KandraDictionaryFolder]` and nothing else. `Counterparty : CounterpartyNode, ISubconto` is the plain
  leaf (its `TypeId` inherited from the Node), with its own business fields (`ContactName`, `Address`, `City`, ...).
- **Forms** — `CounterpartyNodeDto : HierarchicalDictionaryItemDto` (abstract) carries
  `[TypeId(FamilyGarageTypeIds.Counterparty)] [JsonPolymorphic(TypeDiscriminatorPropertyName = "isFolder")]
  [JsonDerivedType(typeof(CounterpartyFolderDto), "1")] [JsonDerivedType(typeof(CounterpartyDto), "0")]`
  **plus** the whole `[KandraDictionaryForm]`/title/nav attribute set (all on the abstract root, not the leaf).
  `CounterpartyDto : CounterpartyNodeDto` carries `[FormTab("Contact")]` and its business fields, each
  `[TabRef("Contact")]`. `CounterpartyFolderDto : CounterpartyNodeDto` carries only its own `[Description]`, no
  fields. Three validators: `CounterpartyValidator`/`CounterpartyFolderValidator` (leaf-only/folder-only rules,
  never registered directly) dispatched from `CounterpartyNodeValidator` (the one actually wired to
  `[KandraDictionaryForm(ValidatorType = ...)]`) via `RuleFor(x => x as CounterpartyDto).SetValidator(...)
  .When(x => x is CounterpartyDto)` — and the mirror for the folder type.
- **Application** — `CounterpartyBehavior : IHierarchicalDictionaryBehavior<CounterpartyNode>` implements
  `OnFolderNewAsync`/`OnLeafNewAsync` against two separate `NumberingBuckets` entries
  (`CounterpartyFolder`/`Counterparty`, different prefixes `CPF-`/`CP-`). `CounterpartyMapper` implements
  `IEntityMapper<CounterpartyNode, CounterpartyNodeDto>` - the abstract roots, which is what the hierarchical CRUD
  service maps against - and each of its two partial methods carries `[MapDerivedType<CounterpartyFolder,
  CounterpartyFolderDto>]` + `[MapDerivedType<Counterparty, CounterpartyDto>]` (reversed on `ToEntity`), so it
  dispatches to the folder/leaf pair at runtime. `Depth`/`Path`/the `ChangesInfo` audit fields aren't on any Dto,
  so a client payload can't carry them and `ToEntity`'s `Source` strategy leaves them default. The hierarchical
  CRUD service restores `Path`/`Depth` on update.

The JSON discriminator is the string `"1"`/`"0"`, not the int `1`/`0` the architecture doc's illustrative
example shows — the client's generated `JsonInheritanceConverter` reads the discriminator via
`JsonElement.GetString()`, which throws on a JSON number token. This is a wire/JSON-layer detail only; the EF
Core `IsFolder` discriminator column is a separate, unrelated int mapping and is untouched by this choice.

### Variant: `Currency` (hand-typed code + narrower `[MaxLength]`)

Shows the two mechanisms for "this one dictionary's Code needs to differ from the norm" without a wholesale
custom validator hierarchy:

```csharp
// Domain — class-level escape hatch, no override needed since only the schema length differs
[KandraDictionaryEntity(TableName = "Dict_Currencies")]
[KandraMaxLengthOverride(nameof(Code), 3)]
public partial class Currency : DictionaryBase, IEntityWithTypeId { ... }

// Forms — full override needed here because the Dto side also wants its own [Caption]
[Caption("CurrencyCode")] [MaxLength(3)]
public override string Code { get; set; }
```

`CurrencyBehavior.OnNewAsync` is an explicit no-op (`ValueTask.CompletedTask`) — the class and its
`[KandraDictionaryBehavior]` attribute still exist (mandatory), they just don't assign `Code`; the user types
ISO codes like `"USD"` by hand. `CurrencyValidator` derives directly from `AbstractValidator<CurrencyDto>` (not
`DictionaryValidator<CurrencyDto>`) because it needs an extra `.MaximumLength(3)` rule the shared base doesn't
parameterize — still calls the same `localizer["CodeRequired"]`/`localizer["NameRequired"]` messages by hand.

## 10. Client Refit interface

Not one of the four generated pieces (§"What a Dictionary actually is") — no generator emits this today (root
`CLAUDE.md`'s "still hand-written" list), so it's a fifth file you write by hand in
`{Config}.ClientLib.Common/ApiClients/`. It's thin: almost everything comes from a shared base interface, and
the concrete file usually adds nothing beyond the class declaration itself.

```csharp
// src/KandraCore/Kandra.ClientLib.Common/ApiClients/IEntityApiClient.cs — the common CRUD contract every
// entity kind's client interface inherits (through IDictionaryApiClient<>/IHierarchicalDictionaryApiClient<>
// below), itself built on IKandraProtectedApiClient (auth header wiring — see reference.md's own scope, not
// reproduced here).
public interface IEntityApiClient<DTO, PAGES_DTO, QUERY_DTO> : IKandraProtectedApiClient
    where DTO : class, IEntityDto
    where PAGES_DTO : class, IPagedResult<DTO>
    where QUERY_DTO : class, IPagedQueryDto
{
    [Get("all")]    Task<IReadOnlyCollection<DTO>> AllAsync(CancellationToken cancellationToken = default);
    [Post("query")] Task<PAGES_DTO> QueryAsync([Body] QUERY_DTO body, CancellationToken cancellationToken = default);
    [Get("read/{id}")] Task<DTO> ReadAsync(Guid id, CancellationToken cancellationToken = default);
    [Post("insert")]   Task<CreatedResultDto> InsertAsync([Body] DTO body, CancellationToken cancellationToken = default);
    [Put("update")]    Task UpdateAsync([Body] DTO body, CancellationToken cancellationToken = default);
    [Delete("delete/{id}")] Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    [Get("changestate/{id}")] Task<ChangeStateDto> GetChangeStateAsync(Guid id, CancellationToken cancellationToken = default);
}
```

Flat dictionaries add `NewAsync`/`LookupAsync` via `IDictionaryApiClient<DTO, PAGES_DTO, QUERY_DTO>`:

```csharp
public interface IDictionaryApiClient<DTO, PAGES_DTO, QUERY_DTO> : IEntityApiClient<DTO, PAGES_DTO, QUERY_DTO>
    where DTO : class, IDictionaryDto
    where PAGES_DTO : class, IPagedResult<DTO>
    where QUERY_DTO : class, IDictionaryQueryDto
{
    [Get("new")]    Task<DTO> NewAsync(CancellationToken cancellationToken = default);
    [Post("lookup")] Task<PagedResult<LookupItemDto>> LookupAsync([Body] QUERY_DTO? dto = null, CancellationToken cancellationToken = default);
}
```

Hierarchical dictionaries use `IHierarchicalDictionaryApiClient<DTO, PAGES_DTO, QUERY_DTO>` instead — same base,
different `NewAsync` shape (takes `parentId`/`isFolder` so the server knows which concrete type to construct)
plus a `MoveToAsync` for re-parenting:

```csharp
public interface IHierarchicalDictionaryApiClient<DTO, PAGES_DTO, QUERY_DTO> : IEntityApiClient<DTO, PAGES_DTO, QUERY_DTO>
    where DTO : class, IDictionaryDto, IHierarchicalItemDto
    where PAGES_DTO : class, IPagedResult<DTO>
    where QUERY_DTO : class, IDictionaryQueryDto
{
    [Get("new")]    Task<DTO> NewAsync(Guid? parentId = null, bool? isFolder = null, CancellationToken cancellationToken = default);
    [Post("lookup")] Task<PagedResult<HierarchicalLookupItemDto>> LookupAsync([Body] QUERY_DTO? dto = null, CancellationToken cancellationToken = default);
    [Post("moveto")] Task MoveToAsync(Guid id, Guid? newParentId = null, CancellationToken cancellationToken = default);
}
```

The concrete interface (flat example, `Branch`):

```csharp
[PathPrefix("/api/v1/dictionaries/Branches")]
public interface IBranchesApiClient : IDictionaryApiClient<BranchDto, PagedResult<BranchDto>, BranchQueryDto>
{
}
```

Hierarchical binds to the abstract **root** Dto (`CounterpartyNodeDto`), never the leaf — same rule as
`[KandraDictionaryForm]`'s placement in §9:

```csharp
[PathPrefix("/api/v1/dictionaries/Counterparties")]
public interface ICounterpartiesApiClient : IHierarchicalDictionaryApiClient<CounterpartyNodeDto, PagedResult<CounterpartyNodeDto>, CounterpartyQueryDto>
{
}
```

`[PathPrefix("/api/v1/dictionaries/{PluralName}")]` must match the server's generated controller route exactly
— it's the same `Name` value passed to `[KandraDictionaryForm(Name = "...")]` (§2), not independently chosen.
Refit's own source generator implements the interface body at compile time from these method-level `[Get]`/
`[Post]`/`[Put]`/`[Delete]` attributes — you only ever write the interface declaration, never an implementation.
See the memory note on Refit client design rules (no default-`null` request bodies beyond what the base
interfaces already declare, real DTO marker-interface constraints, extend `IKandraApiClient`/
`IKandraProtectedApiClient` transitively rather than redeclaring auth wiring, prefer `IReadOnlyCollection<T>`
over `List<T>` for read-only collection returns) if you're ever adding a method beyond the inherited set.

## If this reference goes stale

This document is a snapshot, not a live view. If the code disagrees with something here (a compile error citing
a signature that doesn't match, a missing member), trust the code — then update this file so the next person
doesn't hit the same surprise. The fastest way to re-verify a specific claim: grep the type/attribute name
under `src/KandraCore/Kandra.Attributes/`, `Kandra.Domain/`, `Kandra.Forms/`, `Kandra.Application.Abstractions/`,
`Kandra.Validators/`, or `Kandra.ClientLib.Common/ApiClients/` — every type named above lives in exactly one of
those six projects.
