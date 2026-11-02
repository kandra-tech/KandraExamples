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

namespace PersonalAccounting.Forms.Reports;

public sealed class BudgetVsActualReportLineDto
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
    [Caption("PlannedAmount")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal PlannedAmount { get; set; }

    [Required]
    [Order(3)]
    [PrintColumn]
    [Caption("ActualAmount")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal ActualAmount { get; set; }

    [Required]
    [Order(4)]
    [PrintColumn]
    [Caption("RemainingAmount")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal RemainingAmount { get; set; }

    [Required]
    [Order(5)]
    [PrintColumn]
    [Caption("PercentUsed")]
    [Format("N1")]
    [ReportViewColumn]
    public decimal PercentUsed { get; set; }

    [Required]
    [Order(6)]
    [PrintColumn]
    [Caption("IsOverspent")]
    [ReportViewColumn]
    public bool IsOverspent { get; set; }
}

[KandraReportForm(Name = "BudgetVsActualReport", ResultDtoType = typeof(BudgetVsActualReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(BudgetVsActualReportDataDto))]
[ExecuteFormTitle("BudgetVsActualReport")]
[Description("Compares planned budget expenses with actual category turnovers for a chosen month.")]
public sealed class ReportBudgetVsActualDto : IReportFiltersDto
{
    [Required]
    [Caption("Month")]
    [DateEditor(DateEditorKind.Date)]
    [Description("Month to inspect.")]
    public DateTime Month { get; set; }
}

[PrintForm]
[PrintTitle("BudgetVsActualReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = [15, 10, 15, 10])]
public sealed class BudgetVsActualReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<BudgetVsActualReportLineDto> Lines { get; set; }
}

public sealed class BudgetVsActualReportResult : ReportResult<ReportBudgetVsActualDto, BudgetVsActualReportDataDto>;

public class ReportBudgetVsActualDtoValidator : AbstractValidator<ReportBudgetVsActualDto>
{
    public ReportBudgetVsActualDtoValidator()
    {
        RuleFor(x => x.Month).NotEmpty();
    }
}
