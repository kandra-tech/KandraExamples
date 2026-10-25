using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Application.Abstractions.Services;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using FamilyGarage.Application.Constants;
using FamilyGarage.Domain.Commons;
using FamilyGarage.Domain.Entities.ReportingModels;
using FamilyGarage.Domain.Reporting;
using FamilyGarage.Forms.Reports;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace FamilyGarage.Application.Reports;

[KandraReportBehavior(typeof(ReportUpcomingDto))]
public class UpcomingReportBehavior(
    IReportsRepository repository,
    IConstantsService constants,
    ICurrentTimeZone timeZone,
    IStringLocalizer localizer)
    : IReportBehavior<ReportUpcomingDto, UpcomingReportResult>
{
    public string ObjectName => "UpcomingReport";

    public async Task OnNewAsync(ReportUpcomingDto filters, CancellationToken cancellationToken)
    {
        var days = (await constants.GetAsync<UpcomingDaysAheadConstant>(DateTime.UtcNow, cancellationToken)).Value;
        var km = (await constants.GetAsync<UpcomingKmAheadConstant>(DateTime.UtcNow, cancellationToken)).Value;
        filters.LookAheadDays = days;
        filters.LookAheadKm = km;
    }

    public async Task<UpcomingReportResult> GetDataAsync(ReportUpcomingDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var items = await repository.GetUpcomingItemsAsync(
            filters.LookAheadDays,
            filters.LookAheadKm,
            filters.VehicleId,
            LocalCalendar.LocalDate(DateTime.UtcNow, timeZone.TimeZone),
            timeZone.TimeZone,
            cancellationToken);

        var lines = items.Select((item, index) => new UpcomingReportLineDto
        {
            Id = Guid.NewGuid(),
            Vehicle = item.VehicleName,
            Kind = UpcomingText.Kind(item, localizer),
            Description = UpcomingText.Description(item, localizer),
            DueDate = item.DueDate is { } due ? LocalCalendar.StartUtc(due, timeZone.TimeZone) : null,
            DueKm = item.DueKm,
            RemainingDays = item.RemainingDays,
            RemainingKm = item.RemainingKm,
            IsOverdue = item.IsOverdue
        }).ToList();

        var data = new UpcomingReportDataDto { Lines = lines };
        var result = new UpcomingReportResult { Filters = filters, Data = data };

        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = null;

        return result;
    }

    public IFileData Export(UpcomingReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"UpcomingReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}
