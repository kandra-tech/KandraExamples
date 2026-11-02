using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Forms.Querying;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Domain.Commons;
using PersonalAccounting.Domain.Reporting;
using PersonalAccounting.Forms.Reports;
using Riok.Mapperly.Abstractions;

namespace PersonalAccounting.Application.Reports;

[KandraReportBehavior(typeof(ReportSpendingByCategoryDto))]
public class SpendingByCategoryReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker)
    : IReportBehavior<ReportSpendingByCategoryDto, SpendingByCategoryReportResult>
{
    public string ObjectName => "SpendingByCategoryReport";

    public Task OnNewAsync(ReportSpendingByCategoryDto filters, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        filters.DateRange = new DateRangeDto
        {
            From = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddTicks(-1)
        };
        return Task.CompletedTask;
    }

    public async Task<SpendingByCategoryReportResult> GetDataAsync(ReportSpendingByCategoryDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var rows = await repository.GetSpendingByCategoryAsync(
            filters.DateRange?.From,
            filters.DateRange?.To,
            filters.CategoryIds,
            cancellationToken);

        var data = new SpendingByCategoryReportDataDto
        {
            Lines = SpendingByCategoryReportMapper.ToDtos(rows)
        };

        var result = new SpendingByCategoryReportResult
        {
            Filters = filters,
            Data = data
        };

        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);

        return result;
    }

    public IFileData Export(SpendingByCategoryReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"SpendingByCategory_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content
        };
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class SpendingByCategoryReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<SpendingByCategoryReportLineDto> ToDtos(IEnumerable<SpendingByCategoryRow> lines);
}
