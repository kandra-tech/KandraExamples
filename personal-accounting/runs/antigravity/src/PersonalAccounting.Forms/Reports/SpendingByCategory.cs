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
using PersonalAccounting.Forms.Dictionaries;

namespace PersonalAccounting.Forms.Reports;

public sealed class SpendingByCategoryReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [Order(1)]
    [PrintColumn]
    [Caption("Category")]
    [ReportViewColumn(Groupable = true)]
    public string CategoryName { get; set; } = "";

    [Required]
    [Order(2)]
    [PrintColumn]
    [Caption("Account")]
    [ReportViewColumn(Groupable = true)]
    public string AccountName { get; set; } = "";

    [Required]
    [Order(3)]
    [PrintColumn]
    [Caption("Amount")]
    [Format("N2")]
    [ReportViewColumn]
    public decimal Amount { get; set; }

    [Required]
    [Order(4)]
    [PrintColumn]
    [Caption("Currency")]
    [ReportViewColumn]
    public string CurrencyCode { get; set; } = "";

    [Required]
    [Order(5)]
    [PrintColumn]
    [Caption("AmountInBase")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal AmountInBase { get; set; }
}

[KandraReportForm(Name = "SpendingByCategoryReport", ResultDtoType = typeof(SpendingByCategoryReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(SpendingByCategoryReportDataDto))]
[ExecuteFormTitle("SpendingByCategoryReport")]
[Description("Reports spending by category and account over a chosen date range.")]
public sealed class ReportSpendingByCategoryDto : IReportFiltersDto
{
    [Caption("DateRange")]
    [DateRangeEditor]
    [Description("Period for spending analysis.")]
    public DateRangeDto? DateRange { get; set; }

    [Caption("Categories")]
    [MultiSelect(typeof(CategoryNodeDto))]
    [ExtendedFilter]
    [Description("Filter by categories.")]
    public IReadOnlyList<Guid>? CategoryIds { get; set; }
}

[PrintForm]
[PrintTitle("SpendingByCategoryReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = [15, 10, 15, 10])]
public sealed class SpendingByCategoryReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<SpendingByCategoryReportLineDto> Lines { get; set; }
}

public sealed class SpendingByCategoryReportResult : ReportResult<ReportSpendingByCategoryDto, SpendingByCategoryReportDataDto>;

public class ReportSpendingByCategoryDtoValidator : AbstractValidator<ReportSpendingByCategoryDto>
{
    public ReportSpendingByCategoryDtoValidator()
    {
    }
}
