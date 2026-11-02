using Kandra.ClientLib.Common.ApiClients;
using FamilyGarage.Forms.Reports;
using Refit;

namespace FamilyGarage.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/reports/FuelConsumptionReport")]
public interface IFuelConsumptionReportApiClient : IReportApiClient<ReportFuelConsumptionDto, FuelConsumptionReportResult>
{
}
