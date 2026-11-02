using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomeUtilities.Forms.Dictionaries;
using Refit;

namespace HomeUtilities.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/Properties")]
public interface IPropertiesApiClient : IDictionaryApiClient<PropertyDto, PagedResult<PropertyDto>, PropertyQueryDto>
{
}
