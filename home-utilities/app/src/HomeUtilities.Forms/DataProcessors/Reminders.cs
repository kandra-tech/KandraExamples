using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Naming;
using Kandra.Forms.DataProcessors;

namespace HomeUtilities.Forms.DataProcessors;

[KandraDataProcessorForm(Name = "Reminders", ValidatorType = typeof(RemindersValidator), ResultDtoType = typeof(RemindersResultDto))]
[ExecuteFormTitle("Reminders")]
[Description("Daily scheduled job that checks expected readings and verification deadlines, sending in-app notifications.")]
public class RemindersDto : IDataProcessorInputDto
{
    [Caption("Reminders_AsOfDate")]
    [DateEditor(DateEditorKind.Date)]
    [Description("Optional date to run reminders for; defaults to today.")]
    public DateTime? AsOfDate { get; set; }
}

public class RemindersResultDto : IDataProcessorResultDto
{
    [Caption("Reminders_SentCount")]
    [Description("Number of notifications sent.")]
    public int SentNotificationsCount { get; set; }

    [Caption("Reminders_MissingReadingsMeters")]
    [Description("Number of meters missing readings.")]
    public int MissingReadingsCount { get; set; }

    [Caption("Reminders_VerificationDueMeters")]
    [Description("Number of meters with verification due in 30 days.")]
    public int VerificationDueCount { get; set; }
}

public class RemindersValidator : AbstractValidator<RemindersDto>
{
    public RemindersValidator()
    {
    }
}
