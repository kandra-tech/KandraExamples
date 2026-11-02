using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using HomeUtilities.Enums;
using Microsoft.Extensions.Localization;

namespace HomeUtilities.Forms.Dictionaries;

[TypeId(HomeUtilitiesTypeIds.Property)]
[KandraDictionaryForm(Name = "Properties", ValidatorType = typeof(PropertyValidator), QueryDtoType = typeof(PropertyQueryDto))]
[EditFormTitle("EditProperty")]
[AddFormTitle("CreateProperty")]
[ViewFormTitle("ViewProperty")]
[ListFormTitle("Properties")]
[NavigationName("Nav_Properties")]
[Description("A household property (flat, country house) with its address and floor area.")]
public class PropertyDto : DictionaryDto
{
    [Caption("Property_Address")]
    [ListFormColumn]
    [Description("Street address or location of the property.")]
    public string? Address { get; set; }

    [Caption("Property_Area")]
    [ListFormColumn]
    [Format("N2")]
    [Description("Total area of the property in square metres.")]
    public decimal Area { get; set; }

    [Caption("Property_RegisteredPersonsCount")]
    [ListFormColumn]
    [Format("N0")]
    [Description("Number of persons registered at this property.")]
    public int RegisteredPersonsCount { get; set; } = 1;

    [Caption("Property_Owner")]
    [ListFormColumn]
    [Description("Person to whom invoices for this property are addressed.")]
    public string? Owner { get; set; }
}

public class PropertyQueryDto : DictionaryQueryDto
{
}

public class PropertyValidator : DictionaryValidator<PropertyDto>
{
    public PropertyValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.Area).GreaterThan(0);
        RuleFor(x => x.RegisteredPersonsCount).GreaterThanOrEqualTo(0);
    }
}
