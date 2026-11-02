using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Registers;
using HomePantry.Domain.Entities.Dictionaries;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class PlannedMealsDimensions : IRegisterDimensions<PlannedMealsDimensions>
{
    [Caption("Date")]
    [Description("The day the meal is planned for.")]
    public DateTime Date { get; set; }

    [Caption("Recipe")]
    [Description("The recipe planned to be cooked.")]
    public Guid RecipeId { get; set; }

    [JsonIgnore]
    public Recipe Recipe { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class PlannedMealsDetails : IRegisterDetails<PlannedMealsDetails>
{
    [Precision(14, 2)]
    [Caption("Servings")]
    [Format("N1")]
    [Description("Planned servings for the meal.")]
    public decimal Servings { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_PlannedMeals")]
[RegisterCaption("PlannedMeals")]
public sealed class PlannedMealsRecord : DocumentInfoRecord<PlannedMealsDimensions, PlannedMealsDetails>
{
}

[Description("Document-bound information register storing planned servings per day and recipe, written by meal plan documents.")]
public sealed partial class PlannedMealsRegister : DocumentInfoRegister<PlannedMealsRecord>
{
}
