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

namespace HomePantry.Forms.Reports;

public sealed class EquipmentReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid EquipmentId { get; set; }
    [Required]
    public string EquipmentCode { get; set; } = "";
    [Required]
    public string EquipmentName { get; set; } = "";

    [PrintColumn(WidthPercent = 25)]
    [Caption("Equipment")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string EquipmentText => CodeNameRef.Combine(EquipmentCode, EquipmentName);

    [PrintColumn(WidthPercent = 20)]
    [Caption("Location")]
    [ReportViewColumn(Groupable = true)]
    public string LocationName { get; set; } = "";

    [PrintColumn(WidthPercent = 15)]
    [Caption("BoughtOn")]
    [Format("d")]
    [ReportViewColumn]
    public DateTime? BoughtOn { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("WarrantyUntil")]
    [Format("d")]
    [ReportViewColumn]
    public DateTime? WarrantyUntil { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("DaysLeft")]
    [Format("N0")]
    [ReportViewColumn]
    public int? DaysLeft { get; set; }

    [PrintColumn(WidthPercent = 7)]
    [Caption("HasManual")]
    [ReportViewColumn]
    public bool HasManual { get; set; }

    [PrintColumn(WidthPercent = 8)]
    [Caption("HasPhoto")]
    [ReportViewColumn]
    public bool HasPhoto { get; set; }
}

[KandraReportForm(Name = "EquipmentReport", ResultDtoType = typeof(EquipmentReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(EquipmentReportDataDto))]
[ExecuteFormTitle("EquipmentReport")]
[Description("Lists household equipment whose manufacturer warranty ends within the chosen number of days.")]
public sealed class ReportEquipmentDto : IReportFiltersDto
{
    [Caption("Days")]
    [Description("Number of days ahead to look for expiring warranties.")]
    public int Days { get; set; } = 30;
}

[PrintForm]
[PrintTitle("EquipmentReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class EquipmentReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("Days")]
    public int Days { get; set; } = 30;

    [Required]
    [PrintTable(LineNumbers = true)]
    [ReportViewGrid]
    public required ICollection<EquipmentReportLineDto> Lines { get; set; }
}

public sealed class EquipmentReportResult : ReportResult<ReportEquipmentDto, EquipmentReportDataDto>;

public class ReportEquipmentDtoValidator : AbstractValidator<ReportEquipmentDto>
{
    public ReportEquipmentDtoValidator()
    {
        RuleFor(x => x.Days).GreaterThanOrEqualTo(0);
    }
}
