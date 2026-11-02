using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_CookRecipes")]
[KandraGeneratedEquality]
public sealed partial class CookRecipe : DocumentBase, IEntityWithRows<CookRecipe>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.CookRecipe);

    public CookRecipe() : base(TypeId) { }

    public Guid RecipeId { get; set; }

    [JsonIgnore]
    public Recipe Recipe { get; set; } = null!;

    [Precision(14, 2)]
    public decimal Servings { get; set; } = 1m;

    public Guid LocationId { get; set; }

    [JsonIgnore]
    public LocationNode Location { get; set; } = null!;

    [KandraTablePart(TableName = "Doc_CookRecipeLines")]
    public IList<CookRecipeLine> Lines { get; set; } = [];
}

[KandraGeneratedEquality]
public partial class CookRecipeLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid CookRecipeId { get; set; }

    [JsonIgnore]
    public CookRecipe CookRecipe { get; set; } = null!;

    public Guid ProductId { get; set; }

    [JsonIgnore]
    public ProductNode Product { get; set; } = null!;

    [Precision(14, 3)]
    public decimal Quantity { get; set; }
}
