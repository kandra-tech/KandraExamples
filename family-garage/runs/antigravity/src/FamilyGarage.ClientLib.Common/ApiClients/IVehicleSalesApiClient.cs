using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using FamilyGarage.Forms.Documents;
using Refit;

namespace FamilyGarage.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/documents/VehicleSales")]
public interface IVehicleSalesApiClient : IDocumentApiClient<VehicleSaleDto, PagedResult<VehicleSaleDto>, VehicleSaleQueryDto>
{
}
