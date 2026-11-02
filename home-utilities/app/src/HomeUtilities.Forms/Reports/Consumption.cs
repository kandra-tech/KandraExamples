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

public sealed class ConsumptionReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [Order(1)]
    [PrintColumn]
    [Caption("Consumption_Service")]
    [ReportViewColumn(Groupable = true)]
    public string ServiceName { get; set; } = string.Empty;

    [Required]
    [Order(2)]
    [PrintColumn]
    [Caption("Zone")]
    [ReportViewColumn]
    public TariffZone Zone { get; set; }

    [Order(3)]
    [PrintColumn]
    [Caption("Month_01")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month01 { get; set; }

    [Order(4)]
    [PrintColumn]
    [Caption("Month_02")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month02 { get; set; }

    [Order(5)]
    [PrintColumn]
    [Caption("Month_03")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month03 { get; set; }

    [Order(6)]
    [PrintColumn]
    [Caption("Month_04")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month04 { get; set; }

    [Order(7)]
    [PrintColumn]
    [Caption("Month_05")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month05 { get; set; }

    [Order(8)]
    [PrintColumn]
    [Caption("Month_06")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month06 { get; set; }

    [Order(9)]
    [PrintColumn]
    [Caption("Month_07")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month07 { get; set; }

    [Order(10)]
    [PrintColumn]
    [Caption("Month_08")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month08 { get; set; }

    [Order(11)]
    [PrintColumn]
    [Caption("Month_09")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month09 { get; set; }

    [Order(12)]
    [PrintColumn]
    [Caption("Month_10")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month10 { get; set; }

    [Order(13)]
    [PrintColumn]
    [Caption("Month_11")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month11 { get; set; }

    [Order(14)]
    [PrintColumn]
    [Caption("Month_12")]
    [Format("N3")]
    [ReportViewColumn]
    public decimal Month12 { get; set; }

    [Required]
    [Order(15)]
    [PrintColumn]
    [Caption("Consumption_Total")]
    [Format("N3")]
    [ReportViewColumn]
    [Aggregate(CustomAggregateType = typeof(ConsumptionQuantityAggregate))]
    public decimal Total { get; set; }

    [Required]
    [Order(16)]
    [PrintColumn]
    [Caption("Consumption_PreviousYearTotal")]
    [Format("N3")]
    [ReportViewColumn]
    [Aggregate(CustomAggregateType = typeof(ConsumptionPrevYearQuantityAggregate))]
    public decimal PreviousYearTotal { get; set; }

    [Required]
    [Order(17)]
    [PrintColumn]
    [Caption("Consumption_ChangePercent")]
    [Format("N2")]
    [ReportViewColumn]
    public decimal ChangeYoY { get; set; }
}

public sealed class ConsumptionQuantityAggregate : ICustomAggregate
{
    public static string? Calculate<T>(IEnumerable<T> items, ReportGroupingState groupingState, string? format)
    {
        if (groupingState.FrontGroup != nameof(ConsumptionReportLineDto.ServiceName))
            return string.Empty;

        var total = items.OfType<ConsumptionReportLineDto>().Sum(l => l.Total);
        return format is not null ? total.ToString(format) : total.ToString("N3");
    }
}

public sealed class ConsumptionPrevYearQuantityAggregate : ICustomAggregate
{
    public static string? Calculate<T>(IEnumerable<T> items, ReportGroupingState groupingState, string? format)
    {
        if (groupingState.FrontGroup != nameof(ConsumptionReportLineDto.ServiceName))
            return string.Empty;

        var total = items.OfType<ConsumptionReportLineDto>().Sum(l => l.PreviousYearTotal);
        return format is not null ? total.ToString(format) : total.ToString("N3");
    }
}

[KandraReportForm(Name = "ConsumptionReport", ResultDtoType = typeof(ConsumptionReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(ConsumptionReportDataDto))]
[ExecuteFormTitle("ConsumptionReport")]
[Description("Monthly utility consumption by service for a property across a year, with totals and comparison to previous year.")]
public sealed class ReportConsumptionDto : IReportFiltersDto
{
    [Required]
    [Caption("Consumption_Property")]
    [Search(typeof(PropertyDto))]
    [Description("The household property.")]
    public Guid PropertyId { get; set; }

    [Required]
    [Caption("Consumption_Year")]
    [Description("The billing year.")]
    public int Year { get; set; } = DateTime.Today.Year;
}

[PrintForm]
[PrintTitle("ConsumptionReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class ConsumptionReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("Consumption_Property")]
    public string? PropertyName { get; set; }

    [PrintRole(PrintRole.Parameter)]
    [Caption("Consumption_Year")]
    public int Year { get; set; }

    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<ConsumptionReportLineDto> Lines { get; set; }
}

public sealed class ConsumptionReportResult : ReportResult<ReportConsumptionDto, ConsumptionReportDataDto>;

public class ReportConsumptionDtoValidator : AbstractValidator<ReportConsumptionDto>
{
    public ReportConsumptionDtoValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.Year).GreaterThan(2000).LessThanOrEqualTo(2100);
    }
}
