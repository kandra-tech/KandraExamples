using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using FamilyGarage.Domain.Commons;
using FamilyGarage.Forms.Reports;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Application.Reports;

[KandraReportBehavior(typeof(ReportFuelConsumptionDto))]
public class FuelConsumptionReportBehavior(
    IReportsRepository repository,
    ICurrentTimeZone timeZone)
    : IReportBehavior<ReportFuelConsumptionDto, FuelConsumptionReportResult>
{
    public string ObjectName => "FuelConsumptionReport";

    public async Task<FuelConsumptionReportResult> GetDataAsync(ReportFuelConsumptionDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var (lines, average) = await repository.GetFuelConsumptionAsync(
            filters.VehicleId,
            filters.Period?.From,
            filters.Period?.To,
            timeZone.TimeZone,
            cancellationToken);

        var dtos = lines.Select(l => new FuelConsumptionReportLineDto
        {
            Id = Guid.NewGuid(),
            Date = l.Date,
            Odometer = l.Odometer,
            Litres = l.Litres,
            Distance = l.Distance,
            Consumption = l.Consumption
        }).ToList();

        var data = new FuelConsumptionReportDataDto
        {
            PeriodAverageConsumption = average,
            Lines = dtos
        };
        var result = new FuelConsumptionReportResult { Filters = filters, Data = data };

        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = null;

        return result;
    }

    public IFileData Export(FuelConsumptionReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"FuelConsumptionReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}
