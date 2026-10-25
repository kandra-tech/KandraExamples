---
name: create-print-form
description: Add or extend a [PrintForm]-decorated PDF/CSV print form in a Kandra configuration - a document's Print button (invoice, waybill, receipt) or a report's export. Use when the user asks for a print form, a "nicer" PDF/CSV export, a letterhead, parties/signatures/VAT/totals on a printout, draft handling, a report's parameters line or grouped columns, or asks to change how an existing printout looks.
---

# Creating / extending a print form

> **Note:** This configuration is a bare template: it has no documents or reports of its own yet, so the worked
> examples below (`WaybillPrintForm`, `RestsReportDataDto`, ...) live in the reference configuration, **KandraWms**
> (github.com/kandra-tech/KandraWms), and are named by their paths there. Treat them as illustrative and substitute your
> own domain's names; only the pattern matters. What this template *does* ship is the letterhead: the `CompanyName`/`CompanyLogo`
> constants, `FamilyGaragePrintFrameProvider` and the `CompanySettings` data processor (§8).

A print form is a plain, attribute-decorated Dto rendered to PDF/CSV entirely by a compile-time Roslyn generator, no
hand-written export code. **You declare what each property means; the engine decides where it goes and how it
looks.** A form made this way needs almost no layout attributes: the page frame (letterhead, footer, page numbers),
the typography, the title line, the totals block, the amount in words and the signature block all come from the
engine. Two flavors exist:

- **Documents** — a separate `{Name}PrintForm : DocumentPrintForm`, built in `{Name}Behavior.OnPrintAsync` from a saved
  document and opened in a new browser tab. Worked example: `WaybillPrintForm` (`KandraWms.Forms/Documents/Waybill.cs`) +
  `WaybillBehavior.OnPrintAsync` (`KandraWms.Application/Documents/Waybill.cs`). `Invoice`, `GoodsReceipt`, `Transfer`,
  `BankReceipt` are the same pattern; `BankPayment`'s `PaymentInstructionPrintForm` is the table-less variant.
- **Reports** — the `[PrintForm]` class is the *data* half of the report's `ReportResult<TFilters, TData>` split
  (`RestsReportDataDto : ReportPrintForm`; the `RestsReportResult` envelope never carries `[PrintForm]` itself).
  Worked examples: `RestsReportDataDto` (parameters line, grouped rows), `TrialBalanceReportDataDto` (column groups),
  `ReconciliationActReportDataDto` (a report that is a document sent to a partner: letterhead, parties, signatures).

See `docs/kandra-architecture.md` §2.10 for the full design and `docs/kandra-print-forms-redesign.md` for the reasoning.

## 0. Engine pieces this relies on (don't hand-roll any of this)

- `Kandra.Attributes.Printable` — the attribute vocabulary (below); `Kandra.DtoCommon.Printing` — the value types
  (`PrintParty`, `PrintSignature`, `PrintTax`, `CodeDateRef`, `CodeNameRef`, `PrintFormats`); `Kandra.Forms.Printable`
  — the `DocumentPrintForm`/`ReportPrintForm` base classes. All three sit on the Forms plane, so a client can see them.
- `Kandra.Printing` — the rendering runtime (`IPrintRenderTarget`, the PDF/CSV targets, `PrintTheme`,
  `IPrintFrameProvider`, `IPrintRendererResolver`). You never call the targets directly.
- `Kandra.Generators.Printing`'s `PrintRendererGenerator` — discovers every `[PrintForm]` class at compile time and
  emits, per class, `{TypeName}CsvRenderer`/`{TypeName}PdfRenderer` plus a shared `{TypeName}RenderCore` into your
  configuration's `*.Application/Generated.Net/Kandra.Generators.Printing/...`. Inspect it there when output looks
  wrong: read the emitted file, don't guess from the attributes alone (the engine repository's `inspect-generated-output`
  skill covers it if you have that checked out). **Never hand-edit it.**
- `AddGenerated{ConfigurationName}PrintRenderers()` (here `AddGeneratedFamilyGaragePrintRenderers()`) — called once in
  `ConfigureServices.cs`, registers both renderers for every discovered class under the DI key `"default"`. A new
  `[PrintForm]` class needs no extra DI. `AddKandraPrinting()` (called by `AddKandraApplication`) registers
  `IPrintRendererResolver`, the one service you actually call.
