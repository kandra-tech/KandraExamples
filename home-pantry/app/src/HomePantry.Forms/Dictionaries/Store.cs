using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using HomePantry.Enums;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Dictionaries;

[TypeId(HomePantryTypeIds.Store)]
[KandraDictionaryForm(Name = "Stores", ValidatorType = typeof(StoreValidator), QueryDtoType = typeof(StoreQueryDto))]
[EditFormTitle("EditStore")]
[AddFormTitle("CreateStore")]
[ViewFormTitle("ViewStore")]
[ListFormTitle("Stores")]
[NavigationName("Nav_Stores")]
[Description("A grocery store or retail outlet where household items and food are purchased.")]
public class StoreDto : DictionaryDto
{
}

public class StoreQueryDto : DictionaryQueryDto
{
}

public class StoreValidator : DictionaryValidator<StoreDto>
{
    public StoreValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}
