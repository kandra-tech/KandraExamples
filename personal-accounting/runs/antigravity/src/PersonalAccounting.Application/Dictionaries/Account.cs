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

[KandraDictionaryBehavior(typeof(AccountDto))]
public class AccountBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<Account>
{
    public async ValueTask OnNewAsync(Account entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Account, raw => $"ACC-{raw:D3}",
            requestorTag: "Account:OnNew", cancellationToken: cancellationToken);
}

[Mapper]
public partial class AccountMapper : IEntityMapper<Account, AccountDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial AccountDto ToDto(Account entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreTarget(nameof(Account.Currency))]
    public partial Account ToEntity(AccountDto dto);
}

public class AccountQueryDtoValidator : PagedQueryDtoValidator<AccountQueryDto>;
