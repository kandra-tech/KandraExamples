using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.DtoCommon.Printing;
using Kandra.Forms.Documents;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Validators.Documents;
using FamilyGarage.Enums;
using FamilyGarage.Forms.Dictionaries;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Forms.Documents;

[TypeId(FamilyGarageTypeIds.Accident)]
[KandraDocumentForm(Name = "Accidents", ValidatorType = typeof(AccidentValidator), QueryDtoType = typeof(AccidentQueryDto))]
[EditFormTitle("EditAccident")]
[AddFormTitle("CreateAccident")]
[ViewFormTitle("ViewAccident")]
[ListFormTitle("Accidents")]
[NavigationName("Nav_Accidents")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(AccidentPrintForm))]
[SourceFor(typeof(ServiceDto))]
[Description("Records an accident involving a vehicle, damage estimation, and photo.")]
public class AccidentDto : DocumentDto
{
    [Caption("Vehicle")]
    [Dropdown(typeof(VehicleDto))]
    [ListFormColumn(Filterable = false)]
    [Description("The vehicle involved in the accident.")]
    public Guid VehicleId { get; set; }

    [Caption("Odometer")]
    [ListFormColumn(Filterable = false)]
    [Description("Odometer reading at accident.")]
    public int Odometer { get; set; }

    [Caption("Place")]
    [ListFormColumn]
    [Description("Location of the accident.")]
    public string Place { get; set; } = string.Empty;

    [Caption("Description")]
    [ListFormColumn]
    [Description("Details of what occurred.")]
    public string Description { get; set; } = string.Empty;

    [Caption("EstimatedDamage")]
    [ListFormColumn(Filterable = false)]
    [Description("Estimated cost of damage.")]
    public decimal EstimatedDamage { get; set; }

    [Caption("Photo")]
    [Description("Photo of damage or incident scene.")]
    public string? Photo { get; set; }
}

public class AccidentQueryDto : DocumentQueryDto
{
}

public class AccidentValidator : DocumentValidator<AccidentDto>
{
    public AccidentValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage(localizer["VehicleRequired"]);
        RuleFor(x => x.Place).NotEmpty().WithMessage(localizer["PlaceRequired"]);
        RuleFor(x => x.Description).NotEmpty().WithMessage(localizer["DescriptionRequired"]);
        RuleFor(x => x.Odometer).GreaterThanOrEqualTo(0).WithMessage(localizer["OdometerPositive"]);
        RuleFor(x => x.EstimatedDamage).GreaterThanOrEqualTo(0).WithMessage(localizer["EstimatedDamagePositive"]);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead)]
[PrintTitle("AccidentPrintTitle")]
public sealed class AccidentPrintForm : DocumentPrintForm
{
    [Caption("Vehicle")] public string Vehicle { get; set; } = string.Empty;
    [Caption("Odometer")] public int Odometer { get; set; }
    [Caption("Place")] public string Place { get; set; } = string.Empty;
    [Caption("Description")] public string Description { get; set; } = string.Empty;

    public PrintVatMode VatMode { get; set; } = PrintVatMode.NotPayer;
    public IReadOnlyList<PrintTax> Taxes { get; set; } = [];

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = string.Empty;
}
