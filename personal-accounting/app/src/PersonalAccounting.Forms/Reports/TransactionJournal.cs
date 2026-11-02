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

public sealed class TransactionJournalReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [Order(1)]
    [PrintColumn]
    [Caption("Date")]
    [Format("yyyy-MM-dd HH:mm")]
    [ReportViewColumn]
    public DateTime Date { get; set; }

    [Order(2)]
    [PrintColumn]
    [Caption("DocumentCode")]
    [ReportViewColumn]
    public string DocumentCode { get; set; } = "";

    [Order(3)]
    [PrintColumn]
    [Caption("DocumentType")]
    [ReportViewColumn]
    public string DocumentType { get; set; } = "";

    [Required]
    [Order(4)]
    [PrintColumn]
    [Caption("Account")]
    [ReportViewColumn(Groupable = true)]
    public string AccountName { get; set; } = "";

    [Order(5)]
    [PrintColumn]
    [Caption("Category")]
    [ReportViewColumn(Groupable = true)]
    public string? CategoryName { get; set; }

    [Order(6)]
    [PrintColumn]
    [Caption("Inflow")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal? Inflow { get; set; }

    [Order(7)]
    [PrintColumn]
    [Caption("Outflow")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal? Outflow { get; set; }

    [Order(8)]
    [PrintColumn]
    [Caption("Comment")]
    [ReportViewColumn]
    public string? Comment { get; set; }
}

[KandraReportForm(Name = "TransactionJournalReport", ResultDtoType = typeof(TransactionJournalReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(TransactionJournalReportDataDto))]
[ExecuteFormTitle("TransactionJournalReport")]
[Description("Lists all posted account balance movements within a date range with optional account and category filters.")]
public sealed class ReportTransactionJournalDto : IReportFiltersDto
{
    [Caption("DateRange")]
    [DateRangeEditor]
    [Description("Date range for operations.")]
    public DateRangeDto? DateRange { get; set; }

    [Caption("Accounts")]
    [MultiSelect(typeof(AccountDto))]
    [ExtendedFilter]
    [Description("Filter by accounts.")]
    public IReadOnlyList<Guid>? AccountIds { get; set; }

    [Caption("Categories")]
    [MultiSelect(typeof(CategoryNodeDto))]
    [ExtendedFilter]
    [Description("Filter by categories.")]
    public IReadOnlyList<Guid>? CategoryIds { get; set; }
}

[PrintForm]
[PrintTitle("TransactionJournalReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = [15, 10, 15, 10])]
public sealed class TransactionJournalReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<TransactionJournalReportLineDto> Lines { get; set; }
}

public sealed class TransactionJournalReportResult : ReportResult<ReportTransactionJournalDto, TransactionJournalReportDataDto>;

public class ReportTransactionJournalDtoValidator : AbstractValidator<ReportTransactionJournalDto>
{
    public ReportTransactionJournalDtoValidator()
    {
    }
}
