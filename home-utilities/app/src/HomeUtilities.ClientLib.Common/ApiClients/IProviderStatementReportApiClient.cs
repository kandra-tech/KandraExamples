using Kandra.ClientLib.Common.ApiClients;
using HomeUtilities.Forms.Reports;
using Refit;

namespace HomeUtilities.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/reports/ProviderStatementReport")]
public interface IProviderStatementReportApiClient : IReportApiClient<ReportProviderStatementDto, ProviderStatementReportResult>
{
}
