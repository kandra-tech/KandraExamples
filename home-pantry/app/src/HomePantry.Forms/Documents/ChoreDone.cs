using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Documents;
using Kandra.Forms.Querying;
using Kandra.Validators.Documents;
using HomePantry.Enums;
using HomePantry.Forms.Dictionaries;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Documents;

[TypeId(HomePantryTypeIds.ChoreDone)]
[KandraDocumentForm(Name = "ChoresDone", ValidatorType = typeof(ChoreDoneValidator), QueryDtoType = typeof(ChoreDoneQueryDto))]
[EditFormTitle("EditChoreDone")]
[AddFormTitle("CreateChoreDone")]
[ViewFormTitle("ViewChoreDone")]
[ListFormTitle("ChoresDone")]
[NavigationName("Nav_ChoresDone")]
[Description("Records the completion of a household chore on a specific date by a household member.")]
public class ChoreDoneDto : DocumentDto
{
    [Caption("Chore")]
    [Search(typeof(ChoreDto))]
    [ListFormColumn]
    [Description("The chore that was completed.")]
    public Guid ChoreId { get; set; }

    [Caption("DoneBy")]
    [Search(typeof(HouseholdMemberDto))]
    [ListFormColumn]
    [Description("The household member who completed the chore.")]
    public Guid DoneById { get; set; }

    [Caption("Comment")]
    [Description("Optional notes or comments about the chore completion.")]
    public string? Comment { get; set; }
}

public class ChoreDoneQueryDto : DocumentQueryDto
{
}

public class ChoreDoneValidator : DocumentValidator<ChoreDoneDto>
{
    public ChoreDoneValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.ChoreId)
            .NotEmpty()
            .WithMessage(localizer["ChoreDone_ChoreRequired"].Value);

        RuleFor(x => x.DoneById)
            .NotEmpty()
            .WithMessage(localizer["ChoreDone_DoneByRequired"].Value);
    }
}
