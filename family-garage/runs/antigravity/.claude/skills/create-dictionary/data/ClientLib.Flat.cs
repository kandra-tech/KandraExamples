// TEMPLATE — flat dictionary, Refit client interface (hand-written, no generator for this piece yet).
// Target: {{Config}}.ClientLib.Common/ApiClients/I{{PluralName}}ApiClient.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §10.

using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using {{Config}}.Forms.Dictionaries;
using Refit;

namespace {{Config}}.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/{{PluralName}}")]
public interface I{{PluralName}}ApiClient : IDictionaryApiClient<{{Name}}Dto, PagedResult<{{Name}}Dto>, {{Name}}QueryDto>
{
    // Nothing to add for the common case — IDictionaryApiClient<> already supplies the full CRUD set
    // (AllAsync/QueryAsync/ReadAsync/InsertAsync/UpdateAsync/DeleteAsync/GetChangeStateAsync) plus
    // NewAsync/LookupAsync. Only declare a method here if this dictionary's generated controller exposes
    // an endpoint beyond that shared contract.
}
