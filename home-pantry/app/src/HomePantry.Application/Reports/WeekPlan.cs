using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Application.Abstractions.Services;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.ReportingModels;
using HomePantry.Forms.Reports;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Reports;

[KandraReportBehavior(typeof(ReportWeekPlanDto))]
public class WeekPlanReportBehavior(
    IReportsRepository repository,
    ICurrentTimeZone timeZone,
    IAuthorizationChecker authorizationChecker)
    : IReportBehavior<ReportWeekPlanDto, WeekPlanReportResult>
{
    public string ObjectName => "WeekPlanReport";

    public async Task<WeekPlanReportResult> GetDataAsync(ReportWeekPlanDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetWeekPlanAsync(filters.Week, timeZone.TimeZone, cancellationToken);
        var data = new WeekPlanReportDataDto
        {
            Week = filters.Week,
            Lines = WeekPlanReportMapper.ToDtos(lines)
        };
        var result = new WeekPlanReportResult { Filters = filters, Data = data };
        await EnrichPrintHeaderAsync(result, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(WeekPlanReportResult result, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);
    }

    public IFileData Export(WeekPlanReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"WeekPlanReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class WeekPlanReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<WeekPlanReportLineDto> ToDtos(IEnumerable<WeekPlanReportLine> lines);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial WeekPlanReportLineDto ToDto(WeekPlanReportLine line);
}
