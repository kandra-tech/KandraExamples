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

[KandraReportBehavior(typeof(ReportChoresDto))]
public class ChoresReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker)
    : IReportBehavior<ReportChoresDto, ChoresReportResult>
{
    public string ObjectName => "ChoresReport";

    public async Task<ChoresReportResult> GetDataAsync(ReportChoresDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetChoresAsync(DateTime.UtcNow, cancellationToken);
        var data = new ChoresReportDataDto
        {
            Lines = ChoresReportMapper.ToDtos(lines)
        };
        var result = new ChoresReportResult { Filters = filters, Data = data };
        await EnrichPrintHeaderAsync(result, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(ChoresReportResult result, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);
    }

    public IFileData Export(ChoresReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"ChoresReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class ChoresReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<ChoresReportLineDto> ToDtos(IEnumerable<ChoresReportLine> lines);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial ChoresReportLineDto ToDto(ChoresReportLine line);
}
