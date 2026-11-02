using Kandra.ClientLib.Common.ApiClients;
using HomeUtilities.Forms.Reports;
using Refit;

namespace HomeUtilities.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/reports/MeterConsumptionReport")]
public interface IMeterConsumptionReportApiClient : IReportApiClient<ReportMeterConsumptionDto, MeterConsumptionReportResult>
{
}
