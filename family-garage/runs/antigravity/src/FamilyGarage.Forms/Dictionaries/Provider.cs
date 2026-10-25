using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Forms.Dictionaries;

[TypeId(FamilyGarageTypeIds.Provider)]
[KandraDictionaryForm(Name = "Providers", ValidatorType = typeof(ProviderValidator), QueryDtoType = typeof(ProviderQueryDto))]
[EditFormTitle("EditProvider")]
[AddFormTitle("CreateProvider")]
[ViewFormTitle("ViewProvider")]
[ListFormTitle("Providers")]
[NavigationName("Nav_Providers")]
[Description("A provider of automotive goods or services (service station, fuel station, insurer, bank, leasing company, etc.).")]
public class ProviderDto : DictionaryDto
{
    [Caption("Kind")]
    [ListFormColumn(Filterable = false)]
    [Description("Kind of provider: service station, insurer, fuel station, dealer, bank, leasing company, other.")]
    public ProviderKind Kind { get; set; }

    [Caption("Phone")]
    [ListFormColumn]
    [Description("Contact phone number.")]
    public string? Phone { get; set; }

    [Caption("Comment")]
    [Description("Optional comment or notes.")]
    public string? Comment { get; set; }
}

public class ProviderQueryDto : DictionaryQueryDto
{
}

public class ProviderValidator : DictionaryValidator<ProviderDto>
{
    public ProviderValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}
