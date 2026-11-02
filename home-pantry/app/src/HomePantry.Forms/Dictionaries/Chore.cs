using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Forms.UiBehaviors;
using Kandra.Validators.Dictionaries;
using HomePantry.Enums;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Dictionaries;

[TypeId(HomePantryTypeIds.Chore)]
[KandraDictionaryForm(Name = "Chores", ValidatorType = typeof(ChoreValidator), QueryDtoType = typeof(ChoreQueryDto))]
[UiBehavior(typeof(ChoreUiBehavior))]
[EditFormTitle("EditChore")]
[AddFormTitle("CreateChore")]
[ViewFormTitle("ViewChore")]
[ListFormTitle("Chores")]
[NavigationName("Nav_Chores")]
[Description("A recurring household chore with recurrence schedule and assignment.")]
public class ChoreDto : DictionaryDto
{
    [Caption("Recurrence")]
    [ListFormColumn]
    [Description("How frequently this chore recurs.")]
    public ChoreRecurrence Recurrence { get; set; } = ChoreRecurrence.Daily;

    [Caption("EveryNDays")]
    [Visible(nameof(ChoreUiBehavior.ShowEveryNDays))]
    [Description("Number of days between recurrences when recurrence is EveryNDays.")]
    public int EveryNDays { get; set; } = 1;

    [Caption("AssignedTo")]
    [Search(typeof(HouseholdMemberDto))]
    [Description("Household member assigned to perform this chore.")]
    public Guid? AssignedToId { get; set; }

    [Caption("Description")]
    [Description("Detailed instructions or description of the chore.")]
    public string? Description { get; set; }
}

public class ChoreQueryDto : DictionaryQueryDto
{
}

public sealed class ChoreUiBehavior : DictionaryUiBehavior<ChoreDto>
{
    public bool ShowEveryNDays() => Record.Recurrence == ChoreRecurrence.EveryNDays;
}

public class ChoreValidator : DictionaryValidator<ChoreDto>
{
    public ChoreValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.EveryNDays)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.Recurrence == ChoreRecurrence.EveryNDays)
            .WithMessage(localizer["Chore_EveryNDaysRequired"].Value);
    }
}
