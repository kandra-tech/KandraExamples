using Kandra.ClientLib.Common.ApiClients;
using HomePantry.Forms.DataProcessors;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/processors/CompanySettings")]
public interface ICompanySettingsApiClient : IDataProcessorApiClient<CompanySettingsDto, CompanySettingsResult>
{
}
