using Kandra.ClientLib.Common.ApiClients;
using PersonalAccounting.Forms.Reports;
using Refit;

namespace PersonalAccounting.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/reports/ExchangeRatesReport")]
public interface IExchangeRatesReportApiClient : IReportApiClient<ReportExchangeRatesDto, ExchangeRatesReportResult>;
