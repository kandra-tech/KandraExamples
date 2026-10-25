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
using Kandra.Forms.UiBehaviors;
using Kandra.Validators.Documents;
using FamilyGarage.Enums;
using FamilyGarage.Forms.Dictionaries;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Forms.Documents;

[TypeId(FamilyGarageTypeIds.VehiclePurchase)]
[KandraDocumentForm(Name = "VehiclePurchases", ValidatorType = typeof(VehiclePurchaseValidator), QueryDtoType = typeof(VehiclePurchaseQueryDto))]
[EditFormTitle("EditVehiclePurchase")]
[AddFormTitle("CreateVehiclePurchase")]
[ViewFormTitle("ViewVehiclePurchase")]
[ListFormTitle("VehiclePurchases")]
[NavigationName("Nav_VehiclePurchases")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(VehiclePurchasePrintForm))]
[UiBehavior(typeof(VehiclePurchaseUiBehavior))]
[Description("Records the purchase of a vehicle, whether paid in cash or financed via a bank loan or leasing.")]
public class VehiclePurchaseDto : DocumentDto
{
    [Caption("Vehicle")]
    [Dropdown(typeof(VehicleDto))]
    [ListFormColumn(Filterable = false)]
    [Description("The vehicle being purchased.")]
    public Guid VehicleId { get; set; }

    [Caption("Seller")]
    [Dropdown(typeof(ProviderDto))]
    [Description("The provider/seller of the vehicle.")]
    public Guid SellerId { get; set; }

    [Caption("Price")]
    [ListFormColumn(Filterable = false)]
    [Description("Total purchase price of the vehicle.")]
    public decimal Price { get; set; }

    [Caption("Odometer")]
    [ListFormColumn(Filterable = false)]
    [Description("Odometer reading in kilometres at the time of purchase.")]
    public int Odometer { get; set; }

    [Caption("Payment")]
    [ListFormColumn(Filterable = false)]
    [Description("Payment method: cash, bank loan, or leasing.")]
    public VehiclePurchasePaymentKind Payment { get; set; } = VehiclePurchasePaymentKind.Cash;

    [Caption("DownPayment")]
    [Visible(nameof(VehiclePurchaseUiBehavior.IsFinanced))]
    [Description("Initial down payment when financed.")]
    public decimal DownPayment { get; set; }

    [Caption("FinanceContract")]
    [Dropdown(typeof(FinanceContractDto))]
    [Visible(nameof(VehiclePurchaseUiBehavior.IsFinanced))]
    [Description("Associated finance contract when payment is not cash.")]
    public Guid? FinanceContractId { get; set; }
}

public sealed class VehiclePurchaseUiBehavior : DocumentUiBehavior<VehiclePurchaseDto>
{
    public bool IsFinanced() => Record.Payment != VehiclePurchasePaymentKind.Cash;
}

public class VehiclePurchaseQueryDto : DocumentQueryDto
{
}

public class VehiclePurchaseValidator : DocumentValidator<VehiclePurchaseDto>
{
    public VehiclePurchaseValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage(localizer["VehicleRequired"]);
        RuleFor(x => x.SellerId).NotEmpty().WithMessage(localizer["SellerRequired"]);
        RuleFor(x => x.Price).GreaterThan(0).WithMessage(localizer["PricePositive"]);
        RuleFor(x => x.Odometer).GreaterThanOrEqualTo(0).WithMessage(localizer["OdometerPositive"]);
        When(x => x.Payment != VehiclePurchasePaymentKind.Cash, () =>
        {
            RuleFor(x => x.DownPayment).GreaterThanOrEqualTo(0).WithMessage(localizer["DownPaymentPositive"]);
            RuleFor(x => x.DownPayment).LessThan(x => x.Price).WithMessage(localizer["DownPaymentBelowPrice"]);
            RuleFor(x => x.FinanceContractId).NotEmpty().WithMessage(localizer["FinanceContractRequired"]);
        });
    }
}

[PrintForm(Frame = PrintFrame.Letterhead)]
[PrintTitle("VehiclePurchasePrintTitle")]
public sealed class VehiclePurchasePrintForm : DocumentPrintForm
{
    [Caption("Vehicle")] public string Vehicle { get; set; } = string.Empty;
    [Caption("Seller")] public string Seller { get; set; } = string.Empty;
    [Caption("Odometer")] public int Odometer { get; set; }
    [Caption("Payment")] public string Payment { get; set; } = string.Empty;
    [Caption("DownPayment")] public decimal? DownPayment { get; set; }
    [Caption("FinancedAmount")] public decimal? FinancedAmount { get; set; }
    [Caption("FinanceContract")] public string? FinanceContract { get; set; }

    public PrintVatMode VatMode { get; set; } = PrintVatMode.NotPayer;
    public IReadOnlyList<PrintTax> Taxes { get; set; } = [];

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = string.Empty;
}
