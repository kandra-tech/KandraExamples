using Kandra.ClientLib.Common.ApiClients;
using {{Config}}.Forms.DataProcessors;
using Refit;

namespace {{Config}}.ClientLib.Common.ApiClients;

// Thin — everything is on the shared IDataProcessorApiClient<TInput,TResult> base (reference.md §7). Refit's
// own source generator implements the interface body at compile time; you only ever write this declaration.
[PathPrefix("/api/v1/processors/{{Name}}")]
public interface I{{Name}}ApiClient : IDataProcessorApiClient<{{Name}}Dto, {{Name}}Result>
{
}
