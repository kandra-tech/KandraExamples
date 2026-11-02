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
using Kandra.Forms.Reports;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Dictionaries;

namespace HomeUtilities.Forms.Reports;

public sealed class TariffHistoryReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [Order(1)]
    [PrintColumn]
    [Caption("TariffHistory_Date")]
    [DateEditor(DateEditorKind.Date)]
    [ReportViewColumn]
    public DateTime Date { get; set; }

    [Required]
    [Order(2)]
    [PrintColumn]
    [Caption("TariffHistory_Zone")]
    [ReportViewColumn]
    public TariffZone Zone { get; set; }

    [Required]
    [Order(3)]
    [PrintColumn]
    [Caption("TariffHistory_Price")]
    [Format("N4")]
    [ReportViewColumn]
    public decimal Price { get; set; }

    [Order(4)]
    [PrintColumn]
    [Caption("TariffHistory_ChangePercent")]
    [Format("N2")]
    [ReportViewColumn]
    public decimal? ChangePercent { get; set; }
}

[KandraReportForm(Name = "TariffHistoryReport", ResultDtoType = typeof(TariffHistoryReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(TariffHistoryReportDataDto))]
[ExecuteFormTitle("TariffHistoryReport")]
[Description("Historical tariff rates and percentage changes for a chosen utility service.")]
public sealed class ReportTariffHistoryDto : IReportFiltersDto
{
    [Required]
    [Caption("TariffHistory_Service")]
    [Search(typeof(ServiceNodeDto))]
    [Description("The utility service to show tariff history for.")]
    public Guid ServiceId { get; set; }
}

[PrintForm]
[PrintTitle("TariffHistoryReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Portrait, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class TariffHistoryReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("TariffHistory_Service")]
    public string? ServiceName { get; set; }

    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<TariffHistoryReportLineDto> Lines { get; set; }
}

public sealed class TariffHistoryReportResult : ReportResult<ReportTariffHistoryDto, TariffHistoryReportDataDto>;

public class ReportTariffHistoryDtoValidator : AbstractValidator<ReportTariffHistoryDto>
{
    public ReportTariffHistoryDtoValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
    }
}
