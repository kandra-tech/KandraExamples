using Kandra.Application.Abstractions;
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

[KandraDictionaryBehavior(typeof(BatteryDto))]
public sealed class BatteryBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<Battery>
{
    public async ValueTask OnNewAsync(Battery entity, CancellationToken cancellationToken)
    {
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Battery, raw => $"{raw:D4}", cancellationToken: cancellationToken);
    }
}

[Mapper]
public sealed partial class BatteryMapper : IEntityMapper<Battery, BatteryDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial BatteryDto ToDto(Battery entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Battery ToEntity(BatteryDto dto);
}

public class BatteryQueryDtoValidator : PagedQueryDtoValidator<BatteryQueryDto>;
