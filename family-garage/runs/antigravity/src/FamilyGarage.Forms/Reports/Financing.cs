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

namespace FamilyGarage.Forms.Reports;

public sealed class FinancingReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Order(1)]
    [PrintColumn]
    [Caption("ContractNumber")]
    [ReportViewColumn]
    public string ContractNumber { get; set; } = string.Empty;

    [Order(2)]
    [PrintColumn]
    [Caption("Vehicle")]
    [ReportViewColumn(Groupable = true)]
    public string Vehicle { get; set; } = string.Empty;

    [Order(3)]
    [PrintColumn]
    [Caption("Lender")]
    [ReportViewColumn(Groupable = true)]
    public string Lender { get; set; } = string.Empty;

    [Order(4)]
    [PrintColumn]
    [Caption("FinancedAmount")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal FinancedAmount { get; set; }

    [Order(5)]
    [PrintColumn]
    [Caption("PrincipalPaid")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal PrincipalPaid { get; set; }

    [Order(6)]
    [PrintColumn]
    [Caption("InterestAndFeesPaid")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal InterestAndFeesPaid { get; set; }

    [Order(7)]
    [PrintColumn]
    [Caption("DebtOwed")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal DebtOwed { get; set; }

    [Order(8)]
    [PrintColumn]
    [Caption("PaymentsMade")]
    [Format("N0")]
    [ReportViewColumn]
    public int PaymentsMade { get; set; }

    [Order(9)]
    [PrintColumn]
    [Caption("MonthsLeft")]
    [Format("N0")]
    [ReportViewColumn]
    public int MonthsLeft { get; set; }
}

[KandraReportForm(Name = "FinancingReport", ResultDtoType = typeof(FinancingReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(FinancingReportDataDto))]
[ExecuteFormTitle("FinancingReport")]
[Description("Reports the state of all finance contracts as of a date: financed amount, principal paid, interest/fees paid, debt owed, payments made, and months left.")]
public sealed class ReportFinancingDto : IReportFiltersDto
{
    [Caption("AsOf")]
    [DateEditor(DateEditorKind.Date)]
    [Description("As of date for evaluating loan/lease balances.")]
    public DateTime AsOf { get; set; } = DateTime.Today;
}

[PrintForm]
[PrintTitle("FinancingReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape)]
public sealed class FinancingReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<FinancingReportLineDto> Lines { get; set; }
}

public sealed class FinancingReportResult : ReportResult<ReportFinancingDto, FinancingReportDataDto>;

public class ReportFinancingDtoValidator : AbstractValidator<ReportFinancingDto>
{
    public ReportFinancingDtoValidator()
    {
    }
}
