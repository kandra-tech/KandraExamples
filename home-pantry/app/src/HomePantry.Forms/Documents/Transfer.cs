using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.DtoCommon.Printing;
using Kandra.Forms.Documents;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Validators.Documents;
using HomePantry.Enums;
using HomePantry.Forms.Dictionaries;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Documents;

[TypeId(HomePantryTypeIds.Transfer)]
[KandraDocumentForm(Name = "Transfers", ValidatorType = typeof(TransferValidator), QueryDtoType = typeof(TransferQueryDto))]
[EditFormTitle("EditTransfer")]
[AddFormTitle("CreateTransfer")]
[ViewFormTitle("ViewTransfer")]
[ListFormTitle("Transfers")]
[NavigationName("Nav_Transfers")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(TransferPrintForm))]
[Description("A transfer of items between locations (e.g. pantry to freezer, kitchen to cellar) preserving best-before batches.")]
public class TransferDto : DocumentDto
{
    [TablePart(AddCaption = "AddLine")]
    [Caption("Transfer_Lines")]
    [Description("Items moved with source and destination locations and quantity.")]
    public ICollection<TransferLineDto> Lines { get; set; } = [];
}

[Description("A transfer line specifying the product, source location, destination location, and quantity moved.")]
public class TransferLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Caption("Product")]
    [Search(typeof(ProductNodeDto))]
    [Required]
    [GridColumn(Order = 1, WidthPercent = 35)]
    public Guid ProductId { get; set; }

    [Caption("FromLocation")]
    [Search(typeof(LocationNodeDto))]
    [Required]
    [GridColumn(Order = 2, WidthPercent = 25)]
    public Guid FromLocationId { get; set; }

    [Caption("ToLocation")]
    [Search(typeof(LocationNodeDto))]
    [Required]
    [GridColumn(Order = 3, WidthPercent = 25)]
    public Guid ToLocationId { get; set; }

    [Caption("Quantity")]
    [GridColumn(Order = 4, WidthPercent = 15)]
    public decimal Quantity { get; set; }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("TransferPrintTitle")]
public sealed class TransferPrintForm : DocumentPrintForm
{
    [PrintTable(LineNumbers = true)]
    public List<TransferPrintLine> Lines { get; set; } = [];
}

public sealed class TransferPrintLine
{
    [PrintColumn(WidthPercent = 40)]
    [Caption("Product")]
    public string ProductName { get; set; } = "";

    [PrintColumn(WidthPercent = 20)]
    [Caption("FromLocation")]
    public string FromLocationName { get; set; } = "";

    [PrintColumn(WidthPercent = 20)]
    [Caption("ToLocation")]
    public string ToLocationName { get; set; } = "";

    [PrintColumn(WidthPercent = 20)]
    [Caption("Quantity")]
    [Format(PrintFormats.Quantity)]
    public decimal Quantity { get; set; }
}

public class TransferQueryDto : DocumentQueryDto
{
}

public class TransferValidator : DocumentValidator<TransferDto>
{
    public TransferValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.FromLocationId).NotEmpty();
            line.RuleFor(l => l.ToLocationId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
