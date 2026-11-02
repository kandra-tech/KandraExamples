using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomePantry.Forms.Dictionaries;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/Equipment")]
public interface IEquipmentApiClient : IDictionaryApiClient<EquipmentDto, PagedResult<EquipmentDto>, EquipmentQueryDto>;
