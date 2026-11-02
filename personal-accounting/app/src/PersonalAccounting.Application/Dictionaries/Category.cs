using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Domain.Numbering;
using PersonalAccounting.Forms.Dictionaries;
using Riok.Mapperly.Abstractions;

namespace PersonalAccounting.Application.Dictionaries;

[KandraDictionaryBehavior(typeof(CategoryNodeDto))]
public class CategoryBehavior(ISimpleNumberingSystem numbering) : IHierarchicalDictionaryBehavior<CategoryNode>
{
    public async ValueTask OnFolderNewAsync(CategoryNode entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.CategoryFolder, raw => $"CAT-G-{raw:D3}",
            requestorTag: "CategoryFolder:OnNew", cancellationToken: cancellationToken);

    public async ValueTask OnLeafNewAsync(CategoryNode entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Category, raw => $"CAT-{raw:D3}",
            requestorTag: "Category:OnNew", cancellationToken: cancellationToken);
}

[Mapper]
public partial class CategoryMapper : IEntityMapper<CategoryNode, CategoryNodeDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    [MapDerivedType<CategoryFolder, CategoryFolderDto>]
    [MapDerivedType<Category, CategoryDto>]
    public partial CategoryNodeDto ToDto(CategoryNode entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapDerivedType<CategoryFolderDto, CategoryFolder>]
    [MapDerivedType<CategoryDto, Category>]
    public partial CategoryNode ToEntity(CategoryNodeDto dto);
}

public class CategoryQueryDtoValidator : PagedQueryDtoValidator<CategoryQueryDto>;
