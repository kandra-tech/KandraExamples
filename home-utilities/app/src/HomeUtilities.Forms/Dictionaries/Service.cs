using System.ComponentModel;
using System.Text.Json.Serialization;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using HomeUtilities.Enums;
using Microsoft.Extensions.Localization;

namespace HomeUtilities.Forms.Dictionaries;

[TypeId(HomeUtilitiesTypeIds.Service)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "isFolder")]
[JsonDerivedType(typeof(ServiceFolderDto), "1")]
[JsonDerivedType(typeof(ServiceDto), "0")]
[KandraDictionaryForm(Name = "Services", ValidatorType = typeof(ServiceNodeValidator), QueryDtoType = typeof(ServiceQueryDto))]
[EditFormTitle("EditService")]
[AddFormTitle("CreateService")]
[ViewFormTitle("ViewService")]
[ListFormTitle("Services")]
[NavigationName("Nav_Services")]
[Description("A utility service (hierarchical: group folder or specific billable service).")]
public abstract class ServiceNodeDto : HierarchicalDictionaryItemDto
{
}

[Description("A grouping folder in the utility services hierarchy (e.g. Energy, Water, Fees).")]
public class ServiceFolderDto : ServiceNodeDto
{
}

[Description("A specific billable utility service (electricity, water, heating) with unit, billing method, and provider.")]
public class ServiceDto : ServiceNodeDto
{
    [Caption("Service_Unit")]
    [ListFormColumn(Filterable = false)]
    [Description("Measurement unit (kWh, m³, Gcal, month, m²).")]
    public string? Unit { get; set; }

    [Caption("Service_Billing")]
    [ListFormColumn(Filterable = false)]
    [Description("Billing mode: metered, fixed per month, or per square metre.")]
    public BillingType Billing { get; set; }

    [Caption("Service_Provider")]
    [ListFormColumn(Filterable = false)]
    [Search(typeof(ProviderDto))]
    [Description("The provider furnishing this service.")]
    public Guid? ProviderId { get; set; }
}

public class ServiceQueryDto : HierarchicalDictionaryQueryDto
{
}

public class ServiceValidator : DictionaryValidator<ServiceDto>
{
    public ServiceValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}

public class ServiceFolderValidator : DictionaryValidator<ServiceFolderDto>
{
    public ServiceFolderValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}

public class ServiceNodeValidator : DictionaryValidator<ServiceNodeDto>
{
    public ServiceNodeValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => (x as ServiceDto)!)
            .SetValidator(new ServiceValidator(localizer))
            .When(x => x is ServiceDto);

        RuleFor(x => (x as ServiceFolderDto)!)
            .SetValidator(new ServiceFolderValidator(localizer))
            .When(x => x is ServiceFolderDto);
    }
}
