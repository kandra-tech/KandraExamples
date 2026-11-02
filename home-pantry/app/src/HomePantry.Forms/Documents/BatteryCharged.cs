using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Documents;
using Kandra.Forms.Querying;
using Kandra.Validators.Documents;
using HomePantry.Enums;
using HomePantry.Forms.Dictionaries;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Documents;

[TypeId(HomePantryTypeIds.BatteryCharged)]
[KandraDocumentForm(Name = "BatteriesCharged", ValidatorType = typeof(BatteryChargedValidator), QueryDtoType = typeof(BatteryChargedQueryDto))]
[EditFormTitle("EditBatteryCharged")]
[AddFormTitle("CreateBatteryCharged")]
[ViewFormTitle("ViewBatteryCharged")]
[ListFormTitle("BatteriesCharged")]
[NavigationName("Nav_BatteriesCharged")]
[Description("Records charging or replacement of a household battery.")]
public class BatteryChargedDto : DocumentDto
{
    [Caption("Battery")]
    [Search(typeof(BatteryDto))]
    [ListFormColumn]
    [Description("The battery that was charged or replaced.")]
    public Guid BatteryId { get; set; }

    [Caption("Replaced")]
    [ListFormColumn]
    [Description("Whether the battery was replaced rather than recharged. Disposable batteries are always replaced.")]
    public bool Replaced { get; set; }
}

public class BatteryChargedQueryDto : DocumentQueryDto
{
}

public class BatteryChargedValidator : DocumentValidator<BatteryChargedDto>
{
    public BatteryChargedValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.BatteryId)
            .NotEmpty()
            .WithMessage(localizer["BatteryCharged_BatteryRequired"].Value);
    }
}
