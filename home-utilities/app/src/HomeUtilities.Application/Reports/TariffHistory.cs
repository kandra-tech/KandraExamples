using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using HomeUtilities.Domain.Commons;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Domain.Entities.ReportingModels;
using HomeUtilities.Forms.Reports;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomeUtilities.Application.Reports;

[KandraReportBehavior(typeof(ReportTariffHistoryDto))]
public class TariffHistoryReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker)
    : IReportBehavior<ReportTariffHistoryDto, TariffHistoryReportResult>
{
    public string ObjectName => "TariffHistoryReport";

    public async Task<TariffHistoryReportResult> GetDataAsync(
        ReportTariffHistoryDto filters,
        ILookupRepository lookup,
        CancellationToken cancellationToken)
    {
        var domainLines = await repository.GetTariffHistoryAsync(
            filters.ServiceId,
            cancellationToken);

        var data = new TariffHistoryReportDataDto
        {
            Lines = TariffHistoryReportMapper.ToDtos(domainLines),
        };

        var result = new TariffHistoryReportResult
        {
            Filters = filters,
            Data = data,
        };

        await EnrichPrintHeaderAsync(result, filters, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(
        TariffHistoryReportResult result,
        ReportTariffHistoryDto filters,
        ILookupRepository lookup,
        CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);

        if (filters.ServiceId != Guid.Empty)
        {
            var service = await lookup.GetDictionaryAsync<ServiceNode>([filters.ServiceId], cancellationToken);
            if (service.Count > 0)
            {
                result.Data.ServiceName = service[0].Name;
            }
        }
    }

    public IFileData Export(
        TariffHistoryReportResult report,
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
public static partial class TariffHistoryReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial TariffHistoryReportLineDto ToDto(TariffHistoryReportLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<TariffHistoryReportLineDto> ToDtos(IEnumerable<TariffHistoryReportLine> lines);
}
