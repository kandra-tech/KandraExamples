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

[KandraReportBehavior(typeof(ReportCostsPivotDto))]
public class CostsPivotReportBehavior(
    IReportsRepository repository,
    ICurrentTimeZone timeZone)
    : IReportBehavior<ReportCostsPivotDto, CostsPivotReportResult>
{
    public string ObjectName => "CostsPivotReport";

    public async Task<CostsPivotReportResult> GetDataAsync(ReportCostsPivotDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetCostsPivotAsync(
            filters.Period?.From,
            filters.Period?.To,
            timeZone.TimeZone,
            cancellationToken);

        var dtos = lines.Select(l => new CostsPivotReportLineDto
        {
            Id = Guid.NewGuid(),
            VehicleCode = l.VehicleCode,
            VehicleName = l.VehicleName,
            Month = l.Month,
            Amount = l.Amount
        }).ToList();

        var data = new CostsPivotReportDataDto { Lines = dtos };
        var result = new CostsPivotReportResult { Filters = filters, Data = data };

        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = null;

        return result;
    }

    public IFileData Export(CostsPivotReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"CostsPivotReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}
