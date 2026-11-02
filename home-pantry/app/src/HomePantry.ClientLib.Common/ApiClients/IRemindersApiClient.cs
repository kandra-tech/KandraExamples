using Kandra.ClientLib.Common.ApiClients;
using HomePantry.Forms.DataProcessors;
using Refit;

namespace HomePantry.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/processors/Reminders")]
public interface IRemindersApiClient : IDataProcessorApiClient<RemindersDto, RemindersResultDto>;
