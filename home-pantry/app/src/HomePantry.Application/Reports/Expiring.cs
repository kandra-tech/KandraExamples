using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Constants;
using Kandra.Application.Abstractions.Files;
using Kandra.Application.Abstractions.Services;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using HomePantry.Application.Constants;
using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.ReportingModels;
using HomePantry.Forms.Reports;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Reports;

[KandraReportBehavior(typeof(ReportExpiringDto))]
public class ExpiringReportBehavior(
    IReportsRepository repository,
    ICurrentTimeZone timeZone,
    IConstantsService constants,
    IAuthorizationChecker authorizationChecker)
    : IReportBehavior<ReportExpiringDto, ExpiringReportResult>
{
    public string ObjectName => "ExpiringReport";

    public async Task<ExpiringReportResult> GetDataAsync(ReportExpiringDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var constantVal = await constants.GetAsync<ExpiringLookAheadDaysConstant>(DateTime.UtcNow, cancellationToken);
        var lookAhead = constantVal?.Value ?? 5;
        if (lookAhead <= 0) lookAhead = 5;

        var lines = await repository.GetExpiringAsync(lookAhead, DateTime.UtcNow, timeZone.TimeZone, cancellationToken);
        var data = new ExpiringReportDataDto
        {
            LookAheadDays = lookAhead,
            Lines = ExpiringReportMapper.ToDtos(lines)
        };
        var result = new ExpiringReportResult { Filters = filters, Data = data };
        await EnrichPrintHeaderAsync(result, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(ExpiringReportResult result, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);
    }

    public IFileData Export(ExpiringReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"ExpiringReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class ExpiringReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<ExpiringReportLineDto> ToDtos(IEnumerable<ExpiringLine> lines);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial ExpiringReportLineDto ToDto(ExpiringLine line);
}
