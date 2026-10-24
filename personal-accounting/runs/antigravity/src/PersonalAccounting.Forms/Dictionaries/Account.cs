using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Forms.Dictionaries;

[TypeId(PersonalAccountingTypeIds.Account)]
[KandraDictionaryForm(Name = "Accounts", ValidatorType = typeof(AccountValidator), QueryDtoType = typeof(AccountQueryDto))]
[EditFormTitle("EditAccount")]
[AddFormTitle("CreateAccount")]
[ViewFormTitle("ViewAccount")]
[ListFormTitle("Accounts")]
[NavigationName("Nav_Accounts")]
[Description("A financial account (cash, bank, card, or savings) held in a specific currency.")]
public class AccountDto : DictionaryDto
{
    [Caption("Account_Type")]
    [ListFormColumn]
    [Description("The type of account: cash, bank, card, or savings.")]
    public AccountType Type { get; set; }

    [Caption("Account_CurrencyId")]
    [Search(typeof(CurrencyDto))]
    [ListFormColumn]
    [Description("The currency in which this account is denominated.")]
    public Guid CurrencyId { get; set; }
}

public class AccountQueryDto : DictionaryQueryDto
{
}

public class AccountValidator : DictionaryValidator<AccountDto>
{
    public AccountValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.CurrencyId)
            .NotEmpty()
            .WithMessage(localizer["Validation_CurrencyRequired"]);
    }
}
