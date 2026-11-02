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
using Kandra.Forms.Querying;
using Kandra.Forms.Reports;

namespace HomePantry.Forms.Reports;

public sealed class SpendingReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid ProductGroupId { get; set; }
    [Required]
    public string ProductGroupCode { get; set; } = "";
    [Required]
    public string ProductGroupName { get; set; } = "";

    [PrintColumn(WidthPercent = 40)]
    [Caption("ProductGroup")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string ProductGroupText => CodeNameRef.Combine(ProductGroupCode, ProductGroupName);

    [Required]
    public Guid StoreId { get; set; }
    [Required]
    public string StoreCode { get; set; } = "";
    [Required]
    public string StoreName { get; set; } = "";

    [PrintColumn(WidthPercent = 35)]
    [Caption("Store")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string StoreText => CodeNameRef.Combine(StoreCode, StoreName);

    [PrintColumn(WidthPercent = 25)]
    [Caption("PurchasedAmount")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal PurchasedAmount { get; set; }
}

[KandraReportForm(Name = "SpendingReport", ResultDtoType = typeof(SpendingReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(SpendingReportDataDto))]
[ExecuteFormTitle("SpendingReport")]
[Description("Shows purchased monetary amounts by product group and store over a selected date period.")]
public sealed class ReportSpendingDto : IReportFiltersDto
{
    [Caption("Period")]
    [DateRangeEditor]
    [Description("Date range of purchase documents.")]
    public DateRangeDto? DateRange { get; set; }
}

[PrintForm]
[PrintTitle("SpendingReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class SpendingReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("Period")]
    public string? DateRangeText { get; set; }

    [Required]
    [PrintTable(LineNumbers = true)]
    [ReportViewGrid]
    public required ICollection<SpendingReportLineDto> Lines { get; set; }
}

public sealed class SpendingReportResult : ReportResult<ReportSpendingDto, SpendingReportDataDto>;

public class ReportSpendingDtoValidator : AbstractValidator<ReportSpendingDto>
{
    public ReportSpendingDtoValidator()
    {
        RuleFor(x => x.DateRange)
            .Must(r => r == null || r.From == null || r.To == null || r.From.Value <= r.To.Value)
            .WithName(nameof(ReportSpendingDto.DateRange));
    }
}
