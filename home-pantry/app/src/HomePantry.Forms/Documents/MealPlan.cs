using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Forms.Documents;
using Kandra.Forms.Querying;
using Kandra.Validators.Documents;
using HomePantry.Enums;
using HomePantry.Forms.Dictionaries;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Documents;

[TypeId(HomePantryTypeIds.MealPlan)]
[KandraDocumentForm(Name = "MealPlans", ValidatorType = typeof(MealPlanValidator), QueryDtoType = typeof(MealPlanQueryDto))]
[EditFormTitle("EditMealPlan")]
[AddFormTitle("CreateMealPlan")]
[ViewFormTitle("ViewMealPlan")]
[ListFormTitle("MealPlans")]
[NavigationName("Nav_MealPlans")]
[Description("Weekly meal plan defining planned recipes and servings for each day of the week.")]
public class MealPlanDto : DocumentDto
{
    [Caption("MealPlan_WeekStartDate")]
    [ListFormColumn(Filterable = false)]
    [DateEditor]
    [Description("Monday date of the meal plan week.")]
    public DateTime WeekStartDate { get; set; } = DateTime.UtcNow.Date;

    [TablePart(AddCaption = "AddLine")]
    [Caption("MealPlan_Lines")]
    [Description("Planned meals by day and recipe.")]
    public ICollection<MealPlanLineDto> Lines { get; set; } = [];
}

[Description("A planned meal line specifying the day of the week, recipe, and servings.")]
public class MealPlanLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Caption("MealPlan_Day")]
    [Required]
    [GridColumn(Order = 1, WidthPercent = 25)]
    [Description("Day of the week.")]
    public MealPlanDay Day { get; set; } = MealPlanDay.Monday;

    [Caption("Recipe")]
    [Search(typeof(RecipeDto))]
    [Required]
    [GridColumn(Order = 2, WidthPercent = 50)]
    [Description("The recipe planned for this day.")]
    public Guid RecipeId { get; set; }

    [Caption("Servings")]
    [Format("N1")]
    [Required]
    [GridColumn(Order = 3, WidthPercent = 25)]
    [Description("Planned number of servings.")]
    public decimal Servings { get; set; } = 1m;
}

public class MealPlanQueryDto : DocumentQueryDto
{
}

public class MealPlanValidator : DocumentValidator<MealPlanDto>
{
    public MealPlanValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage(localizer["MealPlan_LinesRequired"].Value);

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.RecipeId)
                .NotEmpty()
                .WithMessage(localizer["MealPlan_RecipeRequired"].Value);

            line.RuleFor(l => l.Servings)
                .GreaterThan(0)
                .WithMessage(localizer["MealPlan_ServingsPositive"].Value);
        });
    }
}
