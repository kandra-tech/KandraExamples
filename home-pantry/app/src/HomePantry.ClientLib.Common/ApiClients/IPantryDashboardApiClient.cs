using Kandra.ClientLib.Common.ApiClients;
using HomePantry.Forms.Dashboards;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dashboards/PantryDashboard")]
public interface IPantryDashboardApiClient : IDashboardApiClient<PantryDashboardDto, PantryDashboardResultDto>;
