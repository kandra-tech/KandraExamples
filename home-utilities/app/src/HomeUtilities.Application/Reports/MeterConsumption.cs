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

[KandraReportBehavior(typeof(ReportMeterConsumptionDto))]
public class MeterConsumptionReportBehavior(
    IReportsRepository repository,
    IEntityRepository<Property> propertiesRepository,
    IAuthorizationChecker authorizationChecker,
    ICurrentTimeZone? currentTimeZone = null,
    TimeProvider? time = null)
    : IReportBehavior<ReportMeterConsumptionDto, MeterConsumptionReportResult>
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public string ObjectName => "MeterConsumptionReport";

    public async Task OnNewAsync(ReportMeterConsumptionDto filters, CancellationToken cancellationToken)
    {
        filters.Year = TimeZoneHelper.ToLocalDate(_time.GetUtcNow().UtcDateTime, currentTimeZone?.TimeZone).Year;

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

    public async Task<MeterConsumptionReportResult> GetDataAsync(
        ReportMeterConsumptionDto filters,
        ILookupRepository lookup,
        CancellationToken cancellationToken)
    {
        var domainLines = await repository.GetMeterConsumptionAsync(
            filters.PropertyId,
            filters.Year,
            cancellationToken);

        var data = new MeterConsumptionReportDataDto
        {
            Year = filters.Year,
            Lines = MeterConsumptionReportMapper.ToDtos(domainLines),
        };

        var result = new MeterConsumptionReportResult
        {
            Filters = filters,
            Data = data,
        };

        await EnrichPrintHeaderAsync(result, filters, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(
        MeterConsumptionReportResult result,
        ReportMeterConsumptionDto filters,
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
        MeterConsumptionReportResult report,
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
public static partial class MeterConsumptionReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial MeterConsumptionReportLineDto ToDto(MeterConsumptionReportLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<MeterConsumptionReportLineDto> ToDtos(IEnumerable<MeterConsumptionReportLine> lines);
}
