using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Domain.Numbering;
using PersonalAccounting.Forms.Dictionaries;
using Riok.Mapperly.Abstractions;

namespace PersonalAccounting.Application.Dictionaries;

[KandraDictionaryBehavior(typeof(CurrencyDto))]
public class CurrencyBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<Currency>
{
    public async ValueTask OnNewAsync(Currency entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Currency, raw => $"CUR-{raw:D3}",
            requestorTag: "Currency:OnNew", cancellationToken: cancellationToken);
}

[Mapper]
public partial class CurrencyMapper : IEntityMapper<Currency, CurrencyDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial CurrencyDto ToDto(Currency entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Currency ToEntity(CurrencyDto dto);
}

public class CurrencyQueryDtoValidator : PagedQueryDtoValidator<CurrencyQueryDto>;
