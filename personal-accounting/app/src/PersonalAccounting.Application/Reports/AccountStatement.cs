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

[KandraReportBehavior(typeof(ReportAccountStatementDto))]
public class AccountStatementReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker,
    IStringLocalizer localizer)
    : IReportBehavior<ReportAccountStatementDto, AccountStatementReportResult>
{
    public string ObjectName => "AccountStatementReport";

    public Task OnNewAsync(ReportAccountStatementDto filters, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        filters.DateRange = new DateRangeDto
        {
            From = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddTicks(-1)
        };
        return Task.CompletedTask;
    }

    public async Task<AccountStatementReportResult> GetDataAsync(ReportAccountStatementDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var openingLabel = localizer["Report_OpeningBalance"].Value ?? "Opening balance";
        var closingLabel = localizer["Report_ClosingBalance"].Value ?? "Closing balance";

        var rows = await repository.GetAccountStatementAsync(
            filters.AccountId,
            filters.DateRange?.From,
            filters.DateRange?.To,
            openingLabel,
            closingLabel,
            cancellationToken);

        var lines = AccountStatementReportMapper.ToDtos(rows).ToList();
        for (var i = 0; i < rows.Count && i < lines.Count; i++)
        {
            if (rows[i].DocumentTypeId.HasValue)
            {
                lines[i].DocumentType = DocumentTypeLocalizer.ResolveName(rows[i].DocumentTypeId, localizer);
            }
        }

        var data = new AccountStatementReportDataDto
        {
            Lines = lines
        };

        var result = new AccountStatementReportResult
        {
            Filters = filters,
            Data = data
        };

        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);

        return result;
    }

    public IFileData Export(AccountStatementReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"AccountStatement_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content
        };
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class AccountStatementReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<AccountStatementReportLineDto> ToDtos(IEnumerable<AccountStatementRow> lines);
}
