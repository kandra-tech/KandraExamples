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

[TypeId(FamilyGarageTypeIds.FinancePayment)]
[KandraDocumentForm(Name = "FinancePayments", ValidatorType = typeof(FinancePaymentValidator), QueryDtoType = typeof(FinancePaymentQueryDto))]
[EditFormTitle("EditFinancePayment")]
[AddFormTitle("CreateFinancePayment")]
[ViewFormTitle("ViewFinancePayment")]
[ListFormTitle("FinancePayments")]
[NavigationName("Nav_FinancePayments")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(FinancePaymentPrintForm))]
[Description("Records a finance payment against a finance contract, reducing remaining debt.")]
public class FinancePaymentDto : DocumentDto
{
    [Caption("FinanceContract")]
    [Dropdown(typeof(FinanceContractDto))]
    [ListFormColumn(Filterable = false)]
    [Description("The finance contract being paid.")]
    public Guid FinanceContractId { get; set; }

    [Caption("Principal")]
    [ListFormColumn(Filterable = false)]
    [Description("Principal portion of payment reducing debt.")]
    public decimal Principal { get; set; }

    [Caption("Interest")]
    [ListFormColumn(Filterable = false)]
    [Description("Interest portion of payment.")]
    public decimal Interest { get; set; }

    [Caption("Fees")]
    [ListFormColumn(Filterable = false)]
    [Description("Bank or leasing fees.")]
    public decimal Fees { get; set; }

    [Caption("Comment")]
    [ListFormColumn]
    [Description("Optional notes about this payment.")]
    public string? Comment { get; set; }
}

public class FinancePaymentQueryDto : DocumentQueryDto
{
}

public class FinancePaymentValidator : DocumentValidator<FinancePaymentDto>
{
    public FinancePaymentValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.FinanceContractId).NotEmpty().WithMessage(localizer["FinanceContractRequired"]);
        RuleFor(x => x.Principal).GreaterThanOrEqualTo(0).WithMessage(localizer["PrincipalPositive"]);
        RuleFor(x => x.Interest).GreaterThanOrEqualTo(0).WithMessage(localizer["InterestPositive"]);
        RuleFor(x => x.Fees).GreaterThanOrEqualTo(0).WithMessage(localizer["FeesPositive"]);
        RuleFor(x => x).Must(x => x.Principal + x.Interest + x.Fees > 0)
            .WithMessage(localizer["PaymentTotalMustBePositive"]);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead)]
[PrintTitle("FinancePaymentPrintTitle")]
public sealed class FinancePaymentPrintForm : DocumentPrintForm
{
    [Caption("FinanceContract")] public string FinanceContract { get; set; } = string.Empty;
    [Caption("Principal")] public decimal Principal { get; set; }
    [Caption("Interest")] public decimal Interest { get; set; }
    [Caption("Fees")] public decimal Fees { get; set; }
    [Caption("Comment")] public string? Comment { get; set; }

    public PrintVatMode VatMode { get; set; } = PrintVatMode.NotPayer;
    public IReadOnlyList<PrintTax> Taxes { get; set; } = [];

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = string.Empty;
}
