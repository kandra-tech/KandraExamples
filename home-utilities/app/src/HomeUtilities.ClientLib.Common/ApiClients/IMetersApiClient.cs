using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomeUtilities.Forms.Dictionaries;
using Refit;

namespace HomeUtilities.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/dictionaries/Meters")]
public interface IMetersApiClient : IDictionaryApiClient<MeterDto, PagedResult<MeterDto>, MeterQueryDto>
{
}
