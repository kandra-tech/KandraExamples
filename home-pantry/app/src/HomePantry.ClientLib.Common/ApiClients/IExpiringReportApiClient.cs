using Kandra.ClientLib.Common.ApiClients;
using HomePantry.Forms.Reports;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/reports/ExpiringReport")]
public interface IExpiringReportApiClient : IReportApiClient<ReportExpiringDto, ExpiringReportResult>;
