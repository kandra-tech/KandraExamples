// TEMPLATE — hierarchical dictionary, Refit client interface (hand-written, no generator for this piece yet).
// Target: {{Config}}.ClientLib.Common/ApiClients/I{{PluralName}}ApiClient.cs
// Bind to the abstract ROOT Dto ({{Name}}NodeDto), not the leaf — same rule as the server-side
// [KandraDictionaryForm] placement (reference.md §9).
// Placeholder legend: data/README.md. Concept reference: ../reference.md §10.

using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using {{Config}}.Forms.Dictionaries;
using Refit;

namespace {{Config}}.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/{{PluralName}}")]
public interface I{{PluralName}}ApiClient : IHierarchicalDictionaryApiClient<{{Name}}NodeDto, PagedResult<{{Name}}NodeDto>, {{Name}}QueryDto>
{
    // Nothing to add for the common case — IHierarchicalDictionaryApiClient<> already supplies the full
    // CRUD set (AllAsync/QueryAsync/ReadAsync/InsertAsync/UpdateAsync/DeleteAsync/GetChangeStateAsync) plus
    // NewAsync(parentId, isFolder)/LookupAsync/MoveToAsync. Only declare a method here if this dictionary's
    // generated controller exposes an endpoint beyond that shared contract.
}
