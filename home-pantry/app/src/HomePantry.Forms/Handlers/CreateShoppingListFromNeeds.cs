using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Forms.Handlers;

namespace HomePantry.Forms.Handlers;

public sealed class CreateShoppingListFromNeedsRequestDto
{
    public Guid? RecipeId { get; set; }
    public decimal? Servings { get; set; }
    public Guid? StoreId { get; set; }
}

public sealed class CreateShoppingListFromNeedsResponseDto
{
    public Guid ShoppingListId { get; set; }
    public string Code { get; set; } = "";
}

[KandraHandler(Name = "CreateShoppingListFromNeeds")]
public sealed class CreateShoppingListFromNeedsHandler : IHandler<CreateShoppingListFromNeedsRequestDto, CreateShoppingListFromNeedsResponseDto>;
