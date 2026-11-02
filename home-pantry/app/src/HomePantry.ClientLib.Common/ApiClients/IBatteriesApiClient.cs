using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomePantry.Forms.Dictionaries;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/Batteries")]
public interface IBatteriesApiClient : IDictionaryApiClient<BatteryDto, PagedResult<BatteryDto>, BatteryQueryDto>;
