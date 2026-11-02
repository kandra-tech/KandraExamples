using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Attributes.Application;
using Kandra.Persistence.Abstractions;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Forms.Handlers;

namespace HomePantry.Application.Handlers;

[KandraHandlerBehavior(typeof(CookRecipeFillHandler))]
public sealed class CookRecipeFillBehavior(IEntityRepository<Recipe> recipeRepo)
    : IHandlerBehavior<CookRecipeFillRequestDto, CookRecipeFillResponseDto>
{
    public async Task<CookRecipeFillResponseDto> HandleAsync(CookRecipeFillRequestDto request, CancellationToken cancellationToken)
    {
        if (request.RecipeId == Guid.Empty || request.Servings <= 0)
        {
            return new CookRecipeFillResponseDto();
        }

        var recipe = await recipeRepo.GetAsync(request.RecipeId, true, cancellationToken);
        if (recipe != null)
        {
            var baseServings = recipe.Servings > 0 ? recipe.Servings : 1m;
            var scale = request.Servings / baseServings;

            return new CookRecipeFillResponseDto
            {
                Lines = recipe.Lines.Select(l => new CookRecipeFillLineDto
                {
                    ProductId = l.ProductId,
                    Quantity = Math.Round(l.Quantity * scale, 3)
                }).ToList()
            };
        }

        return new CookRecipeFillResponseDto();
    }
}
