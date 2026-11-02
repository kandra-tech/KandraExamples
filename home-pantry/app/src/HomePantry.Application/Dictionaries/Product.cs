using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Numbering;
using HomePantry.Forms.Dictionaries;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Dictionaries;

[KandraDictionaryBehavior(typeof(ProductNodeDto))]
public class ProductBehavior(ISimpleNumberingSystem numbering) : IHierarchicalDictionaryBehavior<ProductNode>
{
    public async ValueTask OnFolderNewAsync(ProductNode entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.ProductFolder, raw => $"PF-{raw:D4}", cancellationToken: cancellationToken);

    public async ValueTask OnLeafNewAsync(ProductNode entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Product, raw => $"PRD-{raw:D4}", cancellationToken: cancellationToken);
}

[Mapper]
public partial class ProductMapper : IEntityMapper<ProductNode, ProductNodeDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    [MapDerivedType<ProductFolder, ProductFolderDto>]
    [MapDerivedType<Product, ProductDto>]
    public partial ProductNodeDto ToDto(ProductNode entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapDerivedType<ProductFolderDto, ProductFolder>]
    [MapDerivedType<ProductDto, Product>]
    public partial ProductNode ToEntity(ProductNodeDto dto);
}

public class ProductQueryDtoValidator : PagedQueryDtoValidator<ProductQueryDto>;
