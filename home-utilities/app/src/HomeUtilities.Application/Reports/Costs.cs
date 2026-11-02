using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using HomeUtilities.Application.Common;
using HomeUtilities.Domain.Commons;
using HomeUtilities.Domain.Entities.ReportingModels;
using HomeUtilities.Forms.Reports;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomeUtilities.Application.Reports;

[KandraReportBehavior(typeof(ReportCostsDto))]
public class CostsReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker,
    ICurrentTimeZone? currentTimeZone = null)
    : IReportBehavior<ReportCostsDto, CostsReportResult>
{
    public string ObjectName => "CostsReport";

    public async Task<CostsReportResult> GetDataAsync(
        ReportCostsDto filters,
        ILookupRepository lookup,
        CancellationToken cancellationToken)
    {
        var fromDate = filters.DateRange?.From is { } f ? TimeZoneHelper.StartOfLocalDayUtc(f, currentTimeZone?.TimeZone) : (DateTime?)null;
        var toDate = filters.DateRange?.To is { } t ? TimeZoneHelper.EndOfLocalDayUtc(t, currentTimeZone?.TimeZone) : (DateTime?)null;

        var domainLines = await repository.GetCostsAsync(
            fromDate,
            toDate,
            cancellationToken);

        var data = new CostsReportDataDto
        {
            Lines = CostsReportMapper.ToDtos(domainLines),
        };

        var result = new CostsReportResult
        {
            Filters = filters,
            Data = data,
        };

        await EnrichPrintHeaderAsync(result, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(
        CostsReportResult result,
        ILookupRepository lookup,
        CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);
    }

    public IFileData Export(
        CostsReportResult report,
        ExportFormat format,
        string? layoutTag,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"{ObjectName}_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class CostsReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial CostsReportLineDto ToDto(CostsReportLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<CostsReportLineDto> ToDtos(IEnumerable<CostsReportLine> lines);
}
