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
using Kandra.Forms.Documents;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Validators.Documents;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Dictionaries;
using Microsoft.Extensions.Localization;

namespace HomeUtilities.Forms.Documents;

[TypeId(HomeUtilitiesTypeIds.SetTariffs)]
[KandraDocumentForm(Name = "SetTariffs", ValidatorType = typeof(SetTariffsValidator), QueryDtoType = typeof(SetTariffsQueryDto))]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(SetTariffsPrintForm))]
[EditFormTitle("EditSetTariffs")]
[AddFormTitle("CreateSetTariffs")]
[ViewFormTitle("ViewSetTariffs")]
[ListFormTitle("SetTariffs")]
[NavigationName("Nav_SetTariffs")]
[Description("A document that sets utility tariff rates per service and zone valid from its date.")]
public class SetTariffsDto : DocumentDto
{
    [Caption("Description")]
    [Description("Optional description or note for the tariff schedule.")]
    public string? Description { get; set; }

    [Caption("Lines")]
    [TablePart]
    public ICollection<SetTariffsLineDto> Lines { get; set; } = [];
}

[Description("A tariff line specifying the service, zone, and price per unit.")]
public class SetTariffsLineDto
{
    public Guid Id { get; set; }

    [Caption("SetTariffs_Service")]
    [GridColumn]
    [Search(typeof(ServiceNodeDto))]
    [Description("The utility service to price.")]
    public Guid ServiceId { get; set; }

    [Caption("SetTariffs_Zone")]
    [GridColumn]
    [Description("Tariff zone: Single, Day, or Night.")]
    public TariffZone Zone { get; set; }

    [Caption("SetTariffs_Price")]
    [GridColumn]
    [Format("N4")]
    [Description("Price per unit in UAH.")]
    public decimal Price { get; set; }
}

public class SetTariffsQueryDto : DocumentQueryDto
{
}

public class SetTariffsValidator : DocumentValidator<SetTariffsDto>
{
    public SetTariffsValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).Must(l => l.ServiceId != Guid.Empty);
        RuleForEach(x => x.Lines).Must(l => l.Price >= 0);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("SetTariffsPrintTitle")]
public sealed class SetTariffsPrintForm : DocumentPrintForm
{
    [Required]
    [PrintTable(LineNumbers = true)]
    public required ICollection<SetTariffsPrintLine> Lines { get; set; }

    public PrintVatMode VatMode { get; set; } = PrintVatMode.NotPayer;

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = "UAH";
}

public sealed class SetTariffsPrintLine
{
    [PrintColumn] [Caption("Service")] public string ServiceName { get; set; } = string.Empty;
    [PrintColumn] [Caption("Zone")] public TariffZone Zone { get; set; }
    [PrintColumn] [Caption("Price")] [Format("N4")] public decimal Price { get; set; }
}
