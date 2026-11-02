using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using HomePantry.Enums;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Dictionaries;

[TypeId(HomePantryTypeIds.Recipe)]
[KandraDictionaryForm(Name = "Recipes", ValidatorType = typeof(RecipeValidator), QueryDtoType = typeof(RecipeQueryDto))]
[EditFormTitle("EditRecipe")]
[AddFormTitle("CreateRecipe")]
[ViewFormTitle("ViewRecipe")]
[ListFormTitle("Recipes")]
[NavigationName("Nav_Recipes")]
[Description("A culinary recipe with stated servings, preparation instructions, and ingredient lines.")]
public class RecipeDto : DictionaryDto
{
    [Caption("Recipe_Servings")]
    [ListFormColumn]
    [Description("Number of servings produced by this recipe.")]
    public int Servings { get; set; } = 1;

    [Caption("Recipe_Instructions")]
    [Multiline(Rows = 5)]
    [Description("Cooking instructions and preparation notes.")]
    public string? Instructions { get; set; }

    [TablePart(AddCaption = "AddLine")]
    [Caption("Recipe_Lines")]
    [Description("Ingredients and required quantities for the stated servings.")]
    public ICollection<RecipeLineDto> Lines { get; set; } = [];
}

[Description("An ingredient line of a recipe specifying the product and required quantity for the recipe's stated servings.")]
public class RecipeLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Caption("Product")]
    [Search(typeof(ProductNodeDto))]
    [GridColumn(Order = 1, WidthPercent = 60)]
    public Guid ProductId { get; set; }

    [Caption("Quantity")]
    [GridColumn(Order = 2, WidthPercent = 40)]
    public decimal Quantity { get; set; }
}

public class RecipeQueryDto : DictionaryQueryDto
{
}

public class RecipeValidator : DictionaryValidator<RecipeDto>
{
    public RecipeValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.Servings).GreaterThan(0);
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
