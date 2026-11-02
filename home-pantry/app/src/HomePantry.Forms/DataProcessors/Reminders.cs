using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Naming;
using Kandra.Forms.DataProcessors;

namespace HomePantry.Forms.DataProcessors;

[KandraDataProcessorForm(Name = "Reminders", ValidatorType = typeof(RemindersValidator), ResultDtoType = typeof(RemindersResultDto))]
[ExecuteFormTitle("Reminders")]
[Description("Sends daily notifications for chores and batteries that became due, and for stock batches expiring soon.")]
public class RemindersDto : IDataProcessorInputDto
{
}

[Description("Result of running daily reminders.")]
public class RemindersResultDto : IDataProcessorResultDto
{
    [Caption("Reminders_Message")]
    public string Message { get; set; } = string.Empty;

    [Caption("Reminders_DueChoresCount")]
    public int DueChoresCount { get; set; }

    [Caption("Reminders_DueBatteriesCount")]
    public int DueBatteriesCount { get; set; }

    [Caption("Reminders_ExpiringBatchesCount")]
    public int ExpiringBatchesCount { get; set; }
}

public class RemindersValidator : AbstractValidator<RemindersDto>
{
}
