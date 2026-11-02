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

[KandraDocumentBehavior(typeof(TransferDto))]
[RegisterTransactions(typeof(AccountBalanceMovement))]
public class TransferBehavior(ISimpleNumberingSystem numbering) : IDocumentBehavior<Transfer>
{
    public async ValueTask OnNewAsync(Transfer entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        var yy = entity.Date.ToString("yy");
        entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.Transfer}:{yy}", raw => $"TRF.{yy}{raw:D4}",
            requestorTag: "Transfer:OnNew", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(Transfer entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var movements = new List<AccountBalanceMovement>
        {
            // Source account leg: expense / out
            new()
            {
                LineId = Guid.NewGuid(),
                IsExpense = true,
                Dimensions = new AccountBalancesDimensions { AccountId = entity.FromAccountId },
                Resources = new AccountBalancesResources { Amount = entity.AmountOut },
                Details = new AccountBalancesDetails { CounterAccountId = entity.ToAccountId, Comment = entity.Comment }
            },
            // Target account leg: receipt / in
            new()
            {
                LineId = Guid.NewGuid(),
                IsExpense = false,
                Dimensions = new AccountBalancesDimensions { AccountId = entity.ToAccountId },
                Resources = new AccountBalancesResources { Amount = entity.AmountIn },
                Details = new AccountBalancesDetails { CounterAccountId = entity.FromAccountId, Comment = entity.Comment }
            }
        };

        var writer = registers.GetWriter<IBalanceRegisterWriter<AccountBalanceMovement, AccountBalancesBalance>>();
        await writer.PostAsync(entity.Id, entity.Date, movements, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(Transfer entity, ISubmitScope registers, CancellationToken cancellationToken) =>
        await registers.GetWriter<IBalanceRegisterWriter<AccountBalanceMovement, AccountBalancesBalance>>().UnpostAsync(entity.Id, cancellationToken);

    public ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken) =>
        new(registers.GetWriter<IBalanceRegisterWriter<AccountBalanceMovement, AccountBalancesBalance>>().DeleteAsync(id, cancellationToken));

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(Transfer entity, ExportFormat format, string? layoutTag,
        ILookupRepository lookup, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var accounts = (await lookup.GetDictionaryAsync<Account>([entity.FromAccountId, entity.ToAccountId], cancellationToken)).ToDictionary(a => a.Id);

        var form = new TransferPrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            GeneratedAt = DateTime.UtcNow,
            FromAccountName = accounts.TryGetValue(entity.FromAccountId, out var fromAcc) ? fromAcc.Name : entity.FromAccountId.ToString(),
            ToAccountName = accounts.TryGetValue(entity.ToAccountId, out var toAcc) ? toAcc.Name : entity.ToAccountId.ToString(),
            AmountOut = entity.AmountOut,
            AmountIn = entity.AmountIn,
            Comment = entity.Comment
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class TransferMapper : IEntityMapper<Transfer, TransferDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial TransferDto ToDto(Transfer entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreTarget(nameof(Transfer.FromAccount))]
    [MapperIgnoreTarget(nameof(Transfer.ToAccount))]
    public partial Transfer ToEntity(TransferDto dto);
}

public class TransferQueryDtoValidator : PagedQueryDtoValidator<TransferQueryDto>;
