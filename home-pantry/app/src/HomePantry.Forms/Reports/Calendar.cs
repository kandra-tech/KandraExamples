using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.DtoCommon.Printing;
using Kandra.Forms.Printable;
using Kandra.Forms.Reports;

namespace HomePantry.Forms.Reports;

public sealed class CalendarReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("Date")]
    [Format("d")]
    [ReportViewColumn(Groupable = true)]
    public DateTime Date { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("Calendar_DayOfWeek")]
    [ReportViewColumn(Groupable = true)]
    public string DayOfWeek { get; set; } = "";

    [PrintColumn(WidthPercent = 25)]
    [Caption("Calendar_ChoresDue")]
    [ReportViewColumn]
    public string ChoresDue { get; set; } = "";

    [PrintColumn(WidthPercent = 20)]
    [Caption("Calendar_BatteriesDue")]
    [ReportViewColumn]
    public string BatteriesDue { get; set; } = "";

    [PrintColumn(WidthPercent = 25)]
    [Caption("Calendar_PlannedMeals")]
    [ReportViewColumn]
    public string PlannedMeals { get; set; } = "";
}

[KandraReportForm(Name = "CalendarReport", ResultDtoType = typeof(CalendarReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(CalendarReportDataDto))]
[ExecuteFormTitle("CalendarReport")]
[Description("Displays household routines calendar for a chosen period with chores due, batteries due, and planned meals for each day.")]
public sealed class ReportCalendarDto : IReportFiltersDto
{
    [Caption("FromDate")]
    [DateEditor]
    public DateTime From { get; set; } = DateTime.UtcNow.Date;

    [Caption("ToDate")]
    [DateEditor]
    public DateTime To { get; set; } = DateTime.UtcNow.Date.AddDays(14);
}

[PrintForm]
[PrintTitle("CalendarReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class CalendarReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("FromDate")]
    [Format("d")]
    public DateTime From { get; set; }

    [PrintRole(PrintRole.Parameter)]
    [Caption("ToDate")]
    [Format("d")]
    public DateTime To { get; set; }

    [Required]
    [PrintTable(LineNumbers = true)]
    [ReportViewGrid]
    public required ICollection<CalendarReportLineDto> Lines { get; set; }
}

public sealed class CalendarReportResult : ReportResult<ReportCalendarDto, CalendarReportDataDto>;

public class ReportCalendarDtoValidator : AbstractValidator<ReportCalendarDto>
{
    public ReportCalendarDtoValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From);
    }
}
