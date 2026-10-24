using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using {{Config}}.Domain.Commons;
using {{Config}}.Domain.Entities.Dictionaries;
using {{Config}}.Domain.Entities.ReportingModels;
using {{Config}}.Forms.Reports;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace {{Config}}.Application.Reports;

[KandraReportBehavior(typeof(Report{{Name}}Dto))]
public class {{Name}}ReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker)
    // Add when needed:
    //   IConstantsService constants   - read a constant HERE (as of the report date) and pass its value to the repository;
    //                                   the repository never reads constants itself, and never with raw SQL.
    //   IStringLocalizer localizer    - turn a row's kind into words HERE; the repository returns kinds/ids, no English text:
    //                                   line.Description = localizer[line.Kind == RowKind.Opening ? "Report_OpeningBalance" : "Report_ClosingBalance"];
    : IReportBehavior<Report{{Name}}Dto, {{Name}}ReportResult>
{
    // MUST equal [KandraReportForm(Name = "{{Name}}Report")] on the filters Dto: the generated claims, the nav node's
    // permission check and the endpoint all use that name; any other value answers "permission denied".
    public string ObjectName => "{{Name}}Report";

    // OPTIONAL — delete if the report has no filter that needs a server-side default.
    public Task OnNewAsync(Report{{Name}}Dto filters, CancellationToken cancellationToken)
    {
        filters.{{BoolFilterField}} = true;
        return Task.CompletedTask;
    }

    public async Task<{{Name}}ReportResult> GetDataAsync(Report{{Name}}Dto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.Get{{Name}}(filters.{{BoolFilterField}}, filters.{{MultiSelectFilterField}}Id, cancellationToken);
        var data = new {{Name}}ReportDataDto { Lines = {{Name}}ReportMapper.ToDtos(lines) };
        var result = new {{Name}}ReportResult { Filters = filters, Data = data };
        await EnrichPrintHeaderAsync(result, filters, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync({{Name}}ReportResult result, Report{{Name}}Dto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);

        if (filters.{{MultiSelectFilterField}}Id?.Any() == true)
        {
            result.Data.{{MultiSelectFilterField}}s = await lookup.GetDictionaryAsync<{{MultiSelectSourceEntity}}>(filters.{{MultiSelectFilterField}}Id, cancellationToken);
        }
    }

    // OPTIONAL — delete if [SupportedExportFormats] isn't declared on the filters Dto (§1). Never hand-write
    // CSV/PDF rendering code here — delegate to the resolver, see create-print-form.
    public IFileData Export({{Name}}ReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"{{Name}}Report_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

// Static Mapperly mapper - reports map one way only (row -> line Dto) and need no DI. {{SourceRowType}} is the
// repository's own plain, non-persisted, non-attributed result-row class (step 2 —
// {{Config}}.Domain/Entities/ReportingModels/) - its name doesn't mechanically follow {{Name}} (e.g. the real
// RestsReport's row type is "RestReportLine", singular, not "RestsReportSourceRow"). Target: every line-Dto
// member must have a source; a renamed one gets [MapProperty] on an explicit element method, e.g.
//   [MapProperty(nameof({{SourceRowType}}.DocumentId), nameof({{Name}}ReportLineDto.OrderId))]
//   [MapperRequiredMapping(RequiredMappingStrategy.Target)]
//   private static partial {{Name}}ReportLineDto ToDto({{SourceRowType}} line);
[Mapper]
public static partial class {{Name}}ReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<{{Name}}ReportLineDto> ToDtos(IEnumerable<{{SourceRowType}}> lines);
}
