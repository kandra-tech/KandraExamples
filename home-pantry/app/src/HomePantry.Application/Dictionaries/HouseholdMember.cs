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

[KandraDictionaryBehavior(typeof(HouseholdMemberDto))]
public sealed class HouseholdMemberBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<HouseholdMember>
{
    public async ValueTask OnNewAsync(HouseholdMember entity, CancellationToken cancellationToken)
    {
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.HouseholdMember, raw => $"{raw:D4}", cancellationToken: cancellationToken);
    }
}

[Mapper]
public sealed partial class HouseholdMemberMapper : IEntityMapper<HouseholdMember, HouseholdMemberDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial HouseholdMemberDto ToDto(HouseholdMember entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial HouseholdMember ToEntity(HouseholdMemberDto dto);
}

public class HouseholdMemberQueryDtoValidator : PagedQueryDtoValidator<HouseholdMemberQueryDto>;
