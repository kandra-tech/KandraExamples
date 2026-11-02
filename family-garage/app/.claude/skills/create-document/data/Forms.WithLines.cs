// TEMPLATE — line-bearing document, Forms plane.
// Target: {{Config}}.Forms/Documents/{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §2, §3, §4, §9.

using System.ComponentModel;       // [Description]
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;   // only needed if you add [Search]/[Dropdown] fields
using Kandra.Attributes.Entities;  // [TypeId]
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Documents;
using Kandra.Forms.Querying;
using Kandra.Validators.Documents;
using {{Config}}.Enums;
using Microsoft.Extensions.Localization;

namespace {{Config}}.Forms.Documents;

[TypeId({{TypeIdsClass}}.{{Name}})]
[KandraDocumentForm(Name = "{{PluralName}}", ValidatorType = typeof({{Name}}Validator), QueryDtoType = typeof({{Name}}QueryDto))]
[EditFormTitle("Edit{{Name}}")]
[AddFormTitle("Create{{Name}}")]
[ViewFormTitle("View{{Name}}")]
[ListFormTitle("{{PluralName}}")]
[NavigationName("Nav_{{PluralName}}")]
// OPTIONAL — only if this document prints. Both attributes together, see create-print-form:
// [SupportedExportFormats(ExportFormat.Pdf)]
// [SupportedPrintForm(typeof({{Name}}PrintForm))]
// [Description] is mandatory on every Dto — it's what the MCP server's describe_entity_type/
// list_entity_types tools surface to an AI agent as the entity's actual meaning; [Caption] is only a
// short UI label, not an explanation. See reference.md §2/§9.
[Description("TODO — one or two sentences: what a {{Name}} represents and what submitting it does " +
    "(which registers it writes / what it posts, if anything).")]
public class {{Name}}Dto : DocumentDto
{
    // Add resx keys for every title/nav key above, PLUS {{Name}}AddedSuccessfully/
    // {{Name}}UpdatedSuccessfully (read directly by the generated Add/Update pages, no attribute
    // references them) — reference.md §9. The resx files (English required, uk/ru optional).

    // OPTIONAL — only if Code needs its own caption or list-column behavior; most documents should just
    // inherit it from DocumentDto (Loc["Code"]) unmodified. Note DocumentBase.Code carries no [MaxLength]
    // at all, unlike DictionaryBase.Code.
    // [Caption("SomeResourceKey")] [ListFormColumn(Sortable = false, Filterable = false)]
    // public override string Code { get; set; }

    // Field example — FK to a dictionary via server-filtered autocomplete:
    // [Required] [Caption("SomeDictionary")] [Search(typeof(SomeDictionaryDto))]
    // [Description("What this reference means and how it's used.")]
    // public Guid SomeDictionaryId { get; set; }

    [Caption("Description")]
    [ListFormColumn]
    [Description("Free-text note about this {{Name}}.")]
    public string? Description { get; set; }

    // OPTIONAL — a header total, if this document has one. Server-recomputed in OnSaveAsync
    // (Application.WithLines.cs) — never editable from the client.
    // [NotEditable] [Calculated] [Caption("Total")] [ListFormColumn(Filterable = false)]
    // [Description("Sum of every line's Sum - computed server-side on save, never set directly.")]
    // public decimal Total { get; set; }

    [Required]
    [Caption("Lines")]
    [Description("TODO — what each line represents.")]
    public ICollection<{{Name}}LineDto> Lines { get; set; } = [];
}

// [Description] is mandatory on the line Dto too — it's a distinct entity in describe_entity_type's field
// schema (EntityFieldMetadataBuilder.DescribeTabularLines), not just an implementation detail of the header.
[Description("TODO — one sentence: what one line/row represents on a {{Name}}.")]
public class {{Name}}LineDto
{
    [Required]
    public Guid Id { get; set; }

