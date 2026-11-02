using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using HomePantry.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Recipes")]
[KandraGeneratedEquality]
public sealed partial class Recipe : DictionaryBase, IEntityWithTypeId, IEntityWithRows<Recipe>
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.Recipe);

    public int Servings { get; set; }

    public string? Instructions { get; set; }

    [KandraTablePart(TableName = "Dict_RecipeLines")]
    public IList<RecipeLine> Lines { get; set; } = [];
}

[KandraGeneratedEquality]
public sealed partial class RecipeLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid RecipeId { get; set; }

    [JsonIgnore]
    public Recipe Recipe { get; set; } = null!;

    public Guid ProductId { get; set; }

    [JsonIgnore]
    public ProductNode Product { get; set; } = null!;

    [Precision(14, 3)]
    public decimal Quantity { get; set; }
}
