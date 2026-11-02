using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Forms.Handlers;

namespace HomePantry.Forms.Handlers;

public sealed class CookRecipeFillRequestDto
{
    public Guid RecipeId { get; set; }
    public decimal Servings { get; set; }
}

public sealed class CookRecipeFillLineDto
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class CookRecipeFillResponseDto
{
    public List<CookRecipeFillLineDto> Lines { get; set; } = [];
}

[KandraHandler(Name = "CookRecipeFill")]
public sealed class CookRecipeFillHandler : IHandler<CookRecipeFillRequestDto, CookRecipeFillResponseDto>;
