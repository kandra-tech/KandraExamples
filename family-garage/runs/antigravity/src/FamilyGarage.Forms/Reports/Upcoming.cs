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
using FamilyGarage.Forms.Dictionaries;

namespace FamilyGarage.Forms.Reports;

public sealed class UpcomingReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Order(1)]
    [PrintColumn]
    [Caption("Vehicle")]
    [ReportViewColumn(Groupable = true)]
    public string Vehicle { get; set; } = string.Empty;

    [Order(2)]
    [PrintColumn]
    [Caption("Kind")]
    [ReportViewColumn(Groupable = true)]
    public string Kind { get; set; } = string.Empty;

    [Order(3)]
    [PrintColumn]
    [Caption("Description")]
    [ReportViewColumn]
    public string Description { get; set; } = string.Empty;

    [Order(4)]
    [PrintColumn]
    [Caption("DueDate")]
    [ReportViewColumn]
    [Format("d")]
    public DateTime? DueDate { get; set; }

    [Order(5)]
    [PrintColumn]
    [Caption("DueKm")]
    [Format("N0")]
    [ReportViewColumn]
    public int? DueKm { get; set; }

    [Order(6)]
    [PrintColumn]
    [Caption("RemainingDays")]
    [Format("N0")]
    [ReportViewColumn]
    public int? RemainingDays { get; set; }

    [Order(7)]
    [PrintColumn]
    [Caption("RemainingKm")]
    [Format("N0")]
    [ReportViewColumn]
    public int? RemainingKm { get; set; }

    [Order(8)]
    [PrintColumn]
    [Caption("IsOverdue")]
    [ReportViewColumn]
    public bool IsOverdue { get; set; }
}

[KandraReportForm(Name = "UpcomingReport", ResultDtoType = typeof(UpcomingReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(UpcomingReportDataDto))]
[ExecuteFormTitle("UpcomingReport")]
[Description("Shows every due or overdue service item, expiring policy, and upcoming finance payment within the look-ahead constants.")]
public sealed class ReportUpcomingDto : IReportFiltersDto
{
    [Caption("Vehicle")]
    [Dropdown(typeof(VehicleDto))]
    [Description("Optional vehicle filter; empty includes all vehicles.")]
    public Guid? VehicleId { get; set; }

    [Caption("LookAheadDays")]
    [Description("How many days ahead to look for due events.")]
    public int LookAheadDays { get; set; } = 30;

    [Caption("LookAheadKm")]
    [Description("How many kilometres ahead to look for due service items.")]
    public int LookAheadKm { get; set; } = 1000;
}

[PrintForm]
[PrintTitle("UpcomingReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape)]
public sealed class UpcomingReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<UpcomingReportLineDto> Lines { get; set; }
}

public sealed class UpcomingReportResult : ReportResult<ReportUpcomingDto, UpcomingReportDataDto>;

public class ReportUpcomingDtoValidator : AbstractValidator<ReportUpcomingDto>
{
    public ReportUpcomingDtoValidator()
    {
        RuleFor(x => x.LookAheadDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LookAheadKm).GreaterThanOrEqualTo(0);
    }
}
