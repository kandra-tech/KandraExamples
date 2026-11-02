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

[TypeId(HomePantryTypeIds.HouseholdMember)]
[KandraDictionaryForm(Name = "HouseholdMembers", ValidatorType = typeof(HouseholdMemberValidator), QueryDtoType = typeof(HouseholdMemberQueryDto))]
[EditFormTitle("EditHouseholdMember")]
[AddFormTitle("CreateHouseholdMember")]
[ViewFormTitle("ViewHouseholdMember")]
[ListFormTitle("HouseholdMembers")]
[NavigationName("Nav_HouseholdMembers")]
[Description("A household member or resident of the home.")]
public class HouseholdMemberDto : DictionaryDto
{
}

public class HouseholdMemberQueryDto : DictionaryQueryDto
{
}

public class HouseholdMemberValidator : DictionaryValidator<HouseholdMemberDto>
{
    public HouseholdMemberValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}
