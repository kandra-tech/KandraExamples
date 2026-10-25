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

[TypeId(FamilyGarageTypeIds.Service)]
[KandraDocumentForm(Name = "Services", ValidatorType = typeof(ServiceValidator), QueryDtoType = typeof(ServiceQueryDto))]
[EditFormTitle("EditService")]
[AddFormTitle("CreateService")]
[ViewFormTitle("ViewService")]
[ListFormTitle("Services")]
[NavigationName("Nav_Services")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(ServicePrintForm))]
[UiBehavior(typeof(ServiceUiBehavior))]
[Description("Records vehicle maintenance or repairs, either summary or detailed line-by-line.")]
public class ServiceDto : DocumentDto
{
    [Caption("Vehicle")]
    [Dropdown(typeof(VehicleDto))]
    [ListFormColumn(Filterable = false)]
    [Description("The vehicle being serviced.")]
    public Guid VehicleId { get; set; }

    [Caption("ServiceStation")]
    [Dropdown(typeof(ProviderDto))]
    [Description("The service provider station.")]
    public Guid ServiceStationId { get; set; }

    [Caption("Kind")]
    [ListFormColumn(Filterable = false)]
    [Description("Service kind: maintenance or repair.")]
    public ServiceKind Kind { get; set; } = ServiceKind.Maintenance;

    [Caption("Odometer")]
    [ListFormColumn(Filterable = false)]
    [Description("Odometer reading at service.")]
    public int Odometer { get; set; }

    [Caption("Comment")]
    [ListFormColumn]
    [Description("Optional notes about this service.")]
    public string? Comment { get; set; }

    [Caption("Detailed")]
    [ListFormColumn(Filterable = false)]
    [OnChange(nameof(ServiceUiBehavior.OnDetailedChanged))]
    [Description("When detailed, itemized lines are entered.")]
    public bool Detailed { get; set; }

    [Caption("Amount")]
    [ListFormColumn(Filterable = false)]
    [Visible(nameof(ServiceUiBehavior.IsNotDetailed))]
    [Description("Total cost when summary service.")]
    public decimal Amount { get; set; }

    [Caption("Lines")]
    [Visible(nameof(ServiceUiBehavior.IsDetailed))]
    [TablePart]
    [OnLineRemove(nameof(ServiceUiBehavior.OnLineRemoved))]
    [Description("Itemized parts and works performed.")]
    public ICollection<ServiceLineDto> Lines { get; set; } = [];

    [Caption("Accident")]
    [Dropdown(typeof(AccidentDto))]
    [Description("Associated accident if this service is a repair for an accident.")]
    public Guid? AccidentId { get; set; }
}

public class ServiceLineDto
{
    [Caption("Id")]
    public Guid Id { get; set; }

    [Caption("Item")]
    [Dropdown(typeof(ItemNodeDto))]
    [GridColumn(Order = 1, WidthPercent = 40)]
    [Description("Part, work, or consumable item.")]
    public Guid ItemId { get; set; }

    [Caption("Quantity")]
    [GridColumn(Order = 2, WidthPercent = 20)]
    [OnChange(nameof(ServiceUiBehavior.OnLineChanged))]
    [Description("Quantity installed or performed.")]
    public decimal Quantity { get; set; } = 1;

    [Caption("Price")]
    [GridColumn(Order = 3, WidthPercent = 20)]
    [OnChange(nameof(ServiceUiBehavior.OnLineChanged))]
    [Description("Unit price.")]
    public decimal Price { get; set; }

    [Caption("Amount")]
    [GridColumn(Order = 4, WidthPercent = 20)]
    [Description("Total amount for this line.")]
    public decimal Amount { get; set; }
}

public sealed class ServiceUiBehavior : DocumentUiBehavior<ServiceDto>
{
    public bool IsDetailed() => Record.Detailed;
    public bool IsNotDetailed() => !Record.Detailed;

    public void OnDetailedChanged()
    {
        if (Record.Detailed)
        {
            RecalcTotal();
        }
    }

    public void OnLineChanged(ServiceLineDto line)
    {
        line.Amount = line.Quantity * line.Price;
        RecalcTotal();
    }

    public void OnLineRemoved(ServiceLineDto line)
    {
        RecalcTotal();
    }

    private void RecalcTotal()
    {
        if (Record.Detailed)
        {
            Record.Amount = Record.Lines.Sum(l => l.Amount);
        }
    }
}

public class ServiceQueryDto : DocumentQueryDto
{
}

public class ServiceValidator : DocumentValidator<ServiceDto>
{
    public ServiceValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage(localizer["VehicleRequired"]);
        RuleFor(x => x.ServiceStationId).NotEmpty().WithMessage(localizer["ServiceStationRequired"]);
        RuleFor(x => x.Odometer).GreaterThanOrEqualTo(0).WithMessage(localizer["OdometerPositive"]);
        When(x => !x.Detailed, () =>
        {
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage(localizer["AmountRequired"]);
        });
        When(x => x.Detailed, () =>
        {
            RuleFor(x => x.Lines).NotEmpty().WithMessage(localizer["LinesRequired"]);
        });
    }
}

[PrintForm(Frame = PrintFrame.Letterhead)]
[PrintTitle("ServicePrintTitle")]
public sealed class ServicePrintForm : DocumentPrintForm
{
    [Caption("Vehicle")] public string Vehicle { get; set; } = string.Empty;
    [Caption("ServiceStation")] public string ServiceStation { get; set; } = string.Empty;
    [Caption("Kind")] public string Kind { get; set; } = string.Empty;
    [Caption("Odometer")] public int Odometer { get; set; }
    [Caption("Comment")] public string? Comment { get; set; }

    [PrintTable(LineNumbers = true, HideWhenEmpty = true)]
    public ICollection<ServicePrintLine> Lines { get; set; } = [];

    public PrintVatMode VatMode { get; set; } = PrintVatMode.NotPayer;
    public IReadOnlyList<PrintTax> Taxes { get; set; } = [];

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = string.Empty;
}

public sealed class ServicePrintLine
{
    [PrintColumn] [Caption("Item")] public string ItemName { get; set; } = string.Empty;
    [PrintColumn(Align = TextAlign.Right)] [Caption("Quantity")] public decimal Quantity { get; set; }
    [PrintColumn(Align = TextAlign.Right)] [Caption("Price")] public decimal Price { get; set; }
    [PrintColumn(Align = TextAlign.Right)] [Caption("Amount")] public decimal Amount { get; set; }
}
