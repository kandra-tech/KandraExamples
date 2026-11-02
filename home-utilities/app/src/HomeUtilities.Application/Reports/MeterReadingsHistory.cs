using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using HomeUtilities.Application.Common;
using HomeUtilities.Domain.Commons;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Domain.Entities.ReportingModels;
using HomeUtilities.Forms.Reports;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomeUtilities.Application.Reports;

[KandraReportBehavior(typeof(ReportMeterReadingsHistoryDto))]
public class MeterReadingsHistoryReportBehavior(
    IReportsRepository repository,
    IEntityRepository<Property> propertiesRepository,
    IAuthorizationChecker authorizationChecker,
    ICurrentTimeZone? currentTimeZone = null)
    : IReportBehavior<ReportMeterReadingsHistoryDto, MeterReadingsHistoryReportResult>
{
    public string ObjectName => "MeterReadingsHistoryReport";

    public async Task OnNewAsync(ReportMeterReadingsHistoryDto filters, CancellationToken cancellationToken)
    {
        var properties = new List<Property>();
        await foreach (var p in propertiesRepository.GetAllAsync(q => q.Where(x => !x.IsDeleted)))
        {
            properties.Add(p);
        }

        if (properties.Count == 1)
        {
            filters.PropertyId = properties[0].Id;
        }
    }

    public async Task<MeterReadingsHistoryReportResult> GetDataAsync(
        ReportMeterReadingsHistoryDto filters,
        ILookupRepository lookup,
        CancellationToken cancellationToken)
    {
        var fromDate = filters.DateRange?.From is { } f ? TimeZoneHelper.StartOfLocalDayUtc(f, currentTimeZone?.TimeZone) : (DateTime?)null;
        var toDate = filters.DateRange?.To is { } t ? TimeZoneHelper.EndOfLocalDayUtc(t, currentTimeZone?.TimeZone) : (DateTime?)null;

        var domainLines = await repository.GetMeterReadingsHistoryAsync(
            filters.PropertyId,
            filters.MeterId,
            fromDate,
            toDate,
            cancellationToken);

        var data = new MeterReadingsHistoryReportDataDto
        {
            Lines = MeterReadingsHistoryReportMapper.ToDtos(domainLines),
        };

        var result = new MeterReadingsHistoryReportResult
        {
            Filters = filters,
            Data = data,
        };

        await EnrichPrintHeaderAsync(result, filters, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(
        MeterReadingsHistoryReportResult result,
        ReportMeterReadingsHistoryDto filters,
        ILookupRepository lookup,
        CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);

        if (filters.PropertyId != Guid.Empty)
        {
            var property = await lookup.GetDictionaryAsync<Property>([filters.PropertyId], cancellationToken);
            if (property.Count > 0)
            {
                result.Data.PropertyName = property[0].Name;
            }
        }
    }

    public IFileData Export(
        MeterReadingsHistoryReportResult report,
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
public static partial class MeterReadingsHistoryReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial MeterReadingsHistoryReportLineDto ToDto(MeterReadingsHistoryReportLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<MeterReadingsHistoryReportLineDto> ToDtos(IEnumerable<MeterReadingsHistoryReportLine> lines);
}
