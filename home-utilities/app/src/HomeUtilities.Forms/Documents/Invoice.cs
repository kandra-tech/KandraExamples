using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.DtoCommon.Printing;
using Kandra.Forms.ApiClients;
using Kandra.Forms.Documents;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Forms.UiBehaviors;
using Kandra.Validators.Documents;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Dictionaries;
using HomeUtilities.Forms.Handlers;
using Microsoft.Extensions.Localization;

namespace HomeUtilities.Forms.Documents;

[TypeId(HomeUtilitiesTypeIds.Invoice)]
[KandraDocumentForm(Name = "Invoices", ValidatorType = typeof(InvoiceValidator), QueryDtoType = typeof(InvoiceQueryDto))]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(InvoicePrintForm))]
[SourceFor(typeof(PaymentDto))]
[UiBehavior(typeof(InvoiceUiBehavior))]
[EditFormTitle("EditInvoice")]
[AddFormTitle("CreateInvoice")]
[ViewFormTitle("ViewInvoice")]
[ListFormTitle("Invoices")]
[NavigationName("Nav_Invoices")]
[Description("A utility bill invoice for a provider, property, and billing month.")]
public class InvoiceDto : DocumentDto
{
    [Caption("Invoice_Provider")]
    [ListFormColumn]
    [Search(typeof(ProviderDto))]
    [Description("Utility provider issuing the invoice.")]
    public Guid ProviderId { get; set; }

    [Caption("Invoice_Property")]
    [ListFormColumn]
    [Search(typeof(PropertyDto))]
    [Description("Household property billed.")]
    public Guid PropertyId { get; set; }

    [Caption("Invoice_Month")]
    [ListFormColumn(Filterable = false)]
    [DateEditor]
    [Description("The billing month (first day of the month).")]
    public DateTime Month { get; set; }

    [Caption("Invoice_Calculate")]
    [OnClick(nameof(InvoiceUiBehavior.OnCalculateClicked), Icon = "Calculate", Tooltip = "Invoice_Calculate_Tooltip")]
    [Description("Calculate lines based on meter readings, area, and valid tariffs.")]
    public string? CalculateTrigger { get; set; }

    [Caption("Invoice_TotalAmount")]
    [ListFormColumn]
    [Format("N2")]
    [TableTotal(nameof(Lines), nameof(InvoiceLineDto.Amount))]
    [Description("Total invoice amount owed in UAH.")]
    public decimal TotalAmount { get; set; }

    [Caption("Invoice_PaymentDescription")]
    [Multiline]
    [Description("Payment purpose / description for banking transfer.")]
    public string? PaymentDescription { get; set; }

    [Caption("Description")]
    [Description("Optional comment or note.")]
    public string? Description { get; set; }

    [Caption("Lines")]
    [TablePart]
    [OnLineRemove(nameof(InvoiceUiBehavior.OnLineRemoved))]
    public ICollection<InvoiceLineDto> Lines { get; set; } = [];
}

[Description("A line of a utility invoice representing service consumption and charges.")]
public class InvoiceLineDto
{
    public Guid Id { get; set; }

    [Caption("Invoice_Service")]
    [GridColumn]
    [Search(typeof(ServiceNodeDto))]
    [Description("The utility service charged.")]
    public Guid ServiceId { get; set; }

    [Caption("Invoice_Meter")]
    [GridColumn]
    [Search(typeof(MeterDto))]
    [Description("The meter measuring this service, if metered.")]
    public Guid? MeterId { get; set; }

    [Caption("Invoice_Zone")]
    [GridColumn]
    [Description("Tariff zone: Single, Day, or Night.")]
    public TariffZone Zone { get; set; } = TariffZone.Single;

    [Caption("Invoice_Quantity")]
    [GridColumn]
    [Format("N3")]
    [OnChange(nameof(InvoiceUiBehavior.OnLineQuantityOrPriceChanged))]
    [Description("Consumption quantity or area.")]
    public decimal Quantity { get; set; }

