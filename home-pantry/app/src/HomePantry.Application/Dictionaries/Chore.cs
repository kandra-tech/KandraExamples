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

[KandraDictionaryBehavior(typeof(ChoreDto))]
public sealed class ChoreBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<Chore>
{
    public async ValueTask OnNewAsync(Chore entity, CancellationToken cancellationToken)
    {
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Chore, raw => $"{raw:D4}", cancellationToken: cancellationToken);
    }
}

[Mapper]
public sealed partial class ChoreMapper : IEntityMapper<Chore, ChoreDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial ChoreDto ToDto(Chore entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Chore ToEntity(ChoreDto dto);
}

public class ChoreQueryDtoValidator : PagedQueryDtoValidator<ChoreQueryDto>;
