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

[KandraDictionaryBehavior(typeof(StoreDto))]
public sealed class StoreBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<Store>
{
    public async ValueTask OnNewAsync(Store entity, CancellationToken cancellationToken)
    {
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Store, raw => $"{raw:D4}", cancellationToken: cancellationToken);
    }
}

[Mapper]
public sealed partial class StoreMapper : IEntityMapper<Store, StoreDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial StoreDto ToDto(Store entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Store ToEntity(StoreDto dto);
}

public class StoreQueryDtoValidator : PagedQueryDtoValidator<StoreQueryDto>;
