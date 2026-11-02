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

[TypeId(FamilyGarageTypeIds.VehicleSale)]
[KandraDocumentForm(Name = "VehicleSales", ValidatorType = typeof(VehicleSaleValidator), QueryDtoType = typeof(VehicleSaleQueryDto))]
[EditFormTitle("EditVehicleSale")]
[AddFormTitle("CreateVehicleSale")]
[ViewFormTitle("ViewVehicleSale")]
[ListFormTitle("VehicleSales")]
[NavigationName("Nav_VehicleSales")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(VehicleSalePrintForm))]
[Description("Records the sale of a vehicle, reducing net ownership costs.")]
public class VehicleSaleDto : DocumentDto
{
    [Caption("Vehicle")]
    [Dropdown(typeof(VehicleDto))]
    [ListFormColumn(Filterable = false)]
    [Description("The vehicle being sold.")]
    public Guid VehicleId { get; set; }

    [Caption("Buyer")]
    [ListFormColumn]
    [Description("Buyer name or details.")]
    public string Buyer { get; set; } = string.Empty;

    [Caption("Price")]
    [ListFormColumn(Filterable = false)]
    [Description("Sale price of the vehicle.")]
    public decimal Price { get; set; }

    [Caption("Odometer")]
    [ListFormColumn(Filterable = false)]
    [Description("Odometer reading in kilometres at the time of sale.")]
    public int Odometer { get; set; }
}

public class VehicleSaleQueryDto : DocumentQueryDto
{
}

public class VehicleSaleValidator : DocumentValidator<VehicleSaleDto>
{
    public VehicleSaleValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage(localizer["VehicleRequired"]);
        RuleFor(x => x.Buyer).NotEmpty().WithMessage(localizer["BuyerRequired"]);
        RuleFor(x => x.Price).GreaterThan(0).WithMessage(localizer["PricePositive"]);
        RuleFor(x => x.Odometer).GreaterThanOrEqualTo(0).WithMessage(localizer["OdometerPositive"]);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead)]
[PrintTitle("VehicleSalePrintTitle")]
public sealed class VehicleSalePrintForm : DocumentPrintForm
{
    [Caption("Vehicle")] public string Vehicle { get; set; } = string.Empty;
    [Caption("Buyer")] public string Buyer { get; set; } = string.Empty;
    [Caption("Odometer")] public int Odometer { get; set; }

    public PrintVatMode VatMode { get; set; } = PrintVatMode.NotPayer;
    public IReadOnlyList<PrintTax> Taxes { get; set; } = [];

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = string.Empty;
}
