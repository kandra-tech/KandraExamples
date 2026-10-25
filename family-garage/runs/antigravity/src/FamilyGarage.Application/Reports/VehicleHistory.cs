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

[KandraReportBehavior(typeof(ReportVehicleHistoryDto))]
public class VehicleHistoryReportBehavior(
    IReportsRepository repository,
    ICurrentTimeZone timeZone)
    : IReportBehavior<ReportVehicleHistoryDto, VehicleHistoryReportResult>
{
    public string ObjectName => "VehicleHistoryReport";

    public async Task<VehicleHistoryReportResult> GetDataAsync(ReportVehicleHistoryDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetVehicleHistoryAsync(
            filters.VehicleId,
            filters.Period?.From,
            filters.Period?.To,
            timeZone.TimeZone,
            cancellationToken);

        var dtos = lines.Select(l => new VehicleHistoryReportLineDto
        {
            Id = Guid.NewGuid(),
            Date = l.Date,
            Odometer = l.Odometer,
            Item = l.ItemName,
            Quantity = l.Quantity,
            Amount = l.Amount,
            Document = l.DocumentCode
        }).ToList();

        var data = new VehicleHistoryReportDataDto { Lines = dtos };
        var result = new VehicleHistoryReportResult { Filters = filters, Data = data };

        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = null;

        return result;
    }

    public IFileData Export(VehicleHistoryReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"VehicleHistoryReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}
