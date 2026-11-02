using Kandra.ClientLib.Common.ApiClients;
using Kandra.Forms.Paging;
using HomeUtilities.Forms.Documents;
using Refit;

namespace HomeUtilities.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/documents/MeterReadings")]
public interface IMeterReadingsApiClient : IDocumentApiClient<MeterReadingDto, PagedResult<MeterReadingDto>, MeterReadingQueryDto>
{
}
