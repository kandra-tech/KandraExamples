using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using PersonalAccounting.Forms.Documents;
using Refit;

namespace PersonalAccounting.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/documents/expenses")]
public interface IExpensesApiClient : IDocumentApiClient<ExpenseDto, PagedResult<ExpenseDto>, ExpenseQueryDto>;
