using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomeUtilities.Forms.Documents;
using Refit;

namespace HomeUtilities.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/documents/Payments")]
public interface IPaymentsApiClient : IDocumentApiClient<PaymentDto, PagedResult<PaymentDto>, PaymentQueryDto>
{
}
