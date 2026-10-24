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

[TypeId(PersonalAccountingTypeIds.Expense)]
[KandraDocumentForm(Name = "Expenses", ValidatorType = typeof(ExpenseValidator), QueryDtoType = typeof(ExpenseQueryDto))]
[EditFormTitle("EditExpense")]
[AddFormTitle("CreateExpense")]
[ViewFormTitle("ViewExpense")]
[ListFormTitle("Expenses")]
[NavigationName("Nav_Expenses")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(ExpensePrintForm))]
[Description("Records spending from an account under an expense category.")]
public class ExpenseDto : DocumentDto
{
    [Caption("Expense_AccountId")]
    [Search(typeof(AccountDto))]
    [ListFormColumn]
    [Description("The account from which the expense is paid.")]
    public Guid AccountId { get; set; }

    [Caption("Expense_CategoryId")]
    [Search(typeof(CategoryNodeDto))]
    [ListFormColumn]
    [Description("The expense category.")]
    public Guid CategoryId { get; set; }

    [Caption("Expense_Amount")]
    [ListFormColumn]
    [Description("The expense amount in the account's currency.")]
    public decimal Amount { get; set; }

    [Caption("Expense_Comment")]
    [ListFormColumn]
    [Description("User comment or note.")]
    public string? Comment { get; set; }
}

public class ExpenseQueryDto : DocumentQueryDto
{
}

public class ExpenseValidator : DocumentValidator<ExpenseDto>
{
    public ExpenseValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage(localizer["Validation_AccountRequired"]);
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage(localizer["Validation_CategoryRequired"]);
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage(localizer["Validation_AmountMustBePositive"]);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("ExpensePrintTitle")]
public sealed class ExpensePrintForm : DocumentPrintForm
{
    [Caption("Expense_Account")]
    public string AccountName { get; set; } = string.Empty;

    [Caption("Expense_Category")]
    public string CategoryName { get; set; } = string.Empty;

    [Caption("Expense_Comment")]
    public string? Comment { get; set; }

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = "";
}
