using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Attributes.Application;
using Kandra.Persistence.Abstractions;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Forms.Handlers;

namespace HomePantry.Application.Handlers;

[KandraHandlerBehavior(typeof(ProductDefaultLookupHandler))]
public sealed class ProductDefaultLookupBehavior(IEntityRepository<ProductNode> productRepo)
    : IHandlerBehavior<ProductDefaultLookupRequestDto, ProductDefaultLookupResponseDto>
{
    public async Task<ProductDefaultLookupResponseDto> HandleAsync(ProductDefaultLookupRequestDto request, CancellationToken cancellationToken)
    {
        if (request.ProductId == Guid.Empty)
        {
            return new ProductDefaultLookupResponseDto();
        }

        var node = await productRepo.GetAsync(request.ProductId, false, cancellationToken);
        if (node is Product leaf)
        {
            var bestBefore = leaf.DefaultShelfLifeDays > 0
                ? request.DocumentDate.Date.AddDays(leaf.DefaultShelfLifeDays)
                : new DateTime(9999, 12, 31);

            return new ProductDefaultLookupResponseDto
            {
                DefaultLocationId = leaf.DefaultLocationId,
                BestBeforeDate = bestBefore
            };
        }

        return new ProductDefaultLookupResponseDto();
    }
}
