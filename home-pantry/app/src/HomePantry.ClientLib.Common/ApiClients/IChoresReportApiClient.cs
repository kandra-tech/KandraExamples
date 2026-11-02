using Kandra.ClientLib.Common.ApiClients;
using HomePantry.Forms.Reports;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/reports/ChoresReport")]
public interface IChoresReportApiClient : IReportApiClient<ReportChoresDto, ChoresReportResult>;
