using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_MealPlans")]
[KandraGeneratedEquality]
public sealed partial class MealPlan : DocumentBase, IEntityWithRows<MealPlan>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.MealPlan);

    public MealPlan() : base(TypeId) { }

    public DateTime WeekStartDate { get; set; }

    [KandraTablePart(TableName = "Doc_MealPlanLines")]
    public IList<MealPlanLine> Lines { get; set; } = [];
}

[KandraGeneratedEquality]
public partial class MealPlanLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid MealPlanId { get; set; }

    [JsonIgnore]
    public MealPlan MealPlan { get; set; } = null!;

    public MealPlanDay Day { get; set; } = MealPlanDay.Monday;

    public Guid RecipeId { get; set; }

    [JsonIgnore]
    public Recipe Recipe { get; set; } = null!;

    [Precision(14, 2)]
    public decimal Servings { get; set; } = 1m;
}