- `IPrintFrameProvider` — the company letterhead, registered **once per configuration** after `AddKandraPrinting()`
  (here `FamilyGaragePrintFrameProvider`, reading the `Company*` constants as of the document's date). Forms never carry
  company details; see §8.
- `IPrintPartyService` — **configuration-level**, not an engine type, and not part of this template (it has no party
  dictionary): it turns the company constants and a counterparty id into `PrintParty` values. KandraWms has one
  (`KandraWms.Application/Services/Document/IPrintPartyService.cs`: `GetCompanyAsync(asOf)`,
  `GetCompanyAsync(asOf, bankAccountId)`, `GetCounterpartyAsync(id)`); write the equivalent over your own
  counterparty dictionary the first time a document needs a party block.

## 1. Pick the base class and the frame

| You are printing | Derive | Default frame |
|---|---|---|
| A document | `DocumentPrintForm` | `Letterhead` |
| A report | `ReportPrintForm` | `Internal` |

The bases carry the properties every form needs, already attributed: for a document `Number`, `Date`, `IsSubmitted`
(the draft signal) and the audit pair `GeneratedAt`/`GeneratedBy`; for a report just the audit pair. **Never
redeclare them**, and never put audit data in the body: it lands in the page footer on every page.

```csharp
[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("WaybillPrintTitle")]          // "Видаткова накладна": the document kind, not the nav caption
public sealed class WaybillPrintForm : DocumentPrintForm { /* ... */ }
```

`[PrintTitle(key)]` is a resx key naming the document **kind** ("Delivery note"), separate from the navigation
caption. For a document with `Number` and `Date` roles the engine composes the title line `"{Title} № {Number} від
{Date}"` (the date pattern lives in the engine's `Print_TitleLine` resource per culture) and repeats a short version
as a running header from page 2. Add the key to your resx in the English resx (uk/ru optional).

**`Frame`** — `Letterhead` (the company block above the title: a document sent to a partner), `Internal` (title + a
parameters line, no letterhead: reports), `None` (no letterhead and no parameters line: statutory forms whose layout
is prescribed, and forms where a party block already says who the company is, like the payment instruction). An
explicit `Frame` always wins over the base class's default. Every frame draws the footer band.

**`Drafts`** — what printing a not-yet-submitted document does (needs the `Status` role, which `DocumentPrintForm`
already has):

| Value | Behavior | Use for |
|---|---|---|
| `Marked` (default) | prints, with a diagonal "draft" marker on every PDF page and a red line under the title; CSV has no marker | every ordinary document |
| `Allowed` | prints exactly like a submitted document | internal working papers |
| `Forbidden` | refused in every format and through every entry point (the generated renderer throws `PrintDraftForbiddenException`, HTTP 409); the generated Print button is disabled until the document is submitted | statutory forms |

The attribute is per print form, so one document can have a lenient layout and a strict one (`[SupportedLayoutTag]`).

## 2. Parties and document references

```csharp
[Caption("Supplier")] public required PrintParty Supplier { get; set; }   // muted role label, bold name, requisites line
[Caption("Buyer")]    public required PrintParty Buyer { get; set; }
[Caption("Recipient")] public PrintParty? Recipient { get; set; }          // null leaves the block out

[Caption("Warehouse")] public string Warehouse { get; set; } = string.Empty;   // a plain scalar is a label/value row

[Caption("Basis")]
[PrintRefKind("InvoicePrintTitle")]        // names the kind before "№": "Рахунок № Р-000041 від 18.01.2025"
public CodeDateRef? SourceInvoice { get; set; }
```

- `PrintParty(Name, TaxCode, VatNumber, Iban, BankName, BankMfo, Address, Phone)` renders its requisites in a fixed,
  locale-aware order and skips empty parts. Fill it from `IPrintPartyService`; never hand-assemble requisites strings.
- A `CodeDateRef(Id, Code, Date)` is a document reference row; without `[PrintRefKind]` it reads "№ X від d".
- Properties with no attribute are **references**: label/value rows under the parties, in declaration order; a `null`
  value hides its row. `[PrintRole(PrintRole.Note)]` makes a free-text paragraph under the table instead.
- Resolve display names through `ILookupRepository`, never the entity's nav properties (the CRUD service's plain
  `GetAsync` loads only table-part rows). **Hierarchical dictionaries**: `GetDictionaryAsync<T>` is DI-resolvable
  only for the hierarchy's *root* type (`CounterpartyNode`, `ItemNode`), not a leaf; a wrong one fails at request
  time ("No service for type ..."), not at compile time. Check
  `FamilyGarage.Persistence/Generated.Net/.../GeneratedFamilyGaragePersistenceDiExtensions.g.cs`.

## 3. The table

```csharp
[Required]
[PrintTable(LineNumbers = true)]
public required ICollection<WaybillPrintLine> Lines { get; set; }

public sealed class WaybillPrintLine
{
    [PrintColumn] [Caption("Item")]  public string ItemName { get; set; } = string.Empty;
    [PrintColumn(Under = nameof(ItemName), Prefix = "SkuPrefix")] [Caption("Sku")] public string? Sku { get; set; }
    [PrintColumn(Align = TextAlign.Center)] [Caption("UnitShort")] public string Unit { get; set; } = string.Empty;
    [PrintColumn] [Caption("Quantity")] [Format(PrintFormats.Quantity)] public decimal Quantity { get; set; }
    [PrintColumn(VatSuffix = true)] [Caption("Price")] public decimal Price { get; set; }
    [PrintColumn(VatSuffix = true)] [Caption("Sum")]   public decimal Sum { get; set; }
}
```

**The type decides the look**; declare only what differs:

- Alignment: numbers right, dates centred, text left (`Align = TextAlign.…` overrides; `StyleKey = "Number"` is gone).
- Format: `decimal` is `N2`, `DateTime` is `d`. A quantity wants `[Format(PrintFormats.Quantity)]` (`#,0.###`: "50", "12,5",
  never "50,000"); `PrintFormats.Money` is `N2`. `[Format]` always overrides.
- Widths: with none declared, numeric columns get a measured width and text columns share the rest. Declare
  `WidthPercent` **or** `WidthMm` on every column or none (mixing is `KANDRAPRINT004`; percentages over 100 is `005`).
- `[PrintTable(LineNumbers = true)]` — a leading "№" column (PDF only; CSV omits it).
- `[PrintColumn(Under = nameof(Host), Prefix = "ResxKey")]` — render the value as a small muted second line inside
  the host column's cell (an item's SKU under its name); `Prefix` is a resx key printed before it ("Арт. "). In CSV it
  stays its own column. The host must be a plain column, not itself an `Under` column (`KANDRAPRINT008`).
