using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomePantry.Forms.Dictionaries;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/Locations")]
public interface ILocationsApiClient : IHierarchicalDictionaryApiClient<LocationNodeDto, PagedResult<LocationNodeDto>, LocationQueryDto>;
