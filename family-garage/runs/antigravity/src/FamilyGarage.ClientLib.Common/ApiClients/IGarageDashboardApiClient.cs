using Kandra.ClientLib.Common.ApiClients;
using FamilyGarage.Forms.Dashboards;
using Refit;

namespace FamilyGarage.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dashboards/GarageDashboard")]
public interface IGarageDashboardApiClient : IDashboardApiClient<GarageDashboardDto, GarageDashboardResultDto>
{
}
