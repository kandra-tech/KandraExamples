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
using Kandra.Forms.Querying;
using Kandra.Forms.Reports;

namespace FamilyGarage.Forms.Reports;

public sealed class CostsPivotReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Order(1)]
    [PrintColumn]
    [Caption("Vehicle")]
    [ReportViewColumn]
    [PivotRow(CaptionProperty = nameof(VehicleName))]
    public string VehicleCode { get; set; } = string.Empty;

    public string VehicleName { get; set; } = string.Empty;

    [Order(2)]
    [PrintColumn]
    [Caption("Month")]
    [ReportViewColumn]
    [PivotColumn]
    public string Month { get; set; } = string.Empty;

    [Order(3)]
    [PrintColumn]
    [Caption("Amount")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [PivotValue]
    public decimal Amount { get; set; }
}

[KandraReportForm(Name = "CostsPivotReport", ResultDtoType = typeof(CostsPivotReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(CostsPivotReportDataDto))]
[ExecuteFormTitle("CostsPivotReport")]
[Description("Pivot table showing vehicle costs by month across a chosen period.")]
public sealed class ReportCostsPivotDto : IReportFiltersDto
{
    [DateRangeEditor]
    public DateRangeDto? Period { get; set; }
}

[PrintForm]
[PrintTitle("CostsPivotReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape)]
public sealed class CostsPivotReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable]
    [ReportViewGrid]
    [PivotTable]
    public required ICollection<CostsPivotReportLineDto> Lines { get; set; }
}

public sealed class CostsPivotReportResult : ReportResult<ReportCostsPivotDto, CostsPivotReportDataDto>;

public class ReportCostsPivotDtoValidator : AbstractValidator<ReportCostsPivotDto>
{
    public ReportCostsPivotDtoValidator()
    {
        RuleFor(x => x.Period)
            .Must(r => r is null || r.From is null || r.To is null || r.From.Value <= r.To.Value)
            .WithName(nameof(ReportCostsPivotDto.Period));
    }
}
