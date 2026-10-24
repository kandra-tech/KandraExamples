using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Domain.Commons;
using PersonalAccounting.Domain.Reporting;
using PersonalAccounting.Forms.Reports;
using Riok.Mapperly.Abstractions;

namespace PersonalAccounting.Application.Reports;

[KandraReportBehavior(typeof(ReportExchangeRatesDto))]
public class ExchangeRatesReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker)
    : IReportBehavior<ReportExchangeRatesDto, ExchangeRatesReportResult>
{
    public string ObjectName => "ExchangeRatesReport";

    public Task OnNewAsync(ReportExchangeRatesDto filters, CancellationToken cancellationToken)
    {
        filters.DateAsOf = DateTime.UtcNow.Date;
        return Task.CompletedTask;
    }

    public async Task<ExchangeRatesReportResult> GetDataAsync(ReportExchangeRatesDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var asOf = filters.DateAsOf ?? DateTime.UtcNow;
        var rows = await repository.GetExchangeRatesAsync(asOf, cancellationToken);

        var data = new ExchangeRatesReportDataDto
        {
            Lines = ExchangeRatesReportMapper.ToDtos(rows)
        };

        var result = new ExchangeRatesReportResult
        {
            Filters = filters,
            Data = data
        };

        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);

        return result;
    }

    public IFileData Export(ExchangeRatesReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"ExchangeRates_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content
        };
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ExchangeRatesReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<ExchangeRatesReportLineDto> ToDtos(IEnumerable<ExchangeRateRow> lines);
}
