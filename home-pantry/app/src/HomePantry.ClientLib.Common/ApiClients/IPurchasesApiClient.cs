using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomePantry.Forms.Documents;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/documents/Purchases")]
public interface IPurchasesApiClient : IDocumentApiClient<PurchaseDto, PagedResult<PurchaseDto>, PurchaseQueryDto>;
