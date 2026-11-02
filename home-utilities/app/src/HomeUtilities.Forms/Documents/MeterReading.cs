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

[TypeId(HomeUtilitiesTypeIds.MeterReading)]
[KandraDocumentForm(Name = "MeterReadings", ValidatorType = typeof(MeterReadingValidator), QueryDtoType = typeof(MeterReadingQueryDto))]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(MeterReadingPrintForm))]
[UiBehavior(typeof(MeterReadingUiBehavior))]
[EditFormTitle("EditMeterReading")]
[AddFormTitle("CreateMeterReading")]
[ViewFormTitle("ViewMeterReading")]
[ListFormTitle("MeterReadings")]
[NavigationName("Nav_MeterReadings")]
[Description("A document that records utility meter readings for a property on a date.")]
public class MeterReadingDto : DocumentDto
{
    [Caption("MeterReading_Property")]
    [ListFormColumn]
    [Search(typeof(PropertyDto))]
    [OnChange(nameof(MeterReadingUiBehavior.OnPropertyOrFillChanged))]
    [Description("The property whose meter readings are recorded.")]
    public Guid PropertyId { get; set; }

    [Caption("MeterReading_Fill")]
    [OnClick(nameof(MeterReadingUiBehavior.OnFillClicked), Icon = "AutoFixHigh", Tooltip = "MeterReading_Fill_Tooltip")]
    [Description("Action trigger to fill lines with meters of the selected property and their previous readings.")]
    public string? FillTrigger { get; set; }

    [Caption("Description")]
    [Description("Optional note or comment for this reading event.")]
    public string? Description { get; set; }

    [Caption("Lines")]
    [TablePart]
    public ICollection<MeterReadingLineDto> Lines { get; set; } = [];
}

[Description("A recorded meter reading line with previous and current readings.")]
public class MeterReadingLineDto
{
    public Guid Id { get; set; }

    [Caption("MeterReading_Meter")]
    [GridColumn]
    [Search(typeof(MeterDto))]
    [Description("The meter being read.")]
    public Guid MeterId { get; set; }

    [Caption("MeterReading_Zone")]
    [GridColumn]
    [Description("Tariff zone: Single, Day, or Night.")]
    public TariffZone Zone { get; set; }

    [Caption("MeterReading_PreviousReading")]
    [GridColumn]
    [Format("N3")]
    [NotEditable]
    [Description("Previous recorded reading.")]
    public decimal PreviousReading { get; set; }

    [Caption("MeterReading_Reading")]
    [GridColumn]
    [Format("N3")]
    [Description("Current meter reading.")]
    public decimal Reading { get; set; }
}

public class MeterReadingQueryDto : DocumentQueryDto
{
}

public class MeterReadingValidator : DocumentValidator<MeterReadingDto>
{
    public MeterReadingValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).Must(l => l.MeterId != Guid.Empty);
        RuleForEach(x => x.Lines).Must(l => l.Reading >= l.PreviousReading)
            .WithMessage((_, l) => localizer["ReadingLowerThanPrevious", l.Reading, l.PreviousReading]);
    }
}

public sealed class MeterReadingUiBehavior(
    IHandlerApiClient<MeterReadingFillRequestDto, MeterReadingFillResponseDto> fillClient)
    : DocumentUiBehavior<MeterReadingDto>
{
    public async Task OnPropertyOrFillChanged(CancellationToken cancellationToken)
    {
        if (Record.PropertyId == Guid.Empty) return;
        await FillLinesAsync(cancellationToken);
    }

    public async Task OnFillClicked(CancellationToken cancellationToken)
    {
        if (Record.PropertyId == Guid.Empty) return;
        await FillLinesAsync(cancellationToken);
    }

    private async Task FillLinesAsync(CancellationToken cancellationToken)
    {
        var response = await fillClient.HandleAsync(new MeterReadingFillRequestDto
        {
            PropertyId = Record.PropertyId,
            Date = Record.Date,
        }, cancellationToken);

        Record.Lines.Clear();
        foreach (var line in response.Lines)
        {
            Record.Lines.Add(new MeterReadingLineDto
            {
                Id = Guid.NewGuid(),
                MeterId = line.MeterId,
                Zone = line.Zone,
                PreviousReading = line.PreviousReading,
                Reading = line.PreviousReading,
            });
        }
    }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("MeterReadingPrintTitle")]
public sealed class MeterReadingPrintForm : DocumentPrintForm
{
    [Caption("Property")]
    public string PropertyName { get; set; } = string.Empty;

    [Required]
    [PrintTable(LineNumbers = true)]
    public required ICollection<MeterReadingPrintLine> Lines { get; set; }
}

public sealed class MeterReadingPrintLine
{
    [PrintColumn] [Caption("Meter")] public string MeterName { get; set; } = string.Empty;
    [PrintColumn] [Caption("Zone")] public TariffZone Zone { get; set; }
    [PrintColumn] [Caption("PreviousReading")] [Format(PrintFormats.Quantity)] public decimal PreviousReading { get; set; }
    [PrintColumn] [Caption("Reading")] [Format(PrintFormats.Quantity)] public decimal Reading { get; set; }
    [PrintColumn] [Caption("Difference")] [Format(PrintFormats.Quantity)] public decimal Difference => Reading - PreviousReading;
}