- `[PrintColumn(VatSuffix = true)]` — appends "без ПДВ"/"з ПДВ" to the caption by the form's `PrintVatMode` (nothing for
  a non-payer). The form must have a `PrintVatMode` property (`KANDRAPRINT009`).
- `[Totals(Aggregate.Sum|Count|Avg)]` — a bold totals row after the last line. `GroupOnly = true` totals a column in the
  group rows only (a quantity summed across warehouses is meaningless).
- `[PrintTable(HideWhenEmpty = true)]` drops an empty table with its totals row (the documents a payment settles);
  `[PrintTable(SummaryLine = false)]` drops "Всього найменувань N" where the rows are not items.
- At most one `[PrintTable]` per form (`KANDRAPRINT002`); the table is optional (see the payment instruction).

**Reports only — column groups and row groups** (`TrialBalanceReportDataDto`, `RestsReportDataDto`):

- `[PrintColumn(Group = "ResxKey")]` merges **adjacent** columns of one key under a shared header cell, a two-row
  heading that repeats on every page; use it when several columns share a heading ("Turnover" over "Debit" and
  "Credit"). `[PrintColumn(Caption = "ResxKey")]` is a caption for paper only, because the `[Caption]` also names
  the on-screen grid column. Columns of one group must sit next to each other (`KANDRAPRINT013`). CSV keeps one
  header row, `"{Group} {Caption}"`.
