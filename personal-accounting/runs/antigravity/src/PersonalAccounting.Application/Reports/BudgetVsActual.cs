using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Domain.Commons;
using PersonalAccounting.Domain.Reporting;
using PersonalAccounting.Forms.Reports;
using Riok.Mapperly.Abstractions;

namespace PersonalAccounting.Application.Reports;

[KandraReportBehavior(typeof(ReportBudgetVsActualDto))]
public class BudgetVsActualReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker)
    : IReportBehavior<ReportBudgetVsActualDto, BudgetVsActualReportResult>
{
    public string ObjectName => "BudgetVsActualReport";

    public Task OnNewAsync(ReportBudgetVsActualDto filters, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        filters.Month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return Task.CompletedTask;
    }

    public async Task<BudgetVsActualReportResult> GetDataAsync(ReportBudgetVsActualDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var rows = await repository.GetBudgetVsActualAsync(filters.Month, cancellationToken);

        var data = new BudgetVsActualReportDataDto
        {
            Lines = BudgetVsActualReportMapper.ToDtos(rows)
        };

        var result = new BudgetVsActualReportResult
        {
            Filters = filters,
            Data = data
        };

        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);

        return result;
    }

    public IFileData Export(BudgetVsActualReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"BudgetVsActual_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content
        };
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class BudgetVsActualReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<BudgetVsActualReportLineDto> ToDtos(IEnumerable<BudgetVsActualRow> lines);
}
