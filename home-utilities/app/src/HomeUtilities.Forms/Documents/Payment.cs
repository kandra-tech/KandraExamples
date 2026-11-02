using System.ComponentModel;
using FluentValidation;
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
using HomeUtilities.Enums;
using HomeUtilities.Forms.Dictionaries;
using Microsoft.Extensions.Localization;

namespace HomeUtilities.Forms.Documents;

[TypeId(HomeUtilitiesTypeIds.Payment)]
[KandraDocumentForm(Name = "Payments", ValidatorType = typeof(PaymentValidator), QueryDtoType = typeof(PaymentQueryDto))]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(PaymentPrintForm))]
[EditFormTitle("EditPayment")]
[AddFormTitle("CreatePayment")]
[ViewFormTitle("ViewPayment")]
[ListFormTitle("Payments")]
[NavigationName("Nav_Payments")]
[Description("A payment record to a utility provider for services rendered at a property.")]
public class PaymentDto : DocumentDto
{
    [Caption("Payment_Provider")]
    [ListFormColumn]
    [Search(typeof(ProviderDto))]
    [Description("The utility provider paid.")]
    public Guid ProviderId { get; set; }

    [Caption("Payment_Property")]
    [ListFormColumn]
    [Search(typeof(PropertyDto))]
    [Description("The property this utility payment applies to.")]
    public Guid PropertyId { get; set; }

    [Caption("Payment_Amount")]
    [ListFormColumn]
    [Format("N2")]
    [Description("Payment amount in UAH.")]
    public decimal Amount { get; set; }

    [Caption("Payment_PaymentDescription")]
    [Multiline]
    [Description("Payment purpose / description for banking transfer.")]
    public string? PaymentDescription { get; set; }

    [Caption("Payment_Comment")]
    [ListFormColumn]
    [Description("Optional payment purpose or comment.")]
    public string? Comment { get; set; }
}

public class PaymentQueryDto : DocumentQueryDto
{
}

public class PaymentValidator : DocumentValidator<PaymentDto>
{
    public PaymentValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("PaymentPrintTitle")]
public sealed class PaymentPrintForm : DocumentPrintForm
{
    [Caption("Provider")]
    public string ProviderName { get; set; } = string.Empty;

    [Caption("Invoice_ProviderIban")]
    public string? ProviderIban { get; set; }

    [Caption("Invoice_ProviderEdrpou")]
    public string? ProviderEdrpou { get; set; }

    [Caption("Invoice_ProviderBankName")]
    public string? ProviderBankName { get; set; }

    [Caption("Property")]
    public string PropertyName { get; set; } = string.Empty;

    [Caption("Property_Owner")]
    public string? PropertyOwner { get; set; }

    [Caption("Payment_PaymentDescription")]
    public string? PaymentDescription { get; set; }

    [Caption("Purpose")]
    public string? Comment { get; set; }

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = string.Empty;
}
