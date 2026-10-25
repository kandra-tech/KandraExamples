using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using FamilyGarage.Forms.Dictionaries;
using Refit;

namespace FamilyGarage.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/FinanceContracts")]
public interface IFinanceContractsApiClient : IDictionaryApiClient<FinanceContractDto, PagedResult<FinanceContractDto>, FinanceContractQueryDto>
{
}
