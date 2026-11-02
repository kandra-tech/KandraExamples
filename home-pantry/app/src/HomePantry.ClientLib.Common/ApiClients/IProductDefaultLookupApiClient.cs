using Kandra.Forms.ApiClients;
using HomePantry.Forms.Handlers;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/handlers/ProductDefaultLookup")]
public interface IProductDefaultLookupApiClient : IHandlerApiClient<ProductDefaultLookupRequestDto, ProductDefaultLookupResponseDto>;
