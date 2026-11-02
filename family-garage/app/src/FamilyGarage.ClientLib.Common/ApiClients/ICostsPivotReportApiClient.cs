using Kandra.ClientLib.Common.ApiClients;
using FamilyGarage.Forms.Reports;
using Refit;

namespace FamilyGarage.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/reports/CostsPivotReport")]
public interface ICostsPivotReportApiClient : IReportApiClient<ReportCostsPivotDto, CostsPivotReportResult>
{
}
