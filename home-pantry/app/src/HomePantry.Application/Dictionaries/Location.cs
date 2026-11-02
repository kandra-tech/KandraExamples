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

[KandraDictionaryBehavior(typeof(LocationNodeDto))]
public class LocationBehavior(ISimpleNumberingSystem numbering) : IHierarchicalDictionaryBehavior<LocationNode>
{
    public async ValueTask OnFolderNewAsync(LocationNode entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.LocationFolder, raw => $"LF-{raw:D4}", cancellationToken: cancellationToken);

    public async ValueTask OnLeafNewAsync(LocationNode entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Location, raw => $"LOC-{raw:D4}", cancellationToken: cancellationToken);
}

[Mapper]
public partial class LocationMapper : IEntityMapper<LocationNode, LocationNodeDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    [MapDerivedType<LocationFolder, LocationFolderDto>]
    [MapDerivedType<Location, LocationDto>]
    public partial LocationNodeDto ToDto(LocationNode entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapDerivedType<LocationFolderDto, LocationFolder>]
    [MapDerivedType<LocationDto, Location>]
    public partial LocationNode ToEntity(LocationNodeDto dto);
}

public class LocationQueryDtoValidator : PagedQueryDtoValidator<LocationQueryDto>;
