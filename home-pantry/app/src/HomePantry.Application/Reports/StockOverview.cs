using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Localization;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Entities.ReportingModels;
using HomePantry.Forms.Reports;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Reports;

[KandraReportBehavior(typeof(ReportStockOverviewDto))]
public class StockOverviewReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker,
    IStringLocalizer localizer)
    : IReportBehavior<ReportStockOverviewDto, StockOverviewReportResult>
{
    public string ObjectName => "StockOverviewReport";

    public async Task<StockOverviewReportResult> GetDataAsync(ReportStockOverviewDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetStockOverviewAsync(filters.LocationId, filters.ProductGroupId, cancellationToken);
        foreach (var line in lines)
        {
            line.Unit = line.ProductUnit.GetCaption(localizer);
        }
        var data = new StockOverviewReportDataDto { Lines = StockOverviewReportMapper.ToDtos(lines) };
        var result = new StockOverviewReportResult { Filters = filters, Data = data };
        await EnrichPrintHeaderAsync(result, filters, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(StockOverviewReportResult result, ReportStockOverviewDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);

        if (filters.LocationId.HasValue && filters.LocationId.Value != Guid.Empty)
        {
            var locs = (await lookup.GetDictionaryAsync<LocationNode>(new[] { filters.LocationId.Value }, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
            result.Data.LocationName = locs.GetValueOrDefault(filters.LocationId.Value);
        }

        if (filters.ProductGroupId.HasValue && filters.ProductGroupId.Value != Guid.Empty)
        {
            var groups = (await lookup.GetDictionaryAsync<ProductNode>(new[] { filters.ProductGroupId.Value }, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
            result.Data.ProductGroupName = groups.GetValueOrDefault(filters.ProductGroupId.Value);
        }
    }

    public IFileData Export(StockOverviewReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"StockOverviewReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class StockOverviewReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<StockOverviewReportLineDto> ToDtos(IEnumerable<StockOverviewLine> lines);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial StockOverviewReportLineDto ToDto(StockOverviewLine line);
}
