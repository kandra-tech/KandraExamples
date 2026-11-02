using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Forms.Documents;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Validators.Documents;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Enums;
using PersonalAccounting.Forms.Dictionaries;

namespace PersonalAccounting.Forms.Documents;

[TypeId(PersonalAccountingTypeIds.Budget)]
[KandraDocumentForm(Name = "Budgets", ValidatorType = typeof(BudgetValidator), QueryDtoType = typeof(BudgetQueryDto))]
[EditFormTitle("EditBudget")]
[AddFormTitle("CreateBudget")]
[ViewFormTitle("ViewBudget")]
[ListFormTitle("Budgets")]
[NavigationName("Nav_Budgets")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(BudgetPrintForm))]
[Description("Plans monthly spending amounts per expense category.")]
public class BudgetDto : DocumentDto
{
    [Caption("Description")]
    [ListFormColumn]
    [Description("Optional comment or description.")]
    public string? Description { get; set; }

    [Caption("Lines")]
    [Description("Planned budget amounts per category.")]
    public ICollection<BudgetLineDto> Lines { get; set; } = [];
}

[Description("A budget line specifying an expense category and its planned monthly amount.")]
public class BudgetLineDto
{
    public Guid Id { get; set; }
    [Caption("Budget_CategoryId")]
    [Search(typeof(CategoryNodeDto))]
    [GridColumn(Order = 1, WidthPercent = 60)]
    [Description("The expense category.")]
    public Guid CategoryId { get; set; }

    [Caption("Budget_PlannedAmount")]
    [GridColumn(Order = 2, WidthPercent = 40)]
    [Description("The planned budget amount.")]
    public decimal PlannedAmount { get; set; }
}

public class BudgetQueryDto : DocumentQueryDto
{
}

public class BudgetValidator : DocumentValidator<BudgetDto>
{
    public BudgetValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.Lines).NotEmpty().WithMessage(localizer["Validation_LinesRequired"]);
        RuleForEach(x => x.Lines).Must(l => l.CategoryId != Guid.Empty).WithMessage(localizer["Validation_CategoryRequired"]);
        RuleForEach(x => x.Lines).Must(l => l.PlannedAmount > 0).WithMessage(localizer["Validation_PlannedAmountMustBePositive"]);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("BudgetPrintTitle")]
public sealed class BudgetPrintForm : DocumentPrintForm
{
    [Caption("Lines")]
    [PrintTable(LineNumbers = true)]
    public required ICollection<BudgetPrintLine> Lines { get; set; }

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = "";
}

public sealed class BudgetPrintLine
{
    [PrintColumn]
    [Caption("Budget_Category")]
    public string CategoryName { get; set; } = string.Empty;

    [PrintColumn]
    [Caption("Budget_PlannedAmount")]
    [Format("N2")]
    public decimal PlannedAmount { get; set; }
}
