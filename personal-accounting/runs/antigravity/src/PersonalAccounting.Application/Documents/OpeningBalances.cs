using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Kandra.Printing;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Domain.Entities.Documents;
using PersonalAccounting.Domain.Entities.Registers;
using PersonalAccounting.Domain.Numbering;
using PersonalAccounting.Forms.Documents;
using Riok.Mapperly.Abstractions;

namespace PersonalAccounting.Application.Documents;

[KandraDocumentBehavior(typeof(OpeningBalancesDto))]
[RegisterTransactions(typeof(AccountBalanceMovement))]
public class OpeningBalancesBehavior(ISimpleNumberingSystem numbering) : IDocumentBehavior<OpeningBalances>
{
    public async ValueTask OnNewAsync(OpeningBalances entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        var yy = entity.Date.ToString("yy");
        entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.OpeningBalances}:{yy}", raw => $"OB.{yy}{raw:D4}",
            requestorTag: "OpeningBalances:OnNew", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(OpeningBalances entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var movements = entity.Lines.Select(line => new AccountBalanceMovement
        {
            LineId = line.Id,
            IsExpense = false,
            Dimensions = new AccountBalancesDimensions { AccountId = line.AccountId },
            Resources = new AccountBalancesResources { Amount = line.Amount },
            Details = new AccountBalancesDetails { Comment = entity.Description }
        }).ToList();

        var writer = registers.GetWriter<IBalanceRegisterWriter<AccountBalanceMovement, AccountBalancesBalance>>();
        await writer.PostAsync(entity.Id, entity.Date, movements, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(OpeningBalances entity, ISubmitScope registers, CancellationToken cancellationToken) =>
        await registers.GetWriter<IBalanceRegisterWriter<AccountBalanceMovement, AccountBalancesBalance>>().UnpostAsync(entity.Id, cancellationToken);

    public ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken) =>
        new(registers.GetWriter<IBalanceRegisterWriter<AccountBalanceMovement, AccountBalancesBalance>>().DeleteAsync(id, cancellationToken));

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(OpeningBalances entity, ExportFormat format, string? layoutTag,
        ILookupRepository lookup, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var accountIds = entity.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = (await lookup.GetDictionaryAsync<Account>(accountIds, cancellationToken)).ToDictionary(a => a.Id);

        var lines = entity.Lines.Select(l => new OpeningBalancesPrintLine
        {
            AccountName = accounts.TryGetValue(l.AccountId, out var acc) ? acc.Name : l.AccountId.ToString(),
            Amount = l.Amount
        }).ToList();

        var form = new OpeningBalancesPrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            GeneratedAt = DateTime.UtcNow,
            Lines = lines,
            Total = lines.Sum(l => l.Amount),
            CurrencyCode = ""
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class OpeningBalancesMapper : IEntityMapper<OpeningBalances, OpeningBalancesDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial OpeningBalancesDto ToDto(OpeningBalances entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial OpeningBalances ToEntity(OpeningBalancesDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial OpeningBalancesLineDto ToDto(OpeningBalancesLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreTarget(nameof(OpeningBalancesLine.OpeningBalances))]
    [MapperIgnoreTarget(nameof(OpeningBalancesLine.OpeningBalancesId))]
    [MapperIgnoreTarget(nameof(OpeningBalancesLine.Account))]
    private partial OpeningBalancesLine ToEntity(OpeningBalancesLineDto line);
}

public class OpeningBalancesQueryDtoValidator : PagedQueryDtoValidator<OpeningBalancesQueryDto>;
