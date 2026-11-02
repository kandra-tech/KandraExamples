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

[KandraDictionaryBehavior(typeof(ProviderDto))]
public class ProviderBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<Provider>
{
    public async ValueTask OnNewAsync(Provider entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Providers, raw => $"PROV-{raw:D4}",
            requestorTag: "Provider:OnNew", cancellationToken: cancellationToken);
}

[Mapper]
public partial class ProviderMapper : IEntityMapper<Provider, ProviderDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial ProviderDto ToDto(Provider entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Provider ToEntity(ProviderDto dto);
}

public class ProviderQueryDtoValidator : PagedQueryDtoValidator<ProviderQueryDto>;
