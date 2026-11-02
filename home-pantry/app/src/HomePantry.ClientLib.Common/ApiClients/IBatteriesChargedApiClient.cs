using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomePantry.Forms.Documents;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/documents/BatteriesCharged")]
public interface IBatteriesChargedApiClient : IDocumentApiClient<BatteryChargedDto, PagedResult<BatteryChargedDto>, BatteryChargedQueryDto>;
