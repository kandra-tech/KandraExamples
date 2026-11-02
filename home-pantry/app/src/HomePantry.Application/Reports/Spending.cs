using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
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

[KandraReportBehavior(typeof(ReportSpendingDto))]
public class SpendingReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker)
    : IReportBehavior<ReportSpendingDto, SpendingReportResult>
{
    public string ObjectName => "SpendingReport";

    public async Task<SpendingReportResult> GetDataAsync(ReportSpendingDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var from = filters.DateRange?.From;
        var to = filters.DateRange?.To;

        var lines = await repository.GetSpendingAsync(from, to, cancellationToken);
        var data = new SpendingReportDataDto
        {
            DateRangeText = filters.DateRange != null ? $"{filters.DateRange.From:d} - {filters.DateRange.To:d}" : null,
            Lines = SpendingReportMapper.ToDtos(lines)
        };
        var result = new SpendingReportResult { Filters = filters, Data = data };
        await EnrichPrintHeaderAsync(result, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(SpendingReportResult result, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);
    }

    public IFileData Export(SpendingReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"SpendingReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class SpendingReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<SpendingReportLineDto> ToDtos(IEnumerable<SpendingLine> lines);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial SpendingReportLineDto ToDto(SpendingLine line);
}
