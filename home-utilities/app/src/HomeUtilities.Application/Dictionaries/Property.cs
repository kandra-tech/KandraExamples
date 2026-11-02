using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Domain.Numbering;
using HomeUtilities.Forms.Dictionaries;
using Riok.Mapperly.Abstractions;

namespace HomeUtilities.Application.Dictionaries;

[KandraDictionaryBehavior(typeof(PropertyDto))]
public class PropertyBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<Property>
{
    public async ValueTask OnNewAsync(Property entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Property, raw => $"PROP-{raw:D4}",
            requestorTag: "Property:OnNew", cancellationToken: cancellationToken);
}

[Mapper]
public partial class PropertyMapper : IEntityMapper<Property, PropertyDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial PropertyDto ToDto(Property entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Property ToEntity(PropertyDto dto);
}

public class PropertyQueryDtoValidator : PagedQueryDtoValidator<PropertyQueryDto>;
