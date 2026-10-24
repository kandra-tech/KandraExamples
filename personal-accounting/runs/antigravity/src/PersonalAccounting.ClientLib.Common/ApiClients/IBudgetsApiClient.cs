using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using PersonalAccounting.Forms.Documents;
using Refit;

namespace PersonalAccounting.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/documents/budgets")]
public interface IBudgetsApiClient : IDocumentApiClient<BudgetDto, PagedResult<BudgetDto>, BudgetQueryDto>;
