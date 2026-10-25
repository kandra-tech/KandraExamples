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

[KandraDictionaryBehavior(typeof(FinanceContractDto))]
public class FinanceContractBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<FinanceContract>
{
    public async ValueTask OnNewAsync(FinanceContract entity, CancellationToken cancellationToken)
    {
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.FinanceContracts, raw => $"FC-{raw:D4}",
            requestorTag: "FinanceContract:OnNew", cancellationToken: cancellationToken);
        entity.SignedOn = DateTime.UtcNow.Date;
        entity.PaymentDayOfMonth = 1;
    }
}

[Mapper]
public partial class FinanceContractMapper : IEntityMapper<FinanceContract, FinanceContractDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial FinanceContractDto ToDto(FinanceContract entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial FinanceContract ToEntity(FinanceContractDto dto);
}

public class FinanceContractQueryDtoValidator : PagedQueryDtoValidator<FinanceContractQueryDto>;
