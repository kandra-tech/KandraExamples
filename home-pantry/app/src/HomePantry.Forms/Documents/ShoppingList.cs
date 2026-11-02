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

[TypeId(HomePantryTypeIds.ShoppingList)]
[KandraDocumentForm(Name = "ShoppingLists", ValidatorType = typeof(ShoppingListValidator), QueryDtoType = typeof(ShoppingListQueryDto))]
[EditFormTitle("EditShoppingList")]
[AddFormTitle("CreateShoppingList")]
[ViewFormTitle("ViewShoppingList")]
[ListFormTitle("ShoppingLists")]
[NavigationName("Nav_ShoppingLists")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(ShoppingListPrintForm))]
[SourceFor(typeof(PurchaseDto))]
[Description("A shopping list of items to buy, optionally linked to a store, used to prepare a purchase.")]
public class ShoppingListDto : DocumentDto
{
    [Caption("Store")]
    [Search(typeof(StoreDto))]
    [ListFormColumn]
    [Description("The store to shop at (optional).")]
    public Guid? StoreId { get; set; }

    [TablePart(AddCaption = "AddLine")]
    [Caption("ShoppingList_Lines")]
    [Description("Items to buy with their quantity and bought status.")]
    public ICollection<ShoppingListLineDto> Lines { get; set; } = [];
}

[Description("A shopping list line specifying the product to buy, required quantity, and whether it has been bought.")]
public class ShoppingListLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Caption("Product")]
    [Search(typeof(ProductNodeDto))]
    [Required]
    [GridColumn(Order = 1, WidthPercent = 50)]
    public Guid ProductId { get; set; }

    [Caption("Quantity")]
    [GridColumn(Order = 2, WidthPercent = 25)]
    public decimal Quantity { get; set; }

    [Caption("Bought")]
    [GridColumn(Order = 3, WidthPercent = 25)]
    public bool Bought { get; set; }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("ShoppingListPrintTitle")]
public sealed class ShoppingListPrintForm : DocumentPrintForm
{
    [Caption("Store")]
    public string? StoreName { get; set; }

    [Caption("TotalLines")]
    public int TotalLines { get; set; }

    [PrintTable(LineNumbers = true)]
    public List<ShoppingListPrintLine> Lines { get; set; } = [];
}

public sealed class ShoppingListPrintLine
{
    [PrintColumn(WidthPercent = 50)]
    [Caption("Product")]
    public string ProductName { get; set; } = "";

    [PrintColumn(WidthPercent = 25)]
    [Caption("Quantity")]
    [Format(PrintFormats.Quantity)]
    public decimal Quantity { get; set; }

    [PrintColumn(WidthPercent = 25)]
    [Caption("Bought")]
    public string Bought { get; set; } = "";
}

public class ShoppingListQueryDto : DocumentQueryDto
{
}

public class ShoppingListValidator : DocumentValidator<ShoppingListDto>
{
    public ShoppingListValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
