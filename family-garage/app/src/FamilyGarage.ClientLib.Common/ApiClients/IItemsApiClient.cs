using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using FamilyGarage.Forms.Dictionaries;
using Refit;

namespace FamilyGarage.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/Items")]
public interface IItemsApiClient : IHierarchicalDictionaryApiClient<ItemNodeDto, PagedResult<ItemNodeDto>, ItemQueryDto>
{
}
