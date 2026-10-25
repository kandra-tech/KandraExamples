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

public sealed class FuelConsumptionReportLineDto
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
    [Caption("Litres")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    public decimal Litres { get; set; }

    [Order(4)]
    [PrintColumn]
    [Caption("Distance")]
    [Format("N0")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    public int Distance { get; set; }

    [Order(5)]
    [PrintColumn]
    [Caption("Consumption")]
    [Format("N2")]
    [ReportViewColumn]
    public decimal Consumption { get; set; }
}

[KandraReportForm(Name = "FuelConsumptionReport", ResultDtoType = typeof(FuelConsumptionReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(FuelConsumptionReportDataDto))]
[ExecuteFormTitle("FuelConsumptionReport")]
[Description("Calculates fuel consumption in litres per 100 km between consecutive full-tank refuels, and average for the period.")]
public sealed class ReportFuelConsumptionDto : IReportFiltersDto
{
    [Required]
    [Caption("Vehicle")]
    [Dropdown(typeof(VehicleDto))]
    [Description("The vehicle whose fuel consumption is analyzed.")]
    public Guid VehicleId { get; set; }

    [DateRangeEditor]
    public DateRangeDto? Period { get; set; }
}

[PrintForm]
[PrintTitle("FuelConsumptionReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape)]
public sealed class FuelConsumptionReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("PeriodAverageConsumption")]
    [Format("N2")]
    public decimal PeriodAverageConsumption { get; set; }

    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<FuelConsumptionReportLineDto> Lines { get; set; }
}

public sealed class FuelConsumptionReportResult : ReportResult<ReportFuelConsumptionDto, FuelConsumptionReportDataDto>;

public class ReportFuelConsumptionDtoValidator : AbstractValidator<ReportFuelConsumptionDto>
{
    public ReportFuelConsumptionDtoValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Period)
            .Must(r => r is null || r.From is null || r.To is null || r.From.Value <= r.To.Value)
            .WithName(nameof(ReportFuelConsumptionDto.Period));
    }
}
