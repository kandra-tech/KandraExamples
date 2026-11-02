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

[TypeId(PersonalAccountingTypeIds.Transfer)]
[KandraDocumentForm(Name = "Transfers", ValidatorType = typeof(TransferValidator), QueryDtoType = typeof(TransferQueryDto))]
[EditFormTitle("EditTransfer")]
[AddFormTitle("CreateTransfer")]
[ViewFormTitle("ViewTransfer")]
[ListFormTitle("Transfers")]
[NavigationName("Nav_Transfers")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(TransferPrintForm))]
[Description("Transfers funds between two accounts, optionally across different currencies.")]
public class TransferDto : DocumentDto
{
    [Caption("Transfer_FromAccountId")]
    [Search(typeof(AccountDto))]
    [ListFormColumn]
    [Description("The source account from which funds are deducted.")]
    public Guid FromAccountId { get; set; }

    [Caption("Transfer_ToAccountId")]
    [Search(typeof(AccountDto))]
    [ListFormColumn]
    [Description("The target account to which funds are deposited.")]
    public Guid ToAccountId { get; set; }

    [Caption("Transfer_AmountOut")]
    [ListFormColumn]
    [Description("Amount deducted from the source account.")]
    public decimal AmountOut { get; set; }

    [Caption("Transfer_AmountIn")]
    [ListFormColumn]
    [Description("Amount deposited to the target account.")]
    public decimal AmountIn { get; set; }

    [Caption("Transfer_Comment")]
    [ListFormColumn]
    [Description("User comment or note.")]
    public string? Comment { get; set; }
}

public class TransferQueryDto : DocumentQueryDto
{
}

public class TransferValidator : DocumentValidator<TransferDto>
{
    public TransferValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.FromAccountId).NotEmpty().WithMessage(localizer["Validation_FromAccountRequired"]);
        RuleFor(x => x.ToAccountId).NotEmpty().WithMessage(localizer["Validation_ToAccountRequired"]);
        RuleFor(x => x).Must(x => x.FromAccountId != x.ToAccountId).WithMessage(localizer["Validation_AccountsMustDiffer"]);
        RuleFor(x => x.AmountOut).GreaterThan(0).WithMessage(localizer["Validation_AmountMustBePositive"]);
        RuleFor(x => x.AmountIn).GreaterThan(0).WithMessage(localizer["Validation_AmountMustBePositive"]);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("TransferPrintTitle")]
public sealed class TransferPrintForm : DocumentPrintForm
{
    [Caption("Transfer_FromAccount")]
    public string FromAccountName { get; set; } = string.Empty;

    [Caption("Transfer_ToAccount")]
    public string ToAccountName { get; set; } = string.Empty;

    [Caption("Transfer_AmountOut")]
    [Format("N2")]
    public decimal AmountOut { get; set; }

    [Caption("Transfer_AmountIn")]
    [Format("N2")]
    public decimal AmountIn { get; set; }

    [Caption("Transfer_Comment")]
    public string? Comment { get; set; }
}
