using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Constants;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Abstractions.Services.Notifications;
using Kandra.Attributes.Application;
using Kandra.Domain.Notifications;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using HomeUtilities.Application.Common;
using HomeUtilities.Application.Constants;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Domain.Entities.Registers;
using HomeUtilities.Forms.DataProcessors;
using NotificationMessage = Kandra.Application.Abstractions.Services.Notifications.Notification;

namespace HomeUtilities.Application.DataProcessors;

[KandraDataProcessorBehavior(typeof(RemindersDto))]
public class RemindersBehavior(
    IConstantsService constants,
    IEntityRepository<Meter> metersRepo,
    IInfoRegisterReader<MeterReadingRecord> readingsReader,
    INotificationService notifications,
    IUnitOfWork unitOfWork,
    ICurrentTimeZone? currentTimeZone = null,
    TimeProvider? time = null)
    : IDataProcessorBehavior<RemindersDto, RemindersResultDto>
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public string ObjectName => "Reminders";

    public Task OnNewAsync(RemindersDto dto, CancellationToken cancellationToken)
    {
        dto.AsOfDate = TimeZoneHelper.ToLocalDate(_time.GetUtcNow().UtcDateTime, currentTimeZone?.TimeZone);
        return Task.CompletedTask;
    }

    public async Task<RemindersResultDto> OnProcessAsync(RemindersDto input, CancellationToken cancellationToken)
    {
        var today = input.AsOfDate is { } asOf
            ? asOf.Date
            : TimeZoneHelper.ToLocalDate(_time.GetUtcNow().UtcDateTime, currentTimeZone?.TimeZone);

        var expectedDayConst = await constants.GetAsync<ExpectedReadingsDayConstant>(today, cancellationToken);
        var expectedDay = expectedDayConst?.Value ?? 25;

        var allMeters = new List<Meter>();
        await foreach (var m in metersRepo.GetAllAsync(q => q.Where(x => !x.IsDeleted)))
        {
            allMeters.Add(m);
        }

        var sentCount = 0;
        var missingCount = 0;
        var verificationDueCount = 0;

        // 1. Expected readings day check
        if (today.Day == expectedDay)
        {
            var (monthStartUtc, _) = TimeZoneHelper.LocalMonthBoundsUtc(new DateTime(today.Year, today.Month, 1), currentTimeZone?.TimeZone);
            var todayEndUtc = TimeZoneHelper.EndOfLocalDayUtc(today, currentTimeZone?.TimeZone);
            var readings = await readingsReader.Records(monthStartUtc, todayEndUtc, null, cancellationToken);
            var metersWithReadings = readings.Select(r => r.Dimensions.MeterId).ToHashSet();

            foreach (var meter in allMeters)
            {
                if (!metersWithReadings.Contains(meter.Id))
                {
                    missingCount++;
                }
            }

            if (missingCount > 0)
            {
                await notifications.NotifyCallerAsync(new NotificationMessage(
                    Title: NotificationText.ForKey("Reminder_MissingReadings_Title", missingCount),
                    Body: NotificationText.ForKey("Reminder_MissingReadings_Body", missingCount),
                    Severity: NotificationSeverity.Warning,
                    DeduplicationKey: $"reminders:missing-readings:{today:yyyy-MM-dd}"), cancellationToken);
                sentCount++;
            }
        }

        // 2. Verification date 30 days away check
        var targetDate = today.AddDays(30);
        foreach (var meter in allMeters)
        {
            if (meter.NextVerificationDate.Date == targetDate)
            {
                verificationDueCount++;
                var dueStr = meter.NextVerificationDate.ToString("yyyy-MM-dd");
                await notifications.NotifyCallerAsync(new NotificationMessage(
                    Title: NotificationText.ForKey("Reminder_VerificationDue_Title", meter.Name, dueStr),
                    Body: NotificationText.ForKey("Reminder_VerificationDue_Body", meter.Name, dueStr),
                    Severity: NotificationSeverity.Warning,
                    DeduplicationKey: $"reminders:verification:{meter.Id:N}:{dueStr}"), cancellationToken);
                sentCount++;
            }
        }

        if (sentCount > 0)
        {
            await unitOfWork.SaveAsync(cancellationToken);
        }

        return new RemindersResultDto
        {
            SentNotificationsCount = sentCount,
            MissingReadingsCount = missingCount,
            VerificationDueCount = verificationDueCount,
        };
    }
}
