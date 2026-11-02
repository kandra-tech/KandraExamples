using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Constants;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Abstractions.Services.Notifications;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Domain.Enums;
using Kandra.Domain.Notifications;
using Kandra.Persistence.Abstractions;
using HomePantry.Application.Constants;
using HomePantry.Domain.Commons;
using HomePantry.Forms.DataProcessors;
using Microsoft.Extensions.Localization;
using NotificationMessage = Kandra.Application.Abstractions.Services.Notifications.Notification;

namespace HomePantry.Application.DataProcessors;

[KandraDataProcessorBehavior(typeof(RemindersDto))]
public class RemindersBehavior(
    IReportsRepository repository,
    IConstantsService constants,
    INotificationService notifications,
    IUnitOfWork unitOfWork,
    IStringLocalizer localizer,
    ICurrentTimeZone timeZone)
    : IDataProcessorBehavior<RemindersDto, RemindersResultDto>
{
    public string ObjectName => "Reminders";

    /// <summary>The permission to run the reminders, which every household user has.</summary>
    private static readonly IAuthorizationPoint HouseholdPoint = new RemindersPoint();

    private sealed record RemindersPoint : IAuthorizationPoint
    {
        public ApplicationArea Area { get; init; } = ApplicationArea.DataProcessor;
        public string ObjectName { get; init; } = "Reminders";
        public ApplicationAction Action { get; init; } = ApplicationAction.Execute;
    }

    public ValueTask OnNewAsync(RemindersDto input, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public async Task<RemindersResultDto> OnProcessAsync(RemindersDto input, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var lookAheadConst = await constants.GetAsync<ExpiringLookAheadDaysConstant>(now, cancellationToken);
        var lookAhead = lookAheadConst?.Value ?? 5;
        if (lookAhead <= 0) lookAhead = 5;

        var zone = timeZone.TimeZone;
        var today = LocalCalendar.Today(now, zone);

        // Due today or earlier, in the user's calendar days (NextDueDate is local midnight).
        var chores = await repository.GetChoresAsync(now, zone, cancellationToken);
        var dueChores = chores.Where(c => LocalCalendar.DateOf(c.NextDueDate, zone) <= today).ToList();

        var batteries = await repository.GetBatteriesAsync(now, zone, cancellationToken);
        var dueBatteries = batteries.Where(b => LocalCalendar.DateOf(b.NextDueDate, zone) <= today).ToList();

        var expiring = await repository.GetExpiringAsync(lookAhead, now, zone, cancellationToken);

        if (dueChores.Count > 0 || dueBatteries.Count > 0 || expiring.Count > 0)
        {
            var isWarning = dueChores.Any(c => c.IsOverdue) || expiring.Any(e => e.IsExpired);

            var reminder = new NotificationMessage(
                Title: NotificationText.ForKey("Reminders_NotificationTitle", dueChores.Count, dueBatteries.Count, expiring.Count),
                Severity: isWarning ? NotificationSeverity.Warning : NotificationSeverity.Info,
                DeduplicationKey: $"reminders-{today:yyyyMMdd}");

            // The household, not only whoever runs the job: the administrators, then every user who may run the
            // reminders. Saving in between lets the de-duplication key skip an administrator who is both.
            var audience = new AudienceOptions { IncludeSender = true };
            await notifications.NotifyAdminsAsync(reminder, audience, cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            await notifications.NotifyWithPermissionAsync(HouseholdPoint, reminder, audience, cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
        }

        return new RemindersResultDto
        {
            DueChoresCount = dueChores.Count,
            DueBatteriesCount = dueBatteries.Count,
            ExpiringBatchesCount = expiring.Count,
            Message = localizer["Reminders_SummaryMessage", dueChores.Count, dueBatteries.Count, expiring.Count].Value
        };
    }
}
