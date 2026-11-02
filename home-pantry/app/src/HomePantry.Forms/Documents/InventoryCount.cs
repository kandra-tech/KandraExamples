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

[TypeId(HomePantryTypeIds.InventoryCount)]
[KandraDocumentForm(Name = "InventoryCounts", ValidatorType = typeof(InventoryCountValidator), QueryDtoType = typeof(InventoryCountQueryDto))]
[EditFormTitle("EditInventoryCount")]
[AddFormTitle("CreateInventoryCount")]
[ViewFormTitle("ViewInventoryCount")]
[ListFormTitle("InventoryCounts")]
[NavigationName("Nav_InventoryCounts")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(InventoryCountPrintForm))]
[Description("An inventory count at a location, reconciling actual quantities against system stock.")]
public class InventoryCountDto : DocumentDto
{
    [Caption("Location")]
    [Search(typeof(LocationNodeDto))]
    [Required]
    [ListFormColumn]
    [Description("The location where items were counted.")]
    public Guid LocationId { get; set; }

    [TablePart(AddCaption = "AddLine")]
    [Caption("InventoryCount_Lines")]
    [Description("Products counted with their actual physically verified quantities.")]
    public ICollection<InventoryCountLineDto> Lines { get; set; } = [];
}

[Description("A counted product line specifying the product and the physically counted quantity at the location.")]
public class InventoryCountLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Caption("Product")]
    [Search(typeof(ProductNodeDto))]
    [Required]
    [GridColumn(Order = 1, WidthPercent = 60)]
    public Guid ProductId { get; set; }

    [Caption("CountedQuantity")]
    [GridColumn(Order = 2, WidthPercent = 40)]
    public decimal CountedQuantity { get; set; }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("InventoryCountPrintTitle")]
public sealed class InventoryCountPrintForm : DocumentPrintForm
{
    [Caption("Location")]
    public string LocationName { get; set; } = "";

    [PrintTable(LineNumbers = true)]
    public List<InventoryCountPrintLine> Lines { get; set; } = [];
}

public sealed class InventoryCountPrintLine
{
    [PrintColumn(WidthPercent = 60)]
    [Caption("Product")]
    public string ProductName { get; set; } = "";

    [PrintColumn(WidthPercent = 40)]
    [Caption("CountedQuantity")]
    [Format(PrintFormats.Quantity)]
    public decimal CountedQuantity { get; set; }
}

public class InventoryCountQueryDto : DocumentQueryDto
{
}

public class InventoryCountValidator : DocumentValidator<InventoryCountDto>
{
    public InventoryCountValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.Lines)
            .Must(lines =>
            {
                var nonEmpty = lines.Where(l => l.ProductId != Guid.Empty).Select(l => l.ProductId).ToList();
                return nonEmpty.Distinct().Count() == nonEmpty.Count;
            })
            .WithMessage(localizer["InventoryCount_DuplicateProduct"].Value);
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.CountedQuantity).GreaterThanOrEqualTo(0);
        });
    }
}
