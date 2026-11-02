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

[KandraDictionaryBehavior(typeof(MeterDto))]
public class MeterBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<Meter>
{
    public async ValueTask OnNewAsync(Meter entity, CancellationToken cancellationToken)
    {
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Meter, raw => $"MTR-{raw:D4}",
            requestorTag: "Meter:OnNew", cancellationToken: cancellationToken);
        entity.NextVerificationDate = DateTime.Today.AddYears(4);
    }
}

[Mapper]
public partial class MeterMapper : IEntityMapper<Meter, MeterDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial MeterDto ToDto(Meter entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Meter ToEntity(MeterDto dto);
}

public class MeterQueryDtoValidator : PagedQueryDtoValidator<MeterQueryDto>;