    [Caption("Invoice_Price")]
    [GridColumn]
    [Format("N4")]
    [OnChange(nameof(InvoiceUiBehavior.OnLineQuantityOrPriceChanged))]
    [Description("Applicable tariff price per unit in UAH.")]
    public decimal Price { get; set; }

    [Caption("Invoice_Amount")]
    [GridColumn]
    [Format("N2")]
    [OnChange(nameof(InvoiceUiBehavior.OnLineAmountChanged))]
    [Description("Line total: quantity times price, or the amount on the provider's invoice when the user types it.")]
    public decimal Amount { get; set; }
}

public class InvoiceQueryDto : DocumentQueryDto
{
}

public class InvoiceValidator : DocumentValidator<InvoiceDto>
{
    public InvoiceValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).Must(l => l.ServiceId != Guid.Empty);
    }
}

public sealed class InvoiceUiBehavior(
    IHandlerApiClient<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto> calculateClient)
    : DocumentUiBehavior<InvoiceDto>
{
    public async Task OnCalculateClicked(CancellationToken cancellationToken)
    {
        if (Record.ProviderId == Guid.Empty || Record.PropertyId == Guid.Empty)
            return;

        var response = await calculateClient.HandleAsync(new InvoiceCalculateRequestDto
        {
            ProviderId = Record.ProviderId,
            PropertyId = Record.PropertyId,
            Month = Record.Month == default ? Record.Date : Record.Month,
        }, cancellationToken);

        Record.Lines.Clear();
        foreach (var line in response.Lines)
        {
            Record.Lines.Add(new InvoiceLineDto
            {
                Id = Guid.NewGuid(),
                ServiceId = line.ServiceId,
                MeterId = line.MeterId,
                Zone = line.Zone,
                Quantity = line.Quantity,
                Price = line.Price,
                Amount = line.Amount,
            });
        }
        Record.TotalAmount = response.TotalAmount;
        if (!string.IsNullOrWhiteSpace(response.PaymentDescription))
        {
            Record.PaymentDescription = response.PaymentDescription;
        }
    }

    public void OnLineQuantityOrPriceChanged(InvoiceLineDto line)
    {
        line.Amount = Math.Round(line.Quantity * line.Price, 2);
        Record.TotalAmount = Record.Lines.Sum(l => l.Amount);
    }

    public void OnLineAmountChanged(InvoiceLineDto line)
    {
        Record.TotalAmount = Record.Lines.Sum(l => l.Amount);
    }

    public void OnLineRemoved(InvoiceLineDto line)
    {
        Record.TotalAmount = Record.Lines.Sum(l => l.Amount);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("InvoicePrintTitle")]
public sealed class InvoicePrintForm : DocumentPrintForm
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

    [Caption("Month")]
    public string BillingMonth { get; set; } = string.Empty;

    [Caption("Invoice_PaymentDescription")]
    public string? PaymentDescription { get; set; }

    [Required]
    [PrintTable(LineNumbers = true)]
    public required ICollection<InvoicePrintLine> Lines { get; set; }

    public PrintVatMode VatMode { get; set; } = PrintVatMode.NotPayer;

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = string.Empty;
}

public sealed class InvoicePrintLine
{
    [PrintColumn] [Caption("Service")] public string ServiceName { get; set; } = string.Empty;
    [PrintColumn] [Caption("Meter")] public string? MeterName { get; set; }
    [PrintColumn] [Caption("Zone")] public TariffZone Zone { get; set; }
    [PrintColumn] [Caption("Quantity")] [Format(PrintFormats.Quantity)] public decimal Quantity { get; set; }
    [PrintColumn] [Caption("Price")] [Format("N4")] public decimal Price { get; set; }
    [PrintColumn] [Caption("Sum")] [Format(PrintFormats.Money)] public decimal Amount { get; set; }
}
