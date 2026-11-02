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

public sealed class BatteriesReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid BatteryId { get; set; }
    [Required]
    public string BatteryCode { get; set; } = "";
    [Required]
    public string BatteryName { get; set; } = "";

    [PrintColumn(WidthPercent = 20)]
    [Caption("Battery")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string BatteryText => CodeNameRef.Combine(BatteryCode, BatteryName);

    [PrintColumn(WidthPercent = 20)]
    [Caption("UsedIn")]
    [ReportViewColumn(Groupable = true)]
    public string UsedIn { get; set; } = "";

    [PrintColumn(WidthPercent = 10)]
    [Caption("BatteryKind")]
    [ReportViewColumn(Groupable = true)]
    public BatteryKind BatteryKind { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("ChargeIntervalDays")]
    [Format("N0")]
    [ReportViewColumn]
    public int ChargeIntervalDays { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("LastChargeDate")]
    [Format("d")]
    [ReportViewColumn]
    public DateTime? LastChargeDate { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("NextDueDate")]
    [Format("d")]
    [ReportViewColumn]
    public DateTime NextDueDate { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("IsOverdue")]
    [ReportViewColumn]
    public bool IsOverdue { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("TotalCycles")]
    [Format("N0")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal TotalCycles { get; set; }
}

[KandraReportForm(Name = "BatteriesReport", ResultDtoType = typeof(BatteriesReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(BatteriesReportDataDto))]
[ExecuteFormTitle("BatteriesReport")]
[Description("Lists each household battery, its device, the last charge, the next due date, overdue status, and total charge cycles.")]
public sealed class ReportBatteriesDto : IReportFiltersDto
{
}

[PrintForm]
[PrintTitle("BatteriesReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class BatteriesReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable(LineNumbers = true)]
    [ReportViewGrid]
    public required ICollection<BatteriesReportLineDto> Lines { get; set; }
}

public sealed class BatteriesReportResult : ReportResult<ReportBatteriesDto, BatteriesReportDataDto>;

public class ReportBatteriesDtoValidator : AbstractValidator<ReportBatteriesDto>
{
}
