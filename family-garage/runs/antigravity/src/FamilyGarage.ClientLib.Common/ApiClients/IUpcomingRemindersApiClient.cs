using Kandra.ClientLib.Common.ApiClients;
using FamilyGarage.Forms.DataProcessors;
using Refit;

namespace FamilyGarage.ClientLib.Common.ApiClients;

[PathPrefix("/api/v1/processors/UpcomingReminders")]
public interface IUpcomingRemindersApiClient : IDataProcessorApiClient<UpcomingRemindersDto, UpcomingRemindersResult>
{
}
