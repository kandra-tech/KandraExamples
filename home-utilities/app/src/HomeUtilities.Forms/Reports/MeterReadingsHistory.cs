using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.DtoCommon.Printing;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Forms.Reports;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Dictionaries;

namespace HomeUtilities.Forms.Reports;

public sealed class MeterReadingsHistoryReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [Order(1)]
    [PrintColumn]
    [Caption("MeterReadingsHistory_Date")]
    [DateEditor(DateEditorKind.Date)]
    [ReportViewColumn]
    public DateTime Date { get; set; }

    [Required]
    [Order(2)]
    [PrintColumn]
    [Caption("MeterReadingsHistory_Meter")]
    [ReportViewColumn(Groupable = true)]
    public string MeterName { get; set; } = string.Empty;

    [Required]
    [Order(3)]
    [PrintColumn]
    [Caption("MeterReadingsHistory_Zone")]
    [ReportViewColumn]
    public TariffZone Zone { get; set; }

    [Required]
    [Order(4)]
    [PrintColumn]
    [Caption("MeterReadingsHistory_Reading")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Reading { get; set; }

    [Required]
    [Order(5)]
    [PrintColumn]
    [Caption("MeterReadingsHistory_Unit")]
    [ReportViewColumn]
    public string Unit { get; set; } = string.Empty;
}

[KandraReportForm(Name = "MeterReadingsHistoryReport", ResultDtoType = typeof(MeterReadingsHistoryReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(MeterReadingsHistoryReportDataDto))]
[ExecuteFormTitle("MeterReadingsHistoryReport")]
[Description("Chronological history of recorded meter readings by property and meter.")]
public sealed class ReportMeterReadingsHistoryDto : IReportFiltersDto
{
    [Required]
    [Caption("MeterReadingsHistory_Property")]
    [Search(typeof(PropertyDto))]
    [Description("The household property.")]
    public Guid PropertyId { get; set; }

    [Caption("MeterReadingsHistory_Meter")]
    [Search(typeof(MeterDto))]
    [Description("Optional filter by specific meter.")]
    public Guid? MeterId { get; set; }

    [Caption("DateRange")]
    [DateRangeEditor]
    [Description("Optional date range filter.")]
    public DateRangeDto? DateRange { get; set; }
}

[PrintForm]
[PrintTitle("MeterReadingsHistoryReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Portrait, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class MeterReadingsHistoryReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("MeterReadingsHistory_Property")]
    public string? PropertyName { get; set; }

    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<MeterReadingsHistoryReportLineDto> Lines { get; set; }
}

public sealed class MeterReadingsHistoryReportResult : ReportResult<ReportMeterReadingsHistoryDto, MeterReadingsHistoryReportDataDto>;

public class ReportMeterReadingsHistoryDtoValidator : AbstractValidator<ReportMeterReadingsHistoryDto>
{
    public ReportMeterReadingsHistoryDtoValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
    }
}
