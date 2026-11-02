using Kandra.Forms.ApiClients;
using HomePantry.Forms.Handlers;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/handlers/CreateShoppingListFromNeeds")]
public interface ICreateShoppingListFromNeedsApiClient : IHandlerApiClient<CreateShoppingListFromNeedsRequestDto, CreateShoppingListFromNeedsResponseDto>;
