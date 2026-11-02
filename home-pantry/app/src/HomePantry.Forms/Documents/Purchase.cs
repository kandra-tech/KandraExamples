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
using Kandra.Forms.ApiClients;
using Kandra.Forms.Documents;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Forms.UiBehaviors;
using Kandra.Validators.Documents;
using HomePantry.Enums;
using HomePantry.Forms.Dictionaries;
using HomePantry.Forms.Handlers;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Documents;

[TypeId(HomePantryTypeIds.Purchase)]
[KandraDocumentForm(Name = "Purchases", ValidatorType = typeof(PurchaseValidator), QueryDtoType = typeof(PurchaseQueryDto))]
[EditFormTitle("EditPurchase")]
[AddFormTitle("CreatePurchase")]
[ViewFormTitle("ViewPurchase")]
[ListFormTitle("Purchases")]
[NavigationName("Nav_Purchases")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(PurchasePrintForm))]
[UiBehavior(typeof(PurchaseUiBehavior))]
[Description("A purchase of food or household items from a store, establishing stock batches and recording last purchase prices.")]
public class PurchaseDto : DocumentDto
{
    [Caption("Store")]
    [Search(typeof(StoreDto))]
    [Required]
    [ListFormColumn]
    [Description("The store where the items were bought.")]
    public Guid StoreId { get; set; }

    [Caption("ShoppingList")]
    [Search(typeof(ShoppingListDto))]
    [Description("The source shopping list this purchase was created from (optional).")]
    public Guid? ShoppingListId { get; set; }

    [Caption("Total")]
    [NotEditable]
    [Calculated]
    [TableTotal(nameof(Lines), nameof(PurchaseLineDto.Amount))]
    [ListFormColumn]
    [Description("The total monetary amount of the purchase.")]
    public decimal Total { get; set; }

    [TablePart(AddCaption = "AddLine")]
    [Caption("Purchase_Lines")]
    [OnLineRemove(nameof(PurchaseUiBehavior.OnLineRemoved))]
    [Description("Items purchased with their quantity, price, location and best-before date.")]
    public ICollection<PurchaseLineDto> Lines { get; set; } = [];
}

[Description("A purchased item line with product, quantity, unit price, line amount, storage location, and best-before date.")]
public class PurchaseLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Caption("Product")]
    [Search(typeof(ProductNodeDto))]
    [Required]
    [GridColumn(Order = 1, WidthPercent = 25)]
    [OnChange(nameof(PurchaseUiBehavior.OnProductChanged))]
    public Guid ProductId { get; set; }

    [Caption("Quantity")]
    [GridColumn(Order = 2, WidthPercent = 15)]
    [OnChange(nameof(PurchaseUiBehavior.OnQuantityOrPriceChanged))]
    public decimal Quantity { get; set; }

    [Caption("Price")]
    [GridColumn(Order = 3, WidthPercent = 15)]
    [OnChange(nameof(PurchaseUiBehavior.OnQuantityOrPriceChanged))]
    public decimal Price { get; set; }

    [Caption("Amount")]
    [NotEditable]
    [Calculated]
    [GridColumn(Order = 4, WidthPercent = 15)]
    public decimal Amount { get; set; }

    [Caption("Location")]
    [Search(typeof(LocationNodeDto))]
    [Required]
    [GridColumn(Order = 5, WidthPercent = 15)]
    public Guid LocationId { get; set; }

    [Caption("PurchaseLine_BestBeforeDate")]
    [DateEditor]
    [GridColumn(Order = 6, WidthPercent = 15)]
    public DateTime BestBeforeDate { get; set; } = new(9999, 12, 31);
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("PurchasePrintTitle")]
public sealed class PurchasePrintForm : DocumentPrintForm
{
    [Caption("Store")]
    public string? StoreName { get; set; }

    [PrintRole(PrintRole.GrandTotal)]
    [Caption("Total")]
    [Format("N2")]
    public decimal Total { get; set; }

    [PrintTable(LineNumbers = true)]
    public List<PurchasePrintLine> Lines { get; set; } = [];
}

public sealed class PurchasePrintLine
{
    [PrintColumn(WidthPercent = 35)]
    [Caption("Product")]
    public string ProductName { get; set; } = "";

    [PrintColumn(WidthPercent = 15)]
    [Caption("Quantity")]
    [Format(PrintFormats.Quantity)]
    public decimal Quantity { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("Price")]
    [Format("N2")]
    public decimal Price { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("Amount")]
    [Format("N2")]
    public decimal Amount { get; set; }

    [PrintColumn(WidthPercent = 20)]
    [Caption("Location")]
    public string LocationName { get; set; } = "";
}

public class PurchaseQueryDto : DocumentQueryDto
{
}

public class PurchaseValidator : DocumentValidator<PurchaseDto>
{
    public PurchaseValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.Price).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.LocationId).NotEmpty();
        });
    }
}

public sealed class PurchaseUiBehavior(IHandlerApiClient<ProductDefaultLookupRequestDto, ProductDefaultLookupResponseDto> lookup)
    : DocumentUiBehavior<PurchaseDto>
{
    public async Task OnProductChanged(PurchaseLineDto line, CancellationToken cancellationToken)
    {
        if (line.ProductId == Guid.Empty) return;

        try
        {
            var res = await lookup.HandleAsync(new ProductDefaultLookupRequestDto
            {
                ProductId = line.ProductId,
                DocumentDate = Record.Date
            }, cancellationToken);

            if (res.DefaultLocationId.HasValue && res.DefaultLocationId.Value != Guid.Empty)
            {
                line.LocationId = res.DefaultLocationId.Value;
            }
            line.BestBeforeDate = res.BestBeforeDate;
        }
        catch
        {
            // Fail open
        }
    }

    public void OnQuantityOrPriceChanged(PurchaseLineDto line)
    {
        line.Amount = Math.Round(line.Quantity * line.Price, 2);
        Record.Total = Record.Lines.Sum(l => l.Amount);
    }

    public void OnLineRemoved(PurchaseLineDto line)
    {
        Record.Total = Record.Lines.Sum(l => l.Amount);
    }

    public override ValueTask OnBeforeSaveAsync(CancellationToken cancellationToken)
    {
        foreach (var line in Record.Lines)
        {
            line.Amount = Math.Round(line.Quantity * line.Price, 2);
        }
        Record.Total = Record.Lines.Sum(l => l.Amount);
        return ValueTask.CompletedTask;
    }
}
