using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Forms.Querying;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Domain.Commons;
using PersonalAccounting.Domain.Reporting;
using PersonalAccounting.Forms.Reports;
using Riok.Mapperly.Abstractions;

namespace PersonalAccounting.Application.Reports;

[KandraReportBehavior(typeof(ReportTransactionJournalDto))]
public class TransactionJournalReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker,
    IStringLocalizer localizer)
    : IReportBehavior<ReportTransactionJournalDto, TransactionJournalReportResult>
{
    public string ObjectName => "TransactionJournalReport";

    public Task OnNewAsync(ReportTransactionJournalDto filters, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        filters.DateRange = new DateRangeDto
        {
            From = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddTicks(-1)
        };
        return Task.CompletedTask;
    }

    public async Task<TransactionJournalReportResult> GetDataAsync(ReportTransactionJournalDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var rows = await repository.GetTransactionJournalAsync(
            filters.DateRange?.From,
            filters.DateRange?.To,
            filters.AccountIds,
            filters.CategoryIds,
            cancellationToken);

        var lines = TransactionJournalReportMapper.ToDtos(rows).ToList();
        for (var i = 0; i < rows.Count && i < lines.Count; i++)
        {
            lines[i].DocumentType = DocumentTypeLocalizer.ResolveName(rows[i].DocumentTypeId, localizer);
        }

        var data = new TransactionJournalReportDataDto
        {
            Lines = lines
        };

        var result = new TransactionJournalReportResult
        {
            Filters = filters,
            Data = data
        };

        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);

        return result;
    }

    public IFileData Export(TransactionJournalReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"TransactionJournal_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content
        };
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class TransactionJournalReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<TransactionJournalReportLineDto> ToDtos(IEnumerable<TransactionJournalRow> lines);
}
