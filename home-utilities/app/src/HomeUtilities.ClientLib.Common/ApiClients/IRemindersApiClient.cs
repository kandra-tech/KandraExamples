using Kandra.ClientLib.Common.ApiClients;
using HomeUtilities.Forms.DataProcessors;
using Refit;

namespace HomeUtilities.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/processors/Reminders")]
public interface IRemindersApiClient : IDataProcessorApiClient<RemindersDto, RemindersResultDto>
{
}
