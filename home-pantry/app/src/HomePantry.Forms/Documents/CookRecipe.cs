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

[TypeId(HomePantryTypeIds.CookRecipe)]
[KandraDocumentForm(Name = "CookRecipes", ValidatorType = typeof(CookRecipeValidator), QueryDtoType = typeof(CookRecipeQueryDto))]
[EditFormTitle("EditCookRecipe")]
[AddFormTitle("CreateCookRecipe")]
[ViewFormTitle("ViewCookRecipe")]
[ListFormTitle("CookRecipes")]
[NavigationName("Nav_CookRecipes")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(CookRecipePrintForm))]
[UiBehavior(typeof(CookRecipeUiBehavior))]
[Description("Cooking a recipe, consuming its scaled ingredients from stock batches with earliest best-before dates.")]
public class CookRecipeDto : DocumentDto
{
    [Caption("Recipe")]
    [Search(typeof(RecipeDto))]
    [Required]
    [ListFormColumn]
    [OnChange(nameof(CookRecipeUiBehavior.OnRecipeOrServingsChanged))]
    [Description("The recipe being cooked.")]
    public Guid RecipeId { get; set; }

    [Caption("Servings")]
    [Required]
    [ListFormColumn]
    [OnChange(nameof(CookRecipeUiBehavior.OnRecipeOrServingsChanged))]
    [Description("Number of servings to prepare, scaling the ingredient quantities.")]
    public decimal Servings { get; set; } = 1m;

    [Caption("Location")]
    [Search(typeof(LocationNodeDto))]
    [Required]
    [ListFormColumn]
    [Description("The kitchen or pantry location from which the ingredients are taken.")]
    public Guid LocationId { get; set; }

    [TablePart(AddCaption = "AddLine")]
    [Caption("CookRecipe_Lines")]
    [Description("Scaled recipe ingredient lines to consume.")]
    public ICollection<CookRecipeLineDto> Lines { get; set; } = [];
}

[Description("A scaled ingredient line specifying the product and quantity to consume when cooking the recipe.")]
public class CookRecipeLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Caption("Product")]
    [Search(typeof(ProductNodeDto))]
    [Required]
    [GridColumn(Order = 1, WidthPercent = 60)]
    public Guid ProductId { get; set; }

    [Caption("Quantity")]
    [GridColumn(Order = 2, WidthPercent = 40)]
    public decimal Quantity { get; set; }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("CookRecipePrintTitle")]
public sealed class CookRecipePrintForm : DocumentPrintForm
{
    [Caption("Recipe")]
    public string RecipeName { get; set; } = "";

    [Caption("Servings")]
    [Format("N1")]
    public decimal Servings { get; set; }

    [Caption("Location")]
    public string LocationName { get; set; } = "";

    [PrintTable(LineNumbers = true)]
    public List<CookRecipePrintLine> Lines { get; set; } = [];
}

public sealed class CookRecipePrintLine
{
    [PrintColumn(WidthPercent = 60)]
    [Caption("Product")]
    public string ProductName { get; set; } = "";

    [PrintColumn(WidthPercent = 40)]
    [Caption("Quantity")]
    [Format(PrintFormats.Quantity)]
    public decimal Quantity { get; set; }
}

public class CookRecipeQueryDto : DocumentQueryDto
{
}

public class CookRecipeValidator : DocumentValidator<CookRecipeDto>
{
    public CookRecipeValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.Servings).GreaterThan(0);
        RuleFor(x => x.LocationId).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}

public sealed class CookRecipeUiBehavior(IHandlerApiClient<CookRecipeFillRequestDto, CookRecipeFillResponseDto> fillLookup)
    : DocumentUiBehavior<CookRecipeDto>
{
    public async Task OnRecipeOrServingsChanged(CancellationToken cancellationToken)
    {
        if (Record.RecipeId == Guid.Empty || Record.Servings <= 0) return;

        try
        {
            var res = await fillLookup.HandleAsync(new CookRecipeFillRequestDto
            {
                RecipeId = Record.RecipeId,
                Servings = Record.Servings
            }, cancellationToken);

            Record.Lines.Clear();
            foreach (var line in res.Lines)
            {
                Record.Lines.Add(new CookRecipeLineDto
                {
                    Id = Guid.NewGuid(),
                    ProductId = line.ProductId,
                    Quantity = line.Quantity
                });
            }
        }
        catch
        {
            // Fail open
        }
    }
}
