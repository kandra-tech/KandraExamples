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
using HomeUtilities.Enums;
using Microsoft.Extensions.Localization;

namespace HomeUtilities.Forms.Dictionaries;

[TypeId(HomeUtilitiesTypeIds.Meter)]
[KandraDictionaryForm(Name = "Meters", ValidatorType = typeof(MeterValidator), QueryDtoType = typeof(MeterQueryDto))]
[EditFormTitle("EditMeter")]
[AddFormTitle("CreateMeter")]
[ViewFormTitle("ViewMeter")]
[ListFormTitle("Meters")]
[NavigationName("Nav_Meters")]
[Description("A utility meter measuring consumption at a property for a service.")]
public class MeterDto : DictionaryDto
{
    [Caption("Meter_Property")]
    [ListFormColumn]
    [Search(typeof(PropertyDto))]
    [Description("The property where this meter is installed.")]
    public Guid PropertyId { get; set; }

    [Caption("Meter_Service")]
    [ListFormColumn]
    [Search(typeof(ServiceNodeDto))]
    [Description("The metered service (electricity, gas, water).")]
    public Guid ServiceId { get; set; }

    [Caption("Meter_SerialNumber")]
    [ListFormColumn]
    [Description("Serial number stamped on the meter casing.")]
    public string? SerialNumber { get; set; }

    [Caption("Meter_Zones")]
    [ListFormColumn]
    [Description("Tariff zones supported by this meter (single or day-night).")]
    public MeterZones Zones { get; set; }

    [Caption("Meter_NextVerificationDate")]
    [ListFormColumn(Filterable = false)]
    [DateEditor]
    [Description("Date by which the meter must undergo state technical verification.")]
    public DateTime NextVerificationDate { get; set; }
}

public class MeterQueryDto : DictionaryQueryDto
{
}

public class MeterValidator : DictionaryValidator<MeterDto>
{
    public MeterValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
    }
}
