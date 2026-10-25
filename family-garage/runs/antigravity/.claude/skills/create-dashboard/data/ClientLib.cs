using Kandra.ClientLib.Common.ApiClients;
using {{Config}}.Forms.Dashboards;
using Refit;

namespace {{Config}}.ClientLib.Common.ApiClients;

// Empty body: IDashboardApiClient<,> already declares New/Run/Settings, and Refit implements it at compile time.
// [PathPrefix] must match [KandraDashboardForm(Name = "...")] exactly.
[PathPrefix("/api/v1/dashboards/{{Name}}Dashboard")]
public interface I{{Name}}DashboardApiClient : IDashboardApiClient<{{Name}}DashboardDto, {{Name}}DashboardResultDto>
{
}
