using Kandra.ClientLib.Common.ApiClients;
using HomeUtilities.Forms.DataProcessors;
using Refit;

namespace HomeUtilities.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/processors/CompanySettings")]
public interface ICompanySettingsApiClient : IDataProcessorApiClient<CompanySettingsDto, CompanySettingsResult>
{
}
