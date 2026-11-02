using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.DtoCommon.Printing;
using Kandra.Forms.Printable;
using Kandra.Forms.Reports;
using HomePantry.Forms.Dictionaries;

namespace HomePantry.Forms.Reports;

public sealed class StockOverviewReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid ProductId { get; set; }
    [Required]
    public string ProductCode { get; set; } = "";
    [Required]
    public string ProductName { get; set; } = "";

    [PrintColumn(WidthPercent = 25)]
    [Caption("Product")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string ProductText => CodeNameRef.Combine(ProductCode, ProductName);

    [PrintColumn(WidthPercent = 10)]
    [Caption("Unit")]
    [ReportViewColumn]
    public string Unit { get; set; } = "";

    [PrintColumn(WidthPercent = 15)]
    [Caption("Quantity")]
    [Format(PrintFormats.Quantity)]
    [ReportViewColumn]
    public decimal Quantity { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("Value")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Value { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("NearestBestBeforeDate")]
    [Format("d")]
    [ReportViewColumn]
    public DateTime? NearestBestBeforeDate { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("MinimumStock")]
    [Format(PrintFormats.Quantity)]
    [ReportViewColumn]
    public decimal MinimumStock { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("BelowMinimum")]
    [ReportViewColumn]
    public bool BelowMinimum { get; set; }
}

[KandraReportForm(Name = "StockOverviewReport", ResultDtoType = typeof(StockOverviewReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(StockOverviewReportDataDto))]
[ExecuteFormTitle("StockOverviewReport")]
[Description("Shows current quantity on hand, value, nearest best-before date, and below-minimum stock flags, filterable by location and product group.")]
public sealed class ReportStockOverviewDto : IReportFiltersDto
{
    [Caption("Location")]
    [Search(typeof(LocationNodeDto), NodeSelection = HierarchicalNodeSelection.Any)]
    [Description("Filter stock balances by a specific location or location folder (optional).")]
    public Guid? LocationId { get; set; }

    [Caption("ProductGroup")]
    [Search(typeof(ProductNodeDto), NodeSelection = HierarchicalNodeSelection.Folders)]
    [Description("Filter products by group folder (optional).")]
    public Guid? ProductGroupId { get; set; }
}

[PrintForm]
[PrintTitle("StockOverviewReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class StockOverviewReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("Location")]
    public string? LocationName { get; set; }

    [PrintRole(PrintRole.Parameter)]
    [Caption("ProductGroup")]
    public string? ProductGroupName { get; set; }

    [Required]
    [PrintTable(LineNumbers = true)]
    [ReportViewGrid]
    public required ICollection<StockOverviewReportLineDto> Lines { get; set; }
}

public sealed class StockOverviewReportResult : ReportResult<ReportStockOverviewDto, StockOverviewReportDataDto>;

public class ReportStockOverviewDtoValidator : AbstractValidator<ReportStockOverviewDto>
{
}
