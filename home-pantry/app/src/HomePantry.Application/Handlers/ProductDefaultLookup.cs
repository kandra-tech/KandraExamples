using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Attributes.Application;
using Kandra.Persistence.Abstractions;
using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Forms.Handlers;

namespace HomePantry.Application.Handlers;

[KandraHandlerBehavior(typeof(ProductDefaultLookupHandler))]
public sealed class ProductDefaultLookupBehavior(IEntityRepository<ProductNode> productRepo, ICurrentTimeZone timeZone)
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
            var bestBefore = LocalCalendar.DefaultBestBefore(request.DocumentDate, leaf.DefaultShelfLifeDays, timeZone.TimeZone);

            return new ProductDefaultLookupResponseDto
            {
                DefaultLocationId = leaf.DefaultLocationId,
                BestBeforeDate = bestBefore
            };
        }

        return new ProductDefaultLookupResponseDto();
    }
}
