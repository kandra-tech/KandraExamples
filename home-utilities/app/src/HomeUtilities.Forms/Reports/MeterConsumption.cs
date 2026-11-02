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

public sealed class MeterConsumptionReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [Order(1)]
    [PrintColumn]
    [Caption("MeterConsumption_Meter")]
    [ReportViewColumn]
    public string MeterName { get; set; } = string.Empty;

    [Required]
    [Order(2)]
    [PrintColumn]
    [Caption("MeterConsumption_Zone")]
    [ReportViewColumn]
    public TariffZone Zone { get; set; }

    [Required]
    [Order(3)]
    [PrintColumn]
    [Caption("MeterConsumption_Unit")]
    [ReportViewColumn]
    public string Unit { get; set; } = string.Empty;

    [Order(4)]
    [PrintColumn]
    [Caption("Month_01")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month01 { get; set; }

    [Order(5)]
    [PrintColumn]
    [Caption("Month_02")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month02 { get; set; }

    [Order(6)]
    [PrintColumn]
    [Caption("Month_03")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month03 { get; set; }

    [Order(7)]
    [PrintColumn]
    [Caption("Month_04")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month04 { get; set; }

    [Order(8)]
    [PrintColumn]
    [Caption("Month_05")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month05 { get; set; }

    [Order(9)]
    [PrintColumn]
    [Caption("Month_06")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month06 { get; set; }

    [Order(10)]
    [PrintColumn]
    [Caption("Month_07")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month07 { get; set; }

    [Order(11)]
    [PrintColumn]
    [Caption("Month_08")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month08 { get; set; }

    [Order(12)]
    [PrintColumn]
    [Caption("Month_09")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month09 { get; set; }

    [Order(13)]
    [PrintColumn]
    [Caption("Month_10")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month10 { get; set; }

    [Order(14)]
    [PrintColumn]
    [Caption("Month_11")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month11 { get; set; }

    [Order(15)]
    [PrintColumn]
    [Caption("Month_12")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month12 { get; set; }

    [Required]
    [Order(16)]
    [PrintColumn]
    [Caption("Consumption_Total")]
    [Format("N3")]
    [ReportViewColumn]
    [Aggregate(CustomAggregateType = typeof(MeterConsumptionQuantityAggregate))]
    public decimal Total { get; set; }
}

public sealed class MeterConsumptionQuantityAggregate : ICustomAggregate
{
    public static string? Calculate<T>(IEnumerable<T> items, ReportGroupingState groupingState, string? format)
    {
        if (groupingState.FrontGroup != nameof(MeterConsumptionReportLineDto.MeterName))
            return string.Empty;

        var total = items.OfType<MeterConsumptionReportLineDto>().Sum(l => l.Total);
        return format is not null ? total.ToString(format) : total.ToString("N3");
    }
}

[KandraReportForm(Name = "MeterConsumptionReport", ResultDtoType = typeof(MeterConsumptionReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(MeterConsumptionReportDataDto))]
[ExecuteFormTitle("MeterConsumptionReport")]
[Description("Monthly consumption per utility meter and zone across a year, with totals.")]
public sealed class ReportMeterConsumptionDto : IReportFiltersDto
{
    [Required]
    [Caption("MeterConsumption_Property")]
    [Search(typeof(PropertyDto))]
    [Description("The household property.")]
    public Guid PropertyId { get; set; }

    [Required]
    [Caption("MeterConsumption_Year")]
    [Description("The billing year.")]
    public int Year { get; set; } = DateTime.Today.Year;
}

[PrintForm]
[PrintTitle("MeterConsumptionReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class MeterConsumptionReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("MeterConsumption_Property")]
    public string? PropertyName { get; set; }

    [PrintRole(PrintRole.Parameter)]
    [Caption("MeterConsumption_Year")]
    public int Year { get; set; }

    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<MeterConsumptionReportLineDto> Lines { get; set; }
}

public sealed class MeterConsumptionReportResult : ReportResult<ReportMeterConsumptionDto, MeterConsumptionReportDataDto>;

public class ReportMeterConsumptionDtoValidator : AbstractValidator<ReportMeterConsumptionDto>
{
    public ReportMeterConsumptionDtoValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
    }
}
