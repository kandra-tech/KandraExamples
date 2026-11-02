using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.DtoCommon.Printing;
using Kandra.Forms.Printable;
using Kandra.Forms.Reports;
using HomePantry.Enums;

namespace HomePantry.Forms.Reports;

public sealed class ChoresReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid ChoreId { get; set; }
    [Required]
    public string ChoreCode { get; set; } = "";
    [Required]
    public string ChoreName { get; set; } = "";

    [PrintColumn(WidthPercent = 30)]
    [Caption("Chore")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string ChoreText => CodeNameRef.Combine(ChoreCode, ChoreName);

    [PrintColumn(WidthPercent = 15)]
    [Caption("Recurrence")]
    [ReportViewColumn(Groupable = true)]
    public ChoreRecurrence Recurrence { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("LastDoneDate")]
    [Format("d")]
    [ReportViewColumn]
    public DateTime? LastDoneDate { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("DoneBy")]
    [ReportViewColumn(Groupable = true)]
    public string? DoneByName { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("NextDueDate")]
    [Format("d")]
    [ReportViewColumn]
    public DateTime NextDueDate { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("IsOverdue")]
    [ReportViewColumn]
    public bool IsOverdue { get; set; }
}

[KandraReportForm(Name = "ChoresReport", ResultDtoType = typeof(ChoresReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(ChoresReportDataDto))]
[ExecuteFormTitle("ChoresReport")]
[Description("Lists each household chore, the last time it was done, by whom, the next due date and overdue status.")]
public sealed class ReportChoresDto : IReportFiltersDto
{
}

[PrintForm]
[PrintTitle("ChoresReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class ChoresReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable(LineNumbers = true)]
    [ReportViewGrid]
    public required ICollection<ChoresReportLineDto> Lines { get; set; }
}

public sealed class ChoresReportResult : ReportResult<ReportChoresDto, ChoresReportDataDto>;

public class ReportChoresDtoValidator : AbstractValidator<ReportChoresDto>
{
}