    // Line fields go here, each with its own [GridColumn(Order = N, ...)] — distinct from the header's
    // [ListFormColumn] above. There is no fixed shape: a line may reference zero, one, or several
    // dictionaries, carry quantities/amounts, or neither — this is entirely your document's own domain,
    // not something the engine assumes. Typical patterns, none mandatory, delete/adjust as needed. Add
    // [Description] whenever a field's meaning isn't obvious from its name/caption alone:
    //
    // A dictionary FK (name/target Dto type are yours to choose — this is just one example):
    // [Required] [Caption("SomeDictionary")] [Search(typeof(SomeDictionaryDto))] [GridColumn(Order = 0, WidthPercent = 25)]
    // [Description("What this reference means and how it's used.")]
    // public Guid SomeDictionaryId { get; set; }
    //
    // An "order line" quantity/price/sum shape (only if it genuinely applies — see GoodsReceiptLineDto):
    // [Required] [Caption("Quantity")] [GridColumn(Order = 1, WidthPercent = 17)]
    // public decimal Quantity { get; set; }
    //
    // [Required] [Caption("Price")] [GridColumn(Order = 2, WidthPercent = 17)]
    // public decimal Price { get; set; }
    //
    // [NotEditable] [Calculated] [Caption("Sum")] [GridColumn(Order = 3, WidthPercent = 17)]
    // [Description("Quantity * Price - computed server-side, never set directly.")]
    // public decimal Sum { get; set; }
}

public class {{Name}}QueryDto : DocumentQueryDto
{
    // Almost always empty — DateRange/Id (from DocumentQueryDto) already cover standard list/lookup
    // filtering. Only add a field here if this document's list page needs an extra filter.
}

public class {{Name}}Validator : DocumentValidator<{{Name}}Dto>
{
    public {{Name}}Validator(IStringLocalizer localizer) : base(localizer)
    {
        // base(localizer) already covers Code NotEmpty — don't duplicate that.
        // RuleFor(x => x.SomeDictionaryId).NotEmpty().WithMessage(localizer["SomeDictionaryRequired"]);
        RuleFor(x => x.Lines).NotEmpty().WithMessage(localizer["LinesRequired"]);
        // Example line-level rule, only if the line has a Quantity field (see the LineDto template above):
        // RuleForEach(x => x.Lines).Must(l => l.Quantity > 0).WithMessage(localizer["QuantityMustBePositive"]);
    }
}

// {{Name}}QueryDtoValidator lives in the Application-plane file (Application.WithLines.cs), not here —
// same convention as dictionaries (PagedQueryDtoValidator<T> is in Kandra.Application.Validation).

// OPTIONAL — the print form goes in this same file when this document prints (see create-print-form; it needs
// `using Kandra.Attributes.Printable; using Kandra.DtoCommon.Printing; using Kandra.Forms.Printable;`).
// Uncomment and adapt. Nothing here positions or styles anything: the engine composes the title line, the
// party blocks, the totals block, the amount in words, the signatures, the footer and the draft marker from
// the types and roles below, and the company letterhead from the configuration's IPrintFrameProvider.
//
// [PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
// [PrintTitle("{{Name}}PrintTitle")]   // the document KIND ("Delivery note"), a resx key; not the navigation caption
// public sealed class {{Name}}PrintForm : DocumentPrintForm   // Number/Date/IsSubmitted/GeneratedAt/GeneratedBy come from the base
// {
//     [Caption("Counterparty")]
//     public required PrintParty Counterparty { get; set; }   // filled from the configuration's party service, see Application.WithLines.cs
//
//     [Required]
//     [PrintTable(LineNumbers = true)]
//     public required ICollection<{{Name}}PrintLine> Lines { get; set; }
//
//     public PrintVatMode VatMode { get; set; } = PrintVatMode.NotPayer;   // NotPayer + empty Taxes: "Без ПДВ", no VAT rows
//     public IReadOnlyList<PrintTax> Taxes { get; set; } = [];
//
//     [PrintRole(PrintRole.GrandTotal)]
//     public decimal Total { get; set; }
//
//     [PrintRole(PrintRole.Currency)]
//     public string CurrencyCode { get; set; } = "UAH";
//
//     public IReadOnlyList<PrintSignature> Signatures { get; set; } = [];
// }
//
// public sealed class {{Name}}PrintLine
// {
//     [PrintColumn] [Caption("Item")] public string ItemName { get; set; } = string.Empty;
//     [PrintColumn] [Caption("Quantity")] [Format(PrintFormats.Quantity)] public decimal Quantity { get; set; }
//     [PrintColumn] [Caption("Price")] public decimal Price { get; set; }
//     [PrintColumn] [Caption("Sum")] public decimal Sum { get; set; }
// }
