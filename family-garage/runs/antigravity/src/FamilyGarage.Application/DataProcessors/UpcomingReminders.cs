using System.Globalization;
using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Constants;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Abstractions.Services.Notifications;
using Kandra.Attributes.Application;
using Kandra.Domain.Notifications;
using Kandra.Persistence.Abstractions;
using FamilyGarage.Application.Constants;
using FamilyGarage.Domain.Commons;
using FamilyGarage.Domain.Entities.ReportingModels;
using FamilyGarage.Domain.Reporting;
using FamilyGarage.Enums;
using FamilyGarage.Forms.DataProcessors;
using NotificationMessage = Kandra.Application.Abstractions.Services.Notifications.Notification;

namespace FamilyGarage.Application.DataProcessors;

[KandraDataProcessorBehavior(typeof(UpcomingRemindersDto))]
public class UpcomingRemindersBehavior(
    IReportsRepository repository,
    IConstantsService constants,
    INotificationService notifications,
    IUnitOfWork unitOfWork,
    ICurrentTimeZone timeZone)
    : IDataProcessorBehavior<UpcomingRemindersDto, UpcomingRemindersResult>
{
    public string ObjectName => "UpcomingReminders";

    public async ValueTask OnNewAsync(UpcomingRemindersDto input, CancellationToken cancellationToken)
    {
        input.LookAheadDays = (await constants.GetAsync<UpcomingDaysAheadConstant>(DateTime.UtcNow, cancellationToken)).Value;
        input.LookAheadKm = (await constants.GetAsync<UpcomingKmAheadConstant>(DateTime.UtcNow, cancellationToken)).Value;
    }

    public async Task<UpcomingRemindersResult> OnProcessAsync(UpcomingRemindersDto dto, CancellationToken cancellationToken)
    {
        var zone = timeZone.TimeZone;
        var items = await repository.GetUpcomingItemsAsync(
            dto.LookAheadDays,
            dto.LookAheadKm,
            null,
            LocalCalendar.LocalDate(DateTime.UtcNow, zone),
            zone,
            cancellationToken);

        int sentCount = 0;
        foreach (var item in items)
        {
            var sent = await notifications.NotifyCallerAsync(Reminder(item), cancellationToken);
            if (sent > 0)
            {
                sentCount++;
            }
        }

        await unitOfWork.SaveAsync(cancellationToken);

        return new UpcomingRemindersResult
        {
            TotalEventsFound = items.Count,
            NotificationsSent = sentCount
        };
    }

    /// <summary>
    /// The title and the body share one list of arguments (the title's), so both keys read the same four: the vehicle, the
    /// work item or contract, the due date and the due kilometres. The words come from the keys in the reader's language;
    /// the arguments are data only. The key of the item (it names the occurrence) makes a re-run on the same day send nothing.
    /// </summary>
    private static NotificationMessage Reminder(UpcomingItem item)
    {
        var titleKey = item.Kind switch
        {
            UpcomingEventKind.Policy => $"UpcomingReminder_Policy_{item.PolicyKind}_Title",
            UpcomingEventKind.Payment => "UpcomingReminder_Payment_Title",
            _ => "UpcomingReminder_Work_Title",
        };
        var due = (item.DueDate.HasValue, item.DueKm.HasValue) switch
        {
            (true, true) => "DateKm",
            (false, true) => "Km",
            _ => "Date",
        };
        var bodyKey = $"UpcomingReminder_{(item.IsOverdue ? "Overdue" : "Due")}_{due}";

        return new NotificationMessage(
            Title: NotificationText.ForKey(
                titleKey,
                item.VehicleName,
                item.Subject ?? string.Empty,
                item.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                item.DueKm?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
            Body: NotificationText.ForKey(bodyKey),
            Severity: item.IsOverdue ? NotificationSeverity.Warning : NotificationSeverity.Info,
            DeduplicationKey: $"upcoming:{item.VehicleId}:{item.Key}");
    }
}
