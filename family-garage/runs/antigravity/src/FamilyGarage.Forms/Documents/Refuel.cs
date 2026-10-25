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

[TypeId(FamilyGarageTypeIds.Refuel)]
[KandraDocumentForm(Name = "Refuels", ValidatorType = typeof(RefuelValidator), QueryDtoType = typeof(RefuelQueryDto))]
[EditFormTitle("EditRefuel")]
[AddFormTitle("CreateRefuel")]
[ViewFormTitle("ViewRefuel")]
[ListFormTitle("Refuels")]
[NavigationName("Nav_Refuels")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(RefuelPrintForm))]
[Description("Records vehicle refueling, updating odometer and fuel expense.")]
public class RefuelDto : DocumentDto
{
    [Caption("Vehicle")]
    [Dropdown(typeof(VehicleDto))]
    [ListFormColumn(Filterable = false)]
    [Description("The vehicle being refueled.")]
    public Guid VehicleId { get; set; }

    [Caption("FuelStation")]
    [Dropdown(typeof(ProviderDto))]
    [Description("The fuel provider station.")]
    public Guid FuelStationId { get; set; }

    [Caption("Litres")]
    [ListFormColumn(Filterable = false)]
    [Description("Litres of fuel purchased.")]
    public decimal Litres { get; set; }

    [Caption("Amount")]
    [ListFormColumn(Filterable = false)]
    [Description("Total cost of fuel.")]
    public decimal Amount { get; set; }

    [Caption("Odometer")]
    [ListFormColumn(Filterable = false)]
    [Description("Odometer reading at refuel.")]
    public int Odometer { get; set; }

    [Caption("FullTank")]
    [ListFormColumn(Filterable = false)]
    [Description("Whether tank was filled completely.")]
    public bool FullTank { get; set; }
}

public class RefuelQueryDto : DocumentQueryDto
{
}

public class RefuelValidator : DocumentValidator<RefuelDto>
{
    public RefuelValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage(localizer["VehicleRequired"]);
        RuleFor(x => x.FuelStationId).NotEmpty().WithMessage(localizer["FuelStationRequired"]);
        RuleFor(x => x.Litres).GreaterThan(0).WithMessage(localizer["LitresPositive"]);
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage(localizer["AmountPositive"]);
        RuleFor(x => x.Odometer).GreaterThanOrEqualTo(0).WithMessage(localizer["OdometerPositive"]);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead)]
[PrintTitle("RefuelPrintTitle")]
public sealed class RefuelPrintForm : DocumentPrintForm
{
    [Caption("Vehicle")] public string Vehicle { get; set; } = string.Empty;
    [Caption("FuelStation")] public string FuelStation { get; set; } = string.Empty;
    [Caption("Litres")] public decimal Litres { get; set; }
    [Caption("Odometer")] public int Odometer { get; set; }
    [Caption("FullTank")] public bool FullTank { get; set; }

    public PrintVatMode VatMode { get; set; } = PrintVatMode.NotPayer;
    public IReadOnlyList<PrintTax> Taxes { get; set; } = [];

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = string.Empty;
}
