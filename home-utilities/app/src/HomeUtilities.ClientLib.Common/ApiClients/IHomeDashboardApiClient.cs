using Kandra.ClientLib.Common.ApiClients;
using HomeUtilities.Forms.Dashboards;
using Refit;

namespace HomeUtilities.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dashboards/HomeDashboard")]
public interface IHomeDashboardApiClient : IDashboardApiClient<HomeDashboardDto, HomeDashboardResultDto>
{
}