- `[PrintTable(GroupBy = nameof(Line.Property))]` writes a bold full-width row on every change of that value and a
  subtotal row after each run. **Use it when the lines have a natural parent** (rows per warehouse, per account) and
  the reader will want per-group totals; skip it for a flat list. **The behavior must sort the rows by that
  property first**; an unsorted list gives one group per run. CSV ignores grouping. A name that is not a readable
  property of the line type is `KANDRAPRINT012`. `PageBreakPerGroup = true` starts every group after the first on a new
  PDF page (the PDF target ends the table, breaks the page and continues in a new table with the header repeated): use
  it for a register kept one sheet per day, like `CashBookReport`, whose group key is a print-only `Sheet` property
  (no `[PrintColumn]`, so it is the heading and not a column) and whose rows carry their own balance and signature lines
  instead of `[Totals]`. A group's subtotal row and the grand total still work as before.

## 4. Totals, VAT and the amount in words

The engine composes the whole totals block from four properties; you never format a total or spell an amount:

```csharp
public PrintVatMode VatMode { get; set; } = PrintVatMode.NotPayer;
public IReadOnlyList<PrintTax> Taxes { get; set; } = [];            // empty ⇒ no VAT rows

[PrintRole(PrintRole.Subtotal)]   public decimal Subtotal { get; set; }   // shown only when prices exclude VAT
[PrintRole(PrintRole.GrandTotal)] public decimal Total { get; set; }      // the bold line + "N items, amount in words"
[PrintRole(PrintRole.Currency)]   public string CurrencyCode { get; set; } = "";   // selects the words and the short name; set by OnPrintAsync, never a literal
```

| `PrintVatMode` | When | Totals block |
|---|---|---|
| `NotPayer` (the sensible default until the configuration has a VAT model) | the company is not a VAT payer | "Всього" and "Без ПДВ"; **never a VAT row** (a zero VAT line reads as a 0% supply) |
| `PricesExcludeVat` | VAT payer, net prices | "Разом без ПДВ", one "ПДВ n%" row per `PrintTax`, bold "Всього з ПДВ", "У т.ч. ПДВ: words" |
| `PricesIncludeVat` | VAT payer, gross prices | bold "Всього з ПДВ", then "у т.ч. ПДВ n%" per tax |
| `None` | a payment document (bank receipt/payment) whose amount may contain VAT | bold "Всього", words; no VAT statement at all |

At most one `Number`, `Date`, `GrandTotal`, `Subtotal` and `Currency` role per form (`KANDRAPRINT006`).

## 5. Signatures

```csharp
public IReadOnlyList<PrintSignature> Signatures { get; set; } = [];
// PrintSignature(Caption /* resx key */, PersonName, WithProxy: "за довіреністю №", Stamp: "М.П.")
new PrintSignature("ShippedBy", Stamp: true), new PrintSignature("ReceivedBy", WithProxy: true)
```

The signature block is kept on one page together with the totals. The captions are resx keys.

## 6. Fill it in `OnPrintAsync` (documents) or `Export` (reports)

