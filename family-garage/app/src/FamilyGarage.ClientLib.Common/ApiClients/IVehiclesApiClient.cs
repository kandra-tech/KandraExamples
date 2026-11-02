using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using FamilyGarage.Forms.Dictionaries;
using Refit;

namespace FamilyGarage.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/Vehicles")]
public interface IVehiclesApiClient : IDictionaryApiClient<VehicleDto, PagedResult<VehicleDto>, VehicleQueryDto>
{
}
