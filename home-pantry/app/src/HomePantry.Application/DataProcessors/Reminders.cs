using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Constants;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Abstractions.Services.Notifications;
using Kandra.Attributes.Application;
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
    IStringLocalizer localizer)
    : IDataProcessorBehavior<RemindersDto, RemindersResultDto>
{
    public string ObjectName => "Reminders";

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

        var chores = await repository.GetChoresAsync(now, cancellationToken);
        var dueChores = chores.Where(c => c.NextDueDate.Date <= now.Date).ToList();

        var batteries = await repository.GetBatteriesAsync(now, cancellationToken);
        var dueBatteries = batteries.Where(b => b.NextDueDate.Date <= now.Date).ToList();

        var expiring = await repository.GetExpiringAsync(lookAhead, now, cancellationToken);

        if (dueChores.Count > 0 || dueBatteries.Count > 0 || expiring.Count > 0)
        {
            var isWarning = dueChores.Any(c => c.IsOverdue) || expiring.Any(e => e.IsExpired);

            await notifications.NotifyCallerAsync(new NotificationMessage(
                Title: NotificationText.ForKey("Reminders_NotificationTitle", dueChores.Count, dueBatteries.Count, expiring.Count),
                Severity: isWarning ? NotificationSeverity.Warning : NotificationSeverity.Info,
                DeduplicationKey: $"reminders-{now:yyyyMMdd}"
            ), cancellationToken);

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
