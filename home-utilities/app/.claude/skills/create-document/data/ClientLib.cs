// TEMPLATE — document Refit client interface (hand-written, no generator for this piece yet).
// Target: {{Config}}.ClientLib.Common/ApiClients/I{{PluralName}}ApiClient.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §11.
// Same file shape regardless of whether the document has lines — IDocumentApiClient<> doesn't vary by that.

using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using {{Config}}.Forms.Documents;
using Refit;

namespace {{Config}}.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/documents/{{PluralName}}")]
public interface I{{PluralName}}ApiClient : IDocumentApiClient<{{Name}}Dto, PagedResult<{{Name}}Dto>, {{Name}}QueryDto>
{
    // Nothing to add for the common case — IDocumentApiClient<> already supplies the full CRUD +
    // document-lifecycle set (AllAsync/QueryAsync/ReadAsync/InsertAsync/UpdateAsync/DeleteAsync/
    // GetChangeStateAsync/NewAsync/LookupAsync/SubmitAsync/UnsubmitAsync/PrintAsync/
    // GetRegisterTransactionsAsync/GetAccountTransactionsAsync/GetLinksAsync). Only declare a method
    // here if this document's generated controller exposes an endpoint beyond that shared contract.
}
