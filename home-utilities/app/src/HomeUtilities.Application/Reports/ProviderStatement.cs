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
using HomeUtilities.Enums;
using HomeUtilities.Forms.Reports;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomeUtilities.Application.Reports;

[KandraReportBehavior(typeof(ReportProviderStatementDto))]
public class ProviderStatementReportBehavior(
    IReportsRepository repository,
    IEntityRepository<Property> propertiesRepository,
    IStringLocalizer localizer,
    IAuthorizationChecker authorizationChecker,
    ICurrentTimeZone? currentTimeZone = null)
    : IReportBehavior<ReportProviderStatementDto, ProviderStatementReportResult>
{
    public string ObjectName => "ProviderStatementReport";

    public async Task OnNewAsync(ReportProviderStatementDto filters, CancellationToken cancellationToken)
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

    public async Task<ProviderStatementReportResult> GetDataAsync(
        ReportProviderStatementDto filters,
        ILookupRepository lookup,
        CancellationToken cancellationToken)
    {
        var fromDate = filters.DateRange?.From is { } f ? TimeZoneHelper.StartOfLocalDayUtc(f, currentTimeZone?.TimeZone) : (DateTime?)null;
        var toDate = filters.DateRange?.To is { } t ? TimeZoneHelper.EndOfLocalDayUtc(t, currentTimeZone?.TimeZone) : (DateTime?)null;

        var domainLines = await repository.GetProviderStatementAsync(
            filters.ProviderId,
            filters.PropertyId,
            fromDate,
            toDate,
            cancellationToken);

        var lines = ProviderStatementReportMapper.ToDtos(domainLines);
        for (var i = 0; i < lines.Count; i++)
        {
            lines[i].Description = GetDescription(domainLines[i].Kind);
        }

        var data = new ProviderStatementReportDataDto
        {
            Lines = lines,
        };

        var result = new ProviderStatementReportResult
        {
            Filters = filters,
            Data = data,
        };

        await EnrichPrintHeaderAsync(result, filters, lookup, cancellationToken);
        return result;
    }

    private string GetDescription(StatementRowKind kind) => kind switch
    {
        StatementRowKind.OpeningBalance => localizer["Report_OpeningBalance"].Value,
        StatementRowKind.Invoice => localizer["Report_Invoice"].Value,
        StatementRowKind.Payment => localizer["Report_Payment"].Value,
        StatementRowKind.ClosingBalance => localizer["Report_ClosingBalance"].Value,
        _ => string.Empty,
    };

    private async Task EnrichPrintHeaderAsync(
        ProviderStatementReportResult result,
        ReportProviderStatementDto filters,
        ILookupRepository lookup,
        CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);

        if (filters.ProviderId != Guid.Empty)
        {
            var provider = await lookup.GetDictionaryAsync<Provider>([filters.ProviderId], cancellationToken);
            if (provider.Count > 0)
            {
                result.Data.ProviderName = provider[0].Name;
            }
        }

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
        ProviderStatementReportResult report,
        ExportFormat format,
        string? layoutTag,
        IPrintRendererResolver renderer,
        IStringLocalizer stringLocalizer,
        TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, stringLocalizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"{ObjectName}_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class ProviderStatementReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    [MapperIgnoreTarget(nameof(ProviderStatementReportLineDto.Description))]
    public static partial ProviderStatementReportLineDto ToDto(ProviderStatementLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<ProviderStatementReportLineDto> ToDtos(IEnumerable<ProviderStatementLine> lines);
}
