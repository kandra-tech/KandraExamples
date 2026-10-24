using Kandra.ClientLib.Common.ApiClients;
using PersonalAccounting.Forms.Dashboards;
using Refit;

namespace PersonalAccounting.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dashboards/PersonalAccountingDashboard")]
public interface IPersonalAccountingDashboardApiClient : IDashboardApiClient<PersonalAccountingDashboardDto, PersonalAccountingDashboardResultDto>;
