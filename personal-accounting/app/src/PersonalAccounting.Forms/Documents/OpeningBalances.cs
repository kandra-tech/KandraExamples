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

[TypeId(PersonalAccountingTypeIds.OpeningBalances)]
[KandraDocumentForm(Name = "OpeningBalances", ValidatorType = typeof(OpeningBalancesValidator), QueryDtoType = typeof(OpeningBalancesQueryDto))]
[EditFormTitle("EditOpeningBalances")]
[AddFormTitle("CreateOpeningBalances")]
[ViewFormTitle("ViewOpeningBalances")]
[ListFormTitle("OpeningBalances")]
[NavigationName("Nav_OpeningBalances")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(OpeningBalancesPrintForm))]
[Description("Sets opening balances across accounts as of a date, posting directly to the account balance register.")]
public class OpeningBalancesDto : DocumentDto
{
    [Caption("Description")]
    [ListFormColumn]
    [Description("Optional comment or description.")]
    public string? Description { get; set; }

    [Caption("Lines")]
    [Description("Opening balance entries per account.")]
    public ICollection<OpeningBalancesLineDto> Lines { get; set; } = [];
}

[Description("An opening balance line specifying an account and its starting balance.")]
public class OpeningBalancesLineDto
{
    public Guid Id { get; set; }
    [Caption("OpeningBalances_AccountId")]
    [Search(typeof(AccountDto))]
    [GridColumn(Order = 1, WidthPercent = 60)]
    [Description("The account for which the opening balance is set.")]
    public Guid AccountId { get; set; }

    [Caption("OpeningBalances_Amount")]
    [GridColumn(Order = 2, WidthPercent = 40)]
    [Description("The opening balance amount in the account's currency.")]
    public decimal Amount { get; set; }
}

public class OpeningBalancesQueryDto : DocumentQueryDto
{
}

public class OpeningBalancesValidator : DocumentValidator<OpeningBalancesDto>
{
    public OpeningBalancesValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.Lines).NotEmpty().WithMessage(localizer["Validation_LinesRequired"]);
        RuleForEach(x => x.Lines).Must(l => l.AccountId != Guid.Empty).WithMessage(localizer["Validation_AccountRequired"]);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("OpeningBalancesPrintTitle")]
public sealed class OpeningBalancesPrintForm : DocumentPrintForm
{
    [Caption("Lines")]
    [PrintTable(LineNumbers = true)]
    public required ICollection<OpeningBalancesPrintLine> Lines { get; set; }

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = "";
}

public sealed class OpeningBalancesPrintLine
{
    [PrintColumn]
    [Caption("OpeningBalances_Account")]
    public string AccountName { get; set; } = string.Empty;

    [PrintColumn]
    [Caption("OpeningBalances_Amount")]
    [Format("N2")]
    public decimal Amount { get; set; }
}
