using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using HomePantry.Enums;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Dictionaries;

[TypeId(HomePantryTypeIds.Battery)]
[KandraDictionaryForm(Name = "Batteries", ValidatorType = typeof(BatteryValidator), QueryDtoType = typeof(BatteryQueryDto))]
[EditFormTitle("EditBattery")]
[AddFormTitle("CreateBattery")]
[ViewFormTitle("ViewBattery")]
[ListFormTitle("Batteries")]
[NavigationName("Nav_Batteries")]
[Description("A household battery tracking device location, charge interval, and type.")]
public class BatteryDto : DictionaryDto
{
    [Caption("UsedIn")]
    [ListFormColumn]
    [Description("Device, appliance or gadget where this battery is used.")]
    public string UsedIn { get; set; } = string.Empty;

    [Caption("ChargeIntervalDays")]
    [ListFormColumn]
    [Description("Recommended interval between charges or replacements in days.")]
    public int ChargeIntervalDays { get; set; } = 30;

    [Caption("BatteryKind")]
    [ListFormColumn]
    [Description("Whether the battery is rechargeable or disposable.")]
    public BatteryKind BatteryKind { get; set; } = BatteryKind.Rechargeable;
}

public class BatteryQueryDto : DictionaryQueryDto
{
}

public class BatteryValidator : DictionaryValidator<BatteryDto>
{
    public BatteryValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.UsedIn)
            .NotEmpty()
            .WithMessage(localizer["Battery_UsedInRequired"].Value);

        RuleFor(x => x.ChargeIntervalDays)
            .GreaterThan(0)
            .WithMessage(localizer["Battery_ChargeIntervalPositive"].Value);
    }
}
