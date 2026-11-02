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

public sealed class AccountStatementReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Order(1)]
    [PrintColumn]
    [Caption("Date")]
    [Format("yyyy-MM-dd HH:mm")]
    [ReportViewColumn]
    public DateTime? Date { get; set; }

    [Order(2)]
    [PrintColumn]
    [Caption("DocumentCode")]
    [ReportViewColumn]
    public string? DocumentCode { get; set; }

    [Order(3)]
    [PrintColumn]
    [Caption("DocumentType")]
    [ReportViewColumn]
    public string? DocumentType { get; set; }

    [Required]
    [Order(4)]
    [PrintColumn]
    [Caption("Description")]
    [ReportViewColumn]
    public string Description { get; set; } = "";

    [Order(5)]
    [PrintColumn]
    [Caption("Inflow")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal? Inflow { get; set; }

    [Order(6)]
    [PrintColumn]
    [Caption("Outflow")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal? Outflow { get; set; }

    [Required]
    [Order(7)]
    [PrintColumn]
    [Caption("RunningBalance")]
    [Format("N2")]
    [ReportViewColumn]
    public decimal RunningBalance { get; set; }
}

[KandraReportForm(Name = "AccountStatementReport", ResultDtoType = typeof(AccountStatementReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(AccountStatementReportDataDto))]
[ExecuteFormTitle("AccountStatementReport")]
[Description("Shows opening balance, all period entries, and closing balance for a chosen account.")]
public sealed class ReportAccountStatementDto : IReportFiltersDto
{
    [Required]
    [Caption("Account")]
    [Dropdown(typeof(AccountDto))]
    [Description("Account to produce statement for.")]
    public Guid AccountId { get; set; }

    [Caption("DateRange")]
    [DateRangeEditor]
    [Description("Period for the statement.")]
    public DateRangeDto? DateRange { get; set; }
}

[PrintForm]
[PrintTitle("AccountStatementReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = [15, 10, 15, 10])]
public sealed class AccountStatementReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<AccountStatementReportLineDto> Lines { get; set; }
}

public sealed class AccountStatementReportResult : ReportResult<ReportAccountStatementDto, AccountStatementReportDataDto>;

public class ReportAccountStatementDtoValidator : AbstractValidator<ReportAccountStatementDto>
{
    public ReportAccountStatementDtoValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
    }
}
