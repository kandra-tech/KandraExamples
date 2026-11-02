using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Domain.Commons;
using Kandra.Domain.Exceptions;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Kandra.Printing;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Application.Services;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Domain.Entities.Documents;
using PersonalAccounting.Domain.Entities.Registers;
using PersonalAccounting.Domain.Numbering;
using PersonalAccounting.Enums;
using PersonalAccounting.Forms.Documents;
using Riok.Mapperly.Abstractions;

namespace PersonalAccounting.Application.Documents;

[KandraDocumentBehavior(typeof(ExpenseDto))]
[RegisterTransactions(typeof(AccountBalanceMovement))]
[RegisterTransactions(typeof(CategoryTurnoverMovement))]
public class ExpenseBehavior(
    ISimpleNumberingSystem numbering,
    IEntityRepository<Account> accountRepo,
    IEntityRepository<CategoryNode> categoryRepo,
    ICurrencyConversionService currencyConversion,
    IStringLocalizer localizer) : IDocumentBehavior<Expense>
{
    public async ValueTask OnNewAsync(Expense entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        var yy = entity.Date.ToString("yy");
        entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.Expense}:{yy}", raw => $"EXP.{yy}{raw:D4}",
            requestorTag: "Expense:OnNew", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(Expense entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var category = await categoryRepo.GetAsync(entity.CategoryId, false, cancellationToken);
        if (category is null || category.Kind != CategoryKind.Expense)
            throw new InvalidStateException(localizer["Error_ExpenseCategoryKind"]);

        var account = await accountRepo.GetAsync(entity.AccountId, false, cancellationToken);
        var currencyId = account?.CurrencyId ?? Guid.Empty;
        var amountInBase = await currencyConversion.ConvertToBaseCurrencyAsync(currencyId, entity.Amount, entity.Date, localizer, cancellationToken);

        var balanceMovements = new List<AccountBalanceMovement>
        {
            new()
            {
                IsExpense = true,
                Dimensions = new AccountBalancesDimensions { AccountId = entity.AccountId },
                Resources = new AccountBalancesResources { Amount = entity.Amount },
                Details = new AccountBalancesDetails { CategoryId = entity.CategoryId, Comment = entity.Comment }
            }
        };

        var balanceWriter = registers.GetWriter<IBalanceRegisterWriter<AccountBalanceMovement, AccountBalancesBalance>>();
        await balanceWriter.PostAsync(entity.Id, entity.Date, balanceMovements, cancellationToken);

        var turnoverMovements = new List<CategoryTurnoverMovement>
        {
            new()
            {
                IsExpense = false,
                Dimensions = new CategoryTurnoverDimensions { CategoryId = entity.CategoryId, AccountId = entity.AccountId },
                Resources = new CategoryTurnoverResources
                {
                    AmountInAccountCurrency = entity.Amount,
                    AmountInBaseCurrency = amountInBase
                },
                Details = new CategoryTurnoverDetails { Comment = entity.Comment }
            }
        };

        var turnoverWriter = registers.GetWriter<ITurnoverRegisterWriter<CategoryTurnoverMovement>>();
        await turnoverWriter.PostAsync(entity.Id, entity.Date, turnoverMovements, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(Expense entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<IBalanceRegisterWriter<AccountBalanceMovement, AccountBalancesBalance>>().UnpostAsync(entity.Id, cancellationToken);
        await registers.GetWriter<ITurnoverRegisterWriter<CategoryTurnoverMovement>>().UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<IBalanceRegisterWriter<AccountBalanceMovement, AccountBalancesBalance>>().DeleteAsync(id, cancellationToken);
        await registers.GetWriter<ITurnoverRegisterWriter<CategoryTurnoverMovement>>().DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(Expense entity, ExportFormat format, string? layoutTag,
        ILookupRepository lookup, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var accounts = (await lookup.GetDictionaryAsync<Account>([entity.AccountId], cancellationToken)).ToDictionary(a => a.Id);
        var categories = (await lookup.GetDictionaryAsync<CategoryNode>([entity.CategoryId], cancellationToken)).ToDictionary(c => c.Id);

        var form = new ExpensePrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            GeneratedAt = DateTime.UtcNow,
            AccountName = accounts.TryGetValue(entity.AccountId, out var acc) ? acc.Name : entity.AccountId.ToString(),
            CategoryName = categories.TryGetValue(entity.CategoryId, out var cat) ? cat.Name : entity.CategoryId.ToString(),
            Comment = entity.Comment,
            Total = entity.Amount,
            CurrencyCode = ""
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class ExpenseMapper : IEntityMapper<Expense, ExpenseDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial ExpenseDto ToDto(Expense entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreTarget(nameof(Expense.Account))]
    [MapperIgnoreTarget(nameof(Expense.Category))]
    public partial Expense ToEntity(ExpenseDto dto);
}

public class ExpenseQueryDtoValidator : PagedQueryDtoValidator<ExpenseQueryDto>;
