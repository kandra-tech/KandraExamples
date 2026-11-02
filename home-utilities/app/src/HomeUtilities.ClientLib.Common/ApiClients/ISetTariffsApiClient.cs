using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomeUtilities.Forms.Documents;
using Refit;

namespace HomeUtilities.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/documents/SetTariffs")]
public interface ISetTariffsApiClient : IDocumentApiClient<SetTariffsDto, PagedResult<SetTariffsDto>, SetTariffsQueryDto>
{
}
