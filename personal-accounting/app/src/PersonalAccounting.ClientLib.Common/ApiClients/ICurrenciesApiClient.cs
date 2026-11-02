using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using PersonalAccounting.Forms.Dictionaries;
using Refit;

namespace PersonalAccounting.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/currencies")]
public interface ICurrenciesApiClient : IDictionaryApiClient<CurrencyDto, PagedResult<CurrencyDto>, CurrencyQueryDto>;
