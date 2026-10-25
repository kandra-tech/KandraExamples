using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Attributes.Application;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Domain.Numbering;
using FamilyGarage.Forms.Dictionaries;
using Riok.Mapperly.Abstractions;
using Kandra.Application.Validation;

namespace FamilyGarage.Application.Dictionaries;

[KandraDictionaryBehavior(typeof(ItemNodeDto))]
public class ItemBehavior(ISimpleNumberingSystem numbering) : IHierarchicalDictionaryBehavior<ItemNode>
{
    public async ValueTask OnFolderNewAsync(ItemNode entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.ItemsFolder, raw => $"GRP-{raw:D4}",
            requestorTag: "ItemFolder:OnNew", cancellationToken: cancellationToken);

    public async ValueTask OnLeafNewAsync(ItemNode entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Items, raw => $"ITEM-{raw:D4}",
            requestorTag: "Item:OnNew", cancellationToken: cancellationToken);
}

[Mapper]
public partial class ItemMapper : IEntityMapper<ItemNode, ItemNodeDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    [MapDerivedType<ItemFolder, ItemFolderDto>]
    [MapDerivedType<Item, ItemDto>]
    public partial ItemNodeDto ToDto(ItemNode entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapDerivedType<ItemFolderDto, ItemFolder>]
    [MapDerivedType<ItemDto, Item>]
    public partial ItemNode ToEntity(ItemNodeDto dto);
}

public class ItemQueryDtoValidator : PagedQueryDtoValidator<ItemQueryDto>;
