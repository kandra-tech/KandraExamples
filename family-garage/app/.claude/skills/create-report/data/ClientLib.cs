using Kandra.ClientLib.Common.ApiClients;
using {{Config}}.Forms.Reports;
using Refit;

namespace {{Config}}.ClientLib.Common.ApiClients;

// Thin — everything is on the shared IReportApiClient<TFilters,TResult> base (reference.md §7). Refit's own
// source generator implements the interface body at compile time; you only ever write this declaration.
[PathPrefix("/api/v1/reports/{{Name}}Report")]
public interface I{{Name}}ReportApiClient : IReportApiClient<Report{{Name}}Dto, {{Name}}ReportResult>
{
}
