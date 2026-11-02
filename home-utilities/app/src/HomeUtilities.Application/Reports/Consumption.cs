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

[KandraReportBehavior(typeof(ReportConsumptionDto))]
public class ConsumptionReportBehavior(
    IReportsRepository repository,
    IEntityRepository<Property> propertiesRepository,
    IAuthorizationChecker authorizationChecker,
    ICurrentTimeZone? currentTimeZone = null,
    TimeProvider? time = null)
    : IReportBehavior<ReportConsumptionDto, ConsumptionReportResult>
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public string ObjectName => "ConsumptionReport";

    public async Task OnNewAsync(ReportConsumptionDto filters, CancellationToken cancellationToken)
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

    public async Task<ConsumptionReportResult> GetDataAsync(
        ReportConsumptionDto filters,
        ILookupRepository lookup,
        CancellationToken cancellationToken)
    {
        var domainLines = await repository.GetConsumptionAsync(
            filters.PropertyId,
            filters.Year,
            cancellationToken);

        var data = new ConsumptionReportDataDto
        {
            Year = filters.Year,
            Lines = ConsumptionReportMapper.ToDtos(domainLines),
        };

        var result = new ConsumptionReportResult
        {
            Filters = filters,
            Data = data,
        };

        await EnrichPrintHeaderAsync(result, filters, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(
        ConsumptionReportResult result,
        ReportConsumptionDto filters,
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
        ConsumptionReportResult report,
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
public static partial class ConsumptionReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial ConsumptionReportLineDto ToDto(ConsumptionReportLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<ConsumptionReportLineDto> ToDtos(IEnumerable<ConsumptionReportLine> lines);
}
