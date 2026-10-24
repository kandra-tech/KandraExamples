using Kandra.ClientLib.Common.ApiClients;
using PersonalAccounting.Forms.DataProcessors;
using Refit;

namespace PersonalAccounting.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/processors/CompanySettings")]
public interface ICompanySettingsApiClient : IDataProcessorApiClient<CompanySettingsDto, CompanySettingsResult>
{
}
