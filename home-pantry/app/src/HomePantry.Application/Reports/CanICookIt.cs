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

[KandraReportBehavior(typeof(ReportCanICookItDto))]
public class CanICookItReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker,
    IStringLocalizer localizer)
    : IReportBehavior<ReportCanICookItDto, CanICookItReportResult>
{
    public string ObjectName => "CanICookItReport";

    public async Task<CanICookItReportResult> GetDataAsync(ReportCanICookItDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetCanICookItAsync(cancellationToken);
        var format = localizer["CanICookIt_MissingIngredientFormat"].Value;
        foreach (var line in lines)
        {
            if (line.MissingItems.Count > 0)
            {
                line.MissingIngredients = string.Join("; ", line.MissingItems.Select(m => string.Format(format, m.ProductName, m.Needed, m.OnHand)));
            }
        }
        var data = new CanICookItReportDataDto { Lines = CanICookItReportMapper.ToDtos(lines) };
        var result = new CanICookItReportResult { Filters = filters, Data = data };
        await EnrichPrintHeaderAsync(result, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(CanICookItReportResult result, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);
    }

    public IFileData Export(CanICookItReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"CanICookItReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class CanICookItReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<CanICookItReportLineDto> ToDtos(IEnumerable<CanICookItLine> lines);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial CanICookItReportLineDto ToDto(CanICookItLine line);
}
