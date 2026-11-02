using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Forms.Printable;
using Kandra.Forms.Reports;

namespace PersonalAccounting.Forms.Reports;

public sealed class ExchangeRatesReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [Order(1)]
    [PrintColumn]
    [Caption("CurrencyCode")]
    [ReportViewColumn]
    public string CurrencyCode { get; set; } = "";

    [Required]
    [Order(2)]
    [PrintColumn]
    [Caption("CurrencyName")]
    [ReportViewColumn]
    public string CurrencyName { get; set; } = "";

    [Required]
    [Order(3)]
    [PrintColumn]
    [Caption("Rate")]
    [Format("N4")]
    [ReportViewColumn]
    public decimal Rate { get; set; }

    [Required]
    [Order(4)]
    [PrintColumn]
    [Caption("ValidFrom")]
    [Format("yyyy-MM-dd")]
    [ReportViewColumn]
    public DateTime ValidFrom { get; set; }
}

[KandraReportForm(Name = "ExchangeRatesReport", ResultDtoType = typeof(ExchangeRatesReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(ExchangeRatesReportDataDto))]
[ExecuteFormTitle("ExchangeRatesReport")]
[Description("Displays exchange rates valid as of a selected date.")]
public sealed class ReportExchangeRatesDto : IReportFiltersDto
{
    [Caption("DateAsOf")]
    [DateEditor(DateEditorKind.Date)]
    [Description("Date to query exchange rates as of.")]
    public DateTime? DateAsOf { get; set; }
}

[PrintForm]
[PrintTitle("ExchangeRatesReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = [15, 10, 15, 10])]
public sealed class ExchangeRatesReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<ExchangeRatesReportLineDto> Lines { get; set; }
}

public sealed class ExchangeRatesReportResult : ReportResult<ReportExchangeRatesDto, ExchangeRatesReportDataDto>;

public class ReportExchangeRatesDtoValidator : AbstractValidator<ReportExchangeRatesDto>
{
    public ReportExchangeRatesDtoValidator()
    {
    }
}
