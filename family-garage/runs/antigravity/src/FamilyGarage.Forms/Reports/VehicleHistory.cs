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
using FamilyGarage.Forms.Dictionaries;

namespace FamilyGarage.Forms.Reports;

public sealed class VehicleHistoryReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Order(1)]
    [PrintColumn]
    [Caption("Date")]
    [ReportViewColumn]
    [Format("d")]
    public DateTime Date { get; set; }

    [Order(2)]
    [PrintColumn]
    [Caption("Odometer")]
    [Format("N0")]
    [ReportViewColumn]
    public int Odometer { get; set; }

    [Order(3)]
    [PrintColumn]
    [Caption("Item")]
    [ReportViewColumn(Groupable = true)]
    public string Item { get; set; } = string.Empty;

    [Order(4)]
    [PrintColumn]
    [Caption("Quantity")]
    [Format("N3")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    public decimal Quantity { get; set; }

    [Order(5)]
    [PrintColumn]
    [Caption("Amount")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Amount { get; set; }

    [Order(6)]
    [PrintColumn]
    [Caption("Document")]
    [ReportViewColumn]
    public string Document { get; set; } = string.Empty;
}

[KandraReportForm(Name = "VehicleHistoryReport", ResultDtoType = typeof(VehicleHistoryReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(VehicleHistoryReportDataDto))]
[ExecuteFormTitle("VehicleHistoryReport")]
[Description("Itemized replacement and service history for a chosen vehicle over a period.")]
public sealed class ReportVehicleHistoryDto : IReportFiltersDto
{
    [Required]
    [Caption("Vehicle")]
    [Dropdown(typeof(VehicleDto))]
    [Description("The vehicle whose history is viewed.")]
    public Guid VehicleId { get; set; }

    [DateRangeEditor]
    public DateRangeDto? Period { get; set; }
}

[PrintForm]
[PrintTitle("VehicleHistoryReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape)]
public sealed class VehicleHistoryReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<VehicleHistoryReportLineDto> Lines { get; set; }
}

public sealed class VehicleHistoryReportResult : ReportResult<ReportVehicleHistoryDto, VehicleHistoryReportDataDto>;

public class ReportVehicleHistoryDtoValidator : AbstractValidator<ReportVehicleHistoryDto>
{
    public ReportVehicleHistoryDtoValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Period)
            .Must(r => r is null || r.From is null || r.To is null || r.From.Value <= r.To.Value)
            .WithName(nameof(ReportVehicleHistoryDto.Period));
    }
}
