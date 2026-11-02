using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.DataProcessors;

namespace FamilyGarage.Forms.DataProcessors;

[KandraDataProcessorForm(Name = "UpcomingReminders", ResultDtoType = typeof(UpcomingRemindersResult), ValidatorType = typeof(UpcomingRemindersValidator))]
[ExecuteFormTitle("UpcomingReminders")]
[NavigationName("Nav_UpcomingReminders")]
[Description("Scans for due or overdue vehicle maintenance, expiring policies, and upcoming loan payments, sending in-app notifications.")]
public sealed class UpcomingRemindersDto : IDataProcessorInputDto
{
    [Caption("LookAheadDays")]
    [Description("How many days ahead to scan for due events.")]
    public int LookAheadDays { get; set; } = 30;

    [Caption("LookAheadKm")]
    [Description("How many kilometres ahead to scan for due service items.")]
    public int LookAheadKm { get; set; } = 1000;
}

[Description("Result of running upcoming reminders check.")]
public sealed class UpcomingRemindersResult : IDataProcessorResultDto
{
    [Caption("NotificationsSent")]
    [Description("Number of new in-app notifications created.")]
    public int NotificationsSent { get; set; }

    [Caption("TotalEventsFound")]
    [Description("Total number of due or overdue events detected.")]
    public int TotalEventsFound { get; set; }
}

public class UpcomingRemindersValidator : AbstractValidator<UpcomingRemindersDto>
{
    public UpcomingRemindersValidator()
    {
        RuleFor(x => x.LookAheadDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LookAheadKm).GreaterThanOrEqualTo(0);
    }
}
