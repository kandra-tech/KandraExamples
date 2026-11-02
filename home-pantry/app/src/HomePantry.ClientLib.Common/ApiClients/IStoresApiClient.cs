using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomePantry.Forms.Dictionaries;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/Stores")]
public interface IStoresApiClient : IDictionaryApiClient<StoreDto, PagedResult<StoreDto>, StoreQueryDto>;
