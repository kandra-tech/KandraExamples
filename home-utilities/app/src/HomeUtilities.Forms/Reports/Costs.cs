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

namespace HomeUtilities.Forms.Reports;

public sealed class CostsReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [Order(1)]
    [PrintColumn]
    [Caption("Costs_Property")]
    [ReportViewColumn(Groupable = true)]
    public string PropertyName { get; set; } = string.Empty;

    [Required]
    [Order(2)]
    [PrintColumn]
    [Caption("Costs_ServiceGroup")]
    [ReportViewColumn(Groupable = true, DefaultGroupOrder = 1)]
    public string ServiceGroupName { get; set; } = string.Empty;

    [Required]
    [Order(3)]
    [PrintColumn]
    [Caption("Costs_Service")]
    [ReportViewColumn]
    public string ServiceName { get; set; } = string.Empty;

    [Required]
    [Order(4)]
    [PrintColumn]
    [Caption("Costs_Amount")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Amount { get; set; }
}

[KandraReportForm(Name = "CostsReport", ResultDtoType = typeof(CostsReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(CostsReportDataDto))]
[ExecuteFormTitle("CostsReport")]
[Description("Utility costs per property and service, grouped by top-level service group, with totals.")]
public sealed class ReportCostsDto : IReportFiltersDto
{
    [Caption("DateRange")]
    [DateRangeEditor]
    [Description("The period for costs analysis.")]
    public DateRangeDto? DateRange { get; set; }
}

[PrintForm]
[PrintTitle("CostsReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Portrait, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class CostsReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<CostsReportLineDto> Lines { get; set; }
}

public sealed class CostsReportResult : ReportResult<ReportCostsDto, CostsReportDataDto>;

public class ReportCostsDtoValidator : AbstractValidator<ReportCostsDto>
{
    public ReportCostsDtoValidator()
    {
    }
}
