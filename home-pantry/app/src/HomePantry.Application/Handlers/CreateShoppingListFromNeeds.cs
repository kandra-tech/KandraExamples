using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Services;
using Kandra.Attributes.Application;
using HomePantry.Domain.Commons;
using HomePantry.Domain.Numbering;
using HomePantry.Forms.Documents;
using HomePantry.Forms.Handlers;

namespace HomePantry.Application.Handlers;

[KandraHandlerBehavior(typeof(CreateShoppingListFromNeedsHandler))]
public sealed class CreateShoppingListFromNeedsBehavior(
    IReportsRepository reportsRepo,
    IDocumentCrudService<ShoppingListDto, ShoppingListQueryDto> shoppingListCrud,
    ISimpleNumberingSystem numbering)
    : IHandlerBehavior<CreateShoppingListFromNeedsRequestDto, CreateShoppingListFromNeedsResponseDto>
{
    public async Task<CreateShoppingListFromNeedsResponseDto> HandleAsync(CreateShoppingListFromNeedsRequestDto request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var needs = await reportsRepo.GetShoppingNeedsAsync(request.RecipeId, request.Servings, now, request.MealPlanWeek, request.MealPlanId, cancellationToken);
        var code = await numbering.GetNextAsync(NumberingBuckets.ShoppingList, raw => $"SHP-{raw:D4}", cancellationToken: cancellationToken);

        var dto = new ShoppingListDto
        {
            Date = now,
            Code = code,
            StoreId = request.StoreId,
            Lines = needs.Select(n => new ShoppingListLineDto
            {
                Id = Guid.NewGuid(),
                ProductId = n.ProductId,
                Quantity = n.TotalNeededQuantity,
                Bought = false
            }).ToList()
        };

        var created = await shoppingListCrud.InsertAsync(dto, cancellationToken);

        return new CreateShoppingListFromNeedsResponseDto
        {
            ShoppingListId = created.Id,
            Code = code
        };
    }
}
