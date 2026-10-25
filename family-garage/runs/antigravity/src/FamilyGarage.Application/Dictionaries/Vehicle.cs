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

[KandraDictionaryBehavior(typeof(VehicleDto))]
public class VehicleBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<Vehicle>
{
    public async ValueTask OnNewAsync(Vehicle entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Vehicles, raw => $"VEH-{raw:D4}",
            requestorTag: "Vehicle:OnNew", cancellationToken: cancellationToken);
}

[Mapper]
public partial class VehicleMapper : IEntityMapper<Vehicle, VehicleDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial VehicleDto ToDto(Vehicle entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Vehicle ToEntity(VehicleDto dto);
}

public class VehicleQueryDtoValidator : PagedQueryDtoValidator<VehicleQueryDto>;
