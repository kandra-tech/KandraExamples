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
using FamilyGarage.Enums;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Forms.Dictionaries;

[TypeId(FamilyGarageTypeIds.Vehicle)]
[KandraDictionaryForm(Name = "Vehicles", ValidatorType = typeof(VehicleValidator), QueryDtoType = typeof(VehicleQueryDto))]
[EditFormTitle("EditVehicle")]
[AddFormTitle("CreateVehicle")]
[ViewFormTitle("ViewVehicle")]
[ListFormTitle("Vehicles")]
[NavigationName("Nav_Vehicles")]
[Description("A vehicle owned by the family.")]
public class VehicleDto : DictionaryDto
{
    [Caption("Make")]
    [ListFormColumn]
    [Description("Manufacturer or brand of the vehicle.")]
    public string Make { get; set; } = string.Empty;

    [Caption("Model")]
    [ListFormColumn]
    [Description("Model name of the vehicle.")]
    public string Model { get; set; } = string.Empty;

    [Caption("Year")]
    [ListFormColumn(Filterable = false)]
    [Description("Manufacture year.")]
    public int Year { get; set; }

    [Caption("PlateNumber")]
    [ListFormColumn]
    [Description("Registration license plate number.")]
    public string PlateNumber { get; set; } = string.Empty;

    [Caption("Vin")]
    [ListFormColumn]
    [Description("Vehicle Identification Number (VIN).")]
    public string Vin { get; set; } = string.Empty;

    [Caption("FuelType")]
    [ListFormColumn(Filterable = false)]
    [Description("Fuel or propulsion type of the vehicle.")]
    public FuelType FuelType { get; set; }

    [Caption("Photo")]
    [FilePicker(Extensions = "*.png;*.jpg;*.jpeg")]
    [Description("Optional photo of the vehicle.")]
    public string? Photo { get; set; }
}

public class VehicleQueryDto : DictionaryQueryDto
{
}

public class VehicleValidator : DictionaryValidator<VehicleDto>
{
    public VehicleValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.Make).NotEmpty().WithMessage(localizer["FieldIsRequired", localizer["Make"]]);
        RuleFor(x => x.Model).NotEmpty().WithMessage(localizer["FieldIsRequired", localizer["Model"]]);
        RuleFor(x => x.Year).GreaterThan(1900).WithMessage(localizer["InvalidYear"]);
        RuleFor(x => x.PlateNumber).NotEmpty().WithMessage(localizer["FieldIsRequired", localizer["PlateNumber"]]);
        RuleFor(x => x.Vin).NotEmpty().WithMessage(localizer["FieldIsRequired", localizer["Vin"]]);
    }
}
