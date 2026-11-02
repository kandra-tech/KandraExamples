using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using HomePantry.Enums;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Dictionaries;

[TypeId(HomePantryTypeIds.Equipment)]
[KandraDictionaryForm(Name = "Equipment", ValidatorType = typeof(EquipmentValidator), QueryDtoType = typeof(EquipmentQueryDto))]
[EditFormTitle("EditEquipment")]
[AddFormTitle("CreateEquipment")]
[ViewFormTitle("ViewEquipment")]
[ListFormTitle("Equipment")]
[NavigationName("Nav_Equipment")]
[Description("Household equipment and appliances tracking location, warranty, manual, and photo.")]
public class EquipmentDto : DictionaryDto
{
    [Caption("Location")]
    [Search(typeof(LocationNodeDto), NodeSelection = HierarchicalNodeSelection.Any)]
    [Description("Location where this equipment is kept.")]
    public Guid LocationId { get; set; }

    [Caption("BoughtOn")]
    [DateEditor]
    [Format("d")]
    [ListFormColumn]
    [Description("Date the equipment was purchased.")]
    public DateTime? BoughtOn { get; set; }

    [Caption("WarrantyUntil")]
    [DateEditor]
    [Format("d")]
    [ListFormColumn]
    [Description("Date until which manufacturer warranty is valid.")]
    public DateTime? WarrantyUntil { get; set; }

    [Caption("Manual")]
    [FilePicker(Extensions = "*.pdf;*.doc;*.docx;*.txt")]
    [Description("User manual or documentation file.")]
    public Guid? ManualFileId { get; set; }

    [Caption("Photo")]
    [FilePicker(Extensions = "*.png;*.jpg;*.jpeg")]
    [Description("Photo of the equipment.")]
    public Guid? PhotoFileId { get; set; }
}

public class EquipmentQueryDto : DictionaryQueryDto
{
}

public class EquipmentValidator : DictionaryValidator<EquipmentDto>
{
    public EquipmentValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.LocationId)
            .NotEmpty()
            .WithMessage(localizer["Equipment_LocationRequired"].Value);
    }
}