Documents — `IDocumentBehavior<TEntity>.OnPrintAsync` has a default no-op (`null` means "this document has no print
form"). Override it, build the form, and render with **`RenderAsync`**, which reads the letterhead asynchronously as of
the form's `Date`-role value; the synchronous `Render` has no letterhead:

```csharp
public async ValueTask<PrintRenderOutput?> OnPrintAsync(Waybill entity, ExportFormat format, string? layoutTag,
    ILookupRepository lookup, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone,
    CancellationToken cancellationToken)
{
    var form = new WaybillPrintForm
    {
        Number = entity.Code,
        Date = entity.Date,
        IsSubmitted = entity.IsActive,
        GeneratedAt = DateTime.UtcNow,                                   // UTC: converted to the client's zone on render
        GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken),

        Supplier = await printParties.GetCompanyAsync(entity.Date, cancellationToken),
        Buyer = await printParties.GetCounterpartyAsync(entity.CounterpartyId, cancellationToken),
        // ... references, Lines, Total, CurrencyCode, VatMode, Signatures
    };

    return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
}
```

`authorizationChecker` (`IAuthorizationChecker`) and `printParties` are ordinary constructor dependencies of the
behavior; `lookup`/`renderer`/`localizer` are passed in. `PrintHeaderUser.TryGetNameAsync` is a small
configuration-level helper (KandraWms has it in `KandraWms.Application/Reports/PrintHeaderUser.cs`; this template does
not, so add one with your first print form): it looks the current user up through `ILookupRepository` and returns
`null` instead of throwing when the caller has no resolvable user identity (an API key), because the name is cosmetic.
**Never write a literal currency (`"UAH"`) or company detail into a print form:** take the currency from the document
(its account's currency) or, for company-wide amounts, from a home-currency constant as of the document date.

**Resolving names in `OnPrintAsync`** (an account's name, a currency's code): collect the ids the form needs and ask the
`ILookupRepository` it receives once per dictionary type. It returns `IReadOnlyList<CodeNameRef>` (`Id`, `Code`, `Name`),
not entities:

```csharp
var accounts = (await lookup.GetDictionaryAsync<Account>(entity.Lines.Select(l => l.AccountId).Distinct().ToList(), ct))
    .ToDictionary(r => r.Id);
// accounts[line.AccountId].Name, .Code
```

For a field that is not on `CodeNameRef` (an account's currency id), read the entity through its repository
(`IEntityRepository<Account>`) injected into the behavior.

Reports — fill `GeneratedAt`/`GeneratedBy` and the parameter properties on `result.Data` (the `TData` half) in
`GetDataAsync`, then `Export` delegates to the resolver. Build the filename yourself; the renderer only returns bytes,
a content type and an extension:

```csharp
public IFileData Export(RestsReportResult report, ExportFormat format, string? layoutTag,
    IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
{
    var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
    return new FileData { FileName = $"RestsReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
                          ContentType = output.ContentType, Content = output.Content };
}
```

A report that is a **document sent to a partner** (`Frame = PrintFrame.Letterhead`) must override `ExportAsync`
instead (it returns `ValueTask<IFileData?>`) and render through `RenderAsync`, otherwise it prints with no
letterhead; `ReconciliationActReportBehavior` is the example.

**Reports: the parameters line.** A property with `[PrintRole(PrintRole.Parameter)]` becomes one `"{Caption}:
{value}"` part of the muted line directly under the title, joined with " · ", empty values skipped. Put the report's
**filters** there (period, warehouse, item), echoing what the user chose: `IReadOnlyCollection<CodeNameRef>` for
id filters (resolved through `lookup.GetDictionaryAsync<T>(ids, ct)`, which returns `IReadOnlyList<CodeNameRef>` (`Id`, `Code`, `Name`), not entities; cut after 5 with "та ще N"), `DateRangeDto`
for a period, `[Format("d")]` for a date. Don't use `Parameter` for data that belongs in the table.

**Report line properties that name a dictionary** — keep two plain `Code`/`Name` wire properties and combine them
with `CodeNameRef.Combine(code, name)` for display, never a hand-rolled `$"{code} - {name}"`.

**Client — the Print button** (documents). The generated Update and View pages already show it, gated on
`[SupportedPrintForm]`; nothing to write. Declare the discovery metadata on the document's `[KandraDocumentForm]` Dto
(not on the print form): `[SupportedExportFormats(ExportFormat.Pdf)]` and `[SupportedPrintForm(typeof(WaybillPrintForm))]`.
Reports declare `[SupportedPrintForm(typeof(RestsReportDataDto))]`, the `TData` half. `[SupportedLayoutTag(key, name)]`
declares one alternate layout beyond `"default"`; add it only when a hand-registered alternate renderer actually
exists.

A document that prints in some states and not in others (`CashReceipt`/`CashPayment` with a card payment have no
cash order) hides the button **per record**: `[SupportedPrintForm(typeof(CashReceiptOrderPrintForm), Visible = nameof(CashReceiptUiBehavior.CanPrint))]` on the Dto and a
`public bool CanPrint()` on its UI behavior (`CashReceiptUiBehavior`). It only hides the button, so `OnPrintAsync` must still
refuse a record that cannot be printed - the API works without the page.

## 7. What goes where in CSV (and PDF)

| Part of the form | PDF | CSV |
|---|---|---|
| `Number`, `Date`, `[PrintTitle]` | title line, footer, running header | omitted |
| `PrintParty`, references, `CodeDateRef` | party blocks, label/value rows | omitted |
| `Parameter` properties | parameters line | omitted |
| the table | table (with "№", `Under` as a second line, group header rows, subtotal rows) | **one header row and one row per line**; numbers are raw (culture decimal separator, **no group separator**, so they re-import), no "№", an `Under` value is its own column, a column group becomes `"{Group} {Caption}"`, grouping ignored |
| totals row, `Subtotal`/`Total`/`Taxes`, amount in words | totals block | omitted (the row is not written either) |
| `Note`, `Signatures` | note, signature block | omitted |
| `Audit`, `Status` | footer, draft marker | omitted |
| legacy `[PrintSectionRef]` fields (§9) | label/value sections | only when the form has **no table**: one `caption,value` row each |

A form with no table and no legacy sections (the payment instruction) has an **empty CSV**; declare
`[SupportedExportFormats(ExportFormat.Pdf)]` on its Dto. Documents are declared Pdf-only by default but not
engine-limited: widening the declaration to include `Csv` is the only change needed to expose it.

**Excel (`ExportFormat.Excel`)** needs no code either: add it to `[SupportedExportFormats]` (a report then gets an "Export to Excel" button). It
writes real numbers and dates (the column's `[Format]` becomes the Excel number format; give a number column an explicit `[Format("N2")]`
or similar, or it is `General`), the table as an Excel Table with filters and a frozen header, `[Totals]` as `SUBTOTAL` formulas, a grouped table
as outline levels, parameters/parties/references as label/value rows above the table. Signatures and the audit text are not on the sheet.

### Pivot (cross-tab) tables

A `[PrintTable]` property that also carries `[PivotTable]` prints as a pivot instead of a flat table: the line type's
`[PrintColumn]`s are ignored, and the matrix comes from its `[PivotRow]`/`[PivotColumn]`/`[PivotValue]` properties
(see `create-report`'s `reference.md` §1). CSV gets one grid with a totals column and row. PDF lays the pivot out at
8pt, shrinking to 6pt if that fits the page width; failing that the columns split into bands, each a full table on its
own page with the row headers repeated and the totals column in the last band. `[Page]` still decides the paper: an
A3 landscape form needs fewer bands than A4. `ChessReport` (`Forms/Reports/Chess.cs`) is the example.

## 8. The letterhead and the logo (once per configuration, not per form)

A `Letterhead`-framed form shows the company block from `IPrintFrameProvider`, which the configuration registers once
after `AddKandraPrinting()`:

```csharp
services.AddScoped<IPrintFrameProvider, FamilyGaragePrintFrameProvider>();   // the last registration wins
```

The provider reads the `Company*` constants **as of the document's date** (so a reprint of an old document shows the
details and logo it had then) and returns a `PrintLetterhead(name, requisites line, contacts line, logo bytes)`, or
`null` when no company name is set (a form then prints with no letterhead rather than an empty block). The logo is the
`CompanyLogo` constant (a `Guid?` blob reference), edited through the `CompanySettings` data processor, not the
constants page; see `create-constant` and `create-data-processor`. **Don't put company name, tax code or logo on a
form.** The only company data on a form is the supplier `PrintParty`.

**In this template** the letterhead works out of the box with two details: `CompanyNameConstant` and `CompanyLogoConstant`
(`FamilyGarage.Application/Constants/`), `FamilyGaragePrintFrameProvider` (`FamilyGarage.Application/Printing/`,
registered in `ConfigureServices.cs` right after the print renderers) and the **Company settings** data processor (Build menu).
Open it and set the company name (an empty name prints no letterhead) and the logo, and every `Letterhead`-framed form prints
them. To carry more on the letterhead (tax code, address, phone, bank requisites) follow the `extend-company-settings` skill.

## 9. The escape hatch: sections and styles, for statutory or fixed layouts only

When a layout is **prescribed** (a form whose geometry is set by regulation) the older geometric vocabulary is still
there. It is not how to build an ordinary document or report: roles and types cover those, and new forms in this
repo do not use it.

| Scope | Attribute | Notes |
|---|---|---|
| Class, repeatable | `[PrintSection(key, Columns = n)]` | a flow section: an n-column label/value grid; `PositionX/Y/Width` anchor it |
| Class, repeatable | `[FixedSection(key, HeightMm = …)]` | an always-anchored rectangle at absolute mm coordinates (`PositionX/Y/Width`) |
| Class, repeatable | `[PrintStyle(key)]` — `Align`/`FontSize`/`Bold`/`Italic`/`Color`/`BackgroundColor`/`Borders` | referenced by `StyleKey` from a section/column; never stacked on a property |
| Property | `[PrintSectionRef(key)]` | assigns a scalar to a section; **an explicit section wins over a role** |
| Class | `[Page(Size, Orientation, MarginsMm)]` | paper geometry (also used by ordinary forms: reports set landscape) |

Use `Frame = PrintFrame.None` for such a form; roles other than `Audit`/`Status` are ignored inside `[FixedSection]`.
Sections have no title of their own, so give each field a distinct caption ("Payer IBAN" / "Recipient IBAN", not two
"IBAN"s). There is **no live example of the escape hatch in the reference configuration today**; the engine's own tests
(`SectionsOnlyPrintGeneratorTests`, `PrintRolesGeneratorTests`) are the reference for the syntax.

## 10. Diagnostics (`KANDRAPRINT…`) — what triggers each

| Id | Trigger |
|---|---|
| `001` | retired (a missing `[PrintTable]` is valid); the id is never reused |
| `002` | more than one `[PrintTable]` property on a form |
| `003` | `[PrintSectionRef]`/`StyleKey` names a key with no matching `[PrintSection]`/`[FixedSection]`/`[PrintStyle]` |
| `004` | a table's columns mix `WidthPercent` and `WidthMm` |
| `005` | a table's `WidthPercent` values sum to more than 100 |
| `006` | a second `Number`, `Date`, `GrandTotal`, `Subtotal` or `Currency` role on one form |
| `007` | `[PrintRole]` on a table-**line** property (roles describe the form; use `[PrintColumn]`) |
| `008` | `[PrintColumn(Under = …)]` names no plain host column (missing, or itself an `Under` column) |
| `009` | `[PrintColumn(VatSuffix = true)]` on a form with no `PrintVatMode` property |
| `010` | `Drafts = Forbidden` on a form with no `Status` role |
| `011` | a role on a property whose type cannot carry it (e.g. `GrandTotal` on a `string`) |
| `012` | `[PrintTable(GroupBy = …)]` names no readable property of the line type |
| `013` | a column `Group` key on columns that are not adjacent |

## 11. Verify

1. `dotnet build Kandra.slnx` — no `KANDRAPRINT` diagnostic; inspect the generated `{Name}PdfRenderer.g.cs` /
   `{Name}RenderCore` under `*.Application/Generated.Net/Kandra.Generators.Printing/...` for the fields and columns you
   expect, in order.
2. `dotnet test Kandra.slnx`. For a visual check set `KANDRA_SAMPLE_OUT` to a folder: the Waybill/report print tests
   write the rendered PDFs there (rasterize with PyMuPDF; PdfPig `Letters`/`Words` for position and font-size
   assertions). `WaybillPrintTests` is the pattern for a document test (real SQLite lookups, real localizers, the
   letterhead provider).
3. Run it:
   - Documents: open a **saved** document's Update or View page, click Print, confirm a PDF opens in a new tab with the
     letterhead, the title line, the parties, the table, the totals and the signatures, and a draft carries the marker.
     The button is absent on the Add page. POST `print/{id}` with `{"format":"Csv"}` and confirm table-only CSV; with
     `{"format":"Excel"}` (or an unregistered `layoutTag`) confirm a clean 404 (`PrintRendererNotFoundException`).
   - Reports: click both export buttons; the PDF has the title, the parameters line, the table (header repeated per
     page, group rows if any), a totals row, and the footer; the CSV has only the table.
   - Check the dev server's console for server-side exceptions, not just a 200 — a lookup mistake (the
     hierarchical-dictionary gotcha in §2) throws at request time.
