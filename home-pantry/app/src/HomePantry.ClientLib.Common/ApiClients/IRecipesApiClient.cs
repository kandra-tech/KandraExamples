using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomePantry.Forms.Dictionaries;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/Recipes")]
public interface IRecipesApiClient : IDictionaryApiClient<RecipeDto, PagedResult<RecipeDto>, RecipeQueryDto>;
