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

[KandraReportBehavior(typeof(ReportEquipmentDto))]
public class EquipmentReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker)
    : IReportBehavior<ReportEquipmentDto, EquipmentReportResult>
{
    public string ObjectName => "EquipmentReport";

    public async Task<EquipmentReportResult> GetDataAsync(ReportEquipmentDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetEquipmentWarrantiesAsync(filters.Days, DateTime.UtcNow, cancellationToken);
        var data = new EquipmentReportDataDto
        {
            Days = filters.Days,
            Lines = EquipmentReportMapper.ToDtos(lines)
        };
        var result = new EquipmentReportResult { Filters = filters, Data = data };
        await EnrichPrintHeaderAsync(result, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(EquipmentReportResult result, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);
    }

    public IFileData Export(EquipmentReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"EquipmentReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class EquipmentReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<EquipmentReportLineDto> ToDtos(IEnumerable<EquipmentReportLine> lines);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial EquipmentReportLineDto ToDto(EquipmentReportLine line);
}
