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

[TypeId(PersonalAccountingTypeIds.Income)]
[KandraDocumentForm(Name = "Income", ValidatorType = typeof(IncomeValidator), QueryDtoType = typeof(IncomeQueryDto))]
[EditFormTitle("EditIncome")]
[AddFormTitle("CreateIncome")]
[ViewFormTitle("ViewIncome")]
[ListFormTitle("Income")]
[NavigationName("Nav_Income")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(IncomePrintForm))]
[Description("Records incoming money to an account under an income category.")]
public class IncomeDto : DocumentDto
{
    [Caption("Income_AccountId")]
    [Search(typeof(AccountDto))]
    [ListFormColumn]
    [Description("The account receiving the income.")]
    public Guid AccountId { get; set; }

    [Caption("Income_CategoryId")]
    [Search(typeof(CategoryNodeDto))]
    [ListFormColumn]
    [Description("The income category.")]
    public Guid CategoryId { get; set; }

    [Caption("Income_Amount")]
    [ListFormColumn]
    [Description("The income amount in the account's currency.")]
    public decimal Amount { get; set; }

    [Caption("Income_Comment")]
    [ListFormColumn]
    [Description("User comment or note.")]
    public string? Comment { get; set; }
}

public class IncomeQueryDto : DocumentQueryDto
{
}

public class IncomeValidator : DocumentValidator<IncomeDto>
{
    public IncomeValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage(localizer["Validation_AccountRequired"]);
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage(localizer["Validation_CategoryRequired"]);
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage(localizer["Validation_AmountMustBePositive"]);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("IncomePrintTitle")]
public sealed class IncomePrintForm : DocumentPrintForm
{
    [Caption("Income_Account")]
    public string AccountName { get; set; } = string.Empty;

    [Caption("Income_Category")]
    public string CategoryName { get; set; } = string.Empty;

    [Caption("Income_Comment")]
    public string? Comment { get; set; }

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = "";
}
