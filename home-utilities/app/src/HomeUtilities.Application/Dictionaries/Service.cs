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

[KandraDictionaryBehavior(typeof(ServiceNodeDto))]
public class ServiceBehavior(ISimpleNumberingSystem numbering) : IHierarchicalDictionaryBehavior<ServiceNode>
{
    public async ValueTask OnFolderNewAsync(ServiceNode entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.ServiceFolder, raw => $"SRVF-{raw:D4}",
            requestorTag: "ServiceFolder:OnNew", cancellationToken: cancellationToken);

    public async ValueTask OnLeafNewAsync(ServiceNode entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Service, raw => $"SRV-{raw:D4}",
            requestorTag: "Service:OnNew", cancellationToken: cancellationToken);
}

[Mapper]
public partial class ServiceMapper : IEntityMapper<ServiceNode, ServiceNodeDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    [MapDerivedType<ServiceFolder, ServiceFolderDto>]
    [MapDerivedType<Service, ServiceDto>]
    public partial ServiceNodeDto ToDto(ServiceNode entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapDerivedType<ServiceFolderDto, ServiceFolder>]
    [MapDerivedType<ServiceDto, Service>]
    public partial ServiceNode ToEntity(ServiceNodeDto dto);
}

public class ServiceQueryDtoValidator : PagedQueryDtoValidator<ServiceQueryDto>;
