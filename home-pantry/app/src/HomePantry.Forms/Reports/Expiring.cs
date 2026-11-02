using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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

public sealed class ExpiringReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid ProductId { get; set; }
    [Required]
    public string ProductCode { get; set; } = "";
    [Required]
    public string ProductName { get; set; } = "";

    [PrintColumn(WidthPercent = 30)]
    [Caption("Product")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string ProductText => CodeNameRef.Combine(ProductCode, ProductName);

    [Required]
    public Guid LocationId { get; set; }
    [Required]
    public string LocationCode { get; set; } = "";
    [Required]
    public string LocationName { get; set; } = "";

    [PrintColumn(WidthPercent = 25)]
    [Caption("Location")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string LocationText => CodeNameRef.Combine(LocationCode, LocationName);

    [PrintColumn(WidthPercent = 15)]
    [Caption("BestBeforeDate")]
    [Format("d")]
    [ReportViewColumn]
    public DateTime BestBeforeDate { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("Quantity")]
    [Format(PrintFormats.Quantity)]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Quantity { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("DaysLeft")]
    [ReportViewColumn]
    public int DaysLeft { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("IsExpired")]
    [ReportViewColumn]
    public bool IsExpired { get; set; }
}

[KandraReportForm(Name = "ExpiringReport", ResultDtoType = typeof(ExpiringReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(ExpiringReportDataDto))]
[ExecuteFormTitle("ExpiringReport")]
[Description("Lists every batch already expired or expiring within the look-ahead period, with location and days left.")]
public sealed class ReportExpiringDto : IReportFiltersDto
{
}

[PrintForm]
[PrintTitle("ExpiringReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class ExpiringReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("LookAheadDays")]
    public int LookAheadDays { get; set; }

    [Required]
    [PrintTable(LineNumbers = true)]
    [ReportViewGrid]
    public required ICollection<ExpiringReportLineDto> Lines { get; set; }
}

public sealed class ExpiringReportResult : ReportResult<ReportExpiringDto, ExpiringReportDataDto>;

public class ReportExpiringDtoValidator : AbstractValidator<ReportExpiringDto>
{
}
