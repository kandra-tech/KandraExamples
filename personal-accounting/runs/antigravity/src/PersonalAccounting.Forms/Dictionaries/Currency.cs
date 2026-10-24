using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Forms.Dictionaries;

[TypeId(PersonalAccountingTypeIds.Currency)]
[KandraDictionaryForm(Name = "Currencies", ValidatorType = typeof(CurrencyValidator), QueryDtoType = typeof(CurrencyQueryDto))]
[EditFormTitle("EditCurrency")]
[AddFormTitle("CreateCurrency")]
[ViewFormTitle("ViewCurrency")]
[ListFormTitle("Currencies")]
[NavigationName("Nav_Currencies")]
[Description("A currency tracked by personal accounting (e.g. UAH, USD, EUR, PLN).")]
public class CurrencyDto : DictionaryDto
{
}

public class CurrencyQueryDto : DictionaryQueryDto
{
}

public class CurrencyValidator : DictionaryValidator<CurrencyDto>
{
    public CurrencyValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}
