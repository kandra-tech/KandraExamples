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

[KandraReportBehavior(typeof(ReportBatteriesDto))]
public class BatteriesReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker)
    : IReportBehavior<ReportBatteriesDto, BatteriesReportResult>
{
    public string ObjectName => "BatteriesReport";

    public async Task<BatteriesReportResult> GetDataAsync(ReportBatteriesDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetBatteriesAsync(DateTime.UtcNow, cancellationToken);
        var data = new BatteriesReportDataDto
        {
            Lines = BatteriesReportMapper.ToDtos(lines)
        };
        var result = new BatteriesReportResult { Filters = filters, Data = data };
        await EnrichPrintHeaderAsync(result, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(BatteriesReportResult result, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);
    }

    public IFileData Export(BatteriesReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"BatteriesReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class BatteriesReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<BatteriesReportLineDto> ToDtos(IEnumerable<BatteriesReportLine> lines);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial BatteriesReportLineDto ToDto(BatteriesReportLine line);
}
