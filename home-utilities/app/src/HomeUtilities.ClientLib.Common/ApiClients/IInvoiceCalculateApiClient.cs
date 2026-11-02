using Kandra.Forms.ApiClients;
using HomeUtilities.Forms.Handlers;
using Refit;

namespace HomeUtilities.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/handlers/InvoiceCalculate")]
public interface IInvoiceCalculateApiClient : IHandlerApiClient<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto>
{
}
