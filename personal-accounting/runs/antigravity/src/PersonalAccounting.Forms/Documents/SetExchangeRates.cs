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

[TypeId(PersonalAccountingTypeIds.SetExchangeRates)]
[KandraDocumentForm(Name = "SetExchangeRates", ValidatorType = typeof(SetExchangeRatesValidator), QueryDtoType = typeof(SetExchangeRatesQueryDto))]
[EditFormTitle("EditSetExchangeRates")]
[AddFormTitle("CreateSetExchangeRates")]
[ViewFormTitle("ViewSetExchangeRates")]
[ListFormTitle("SetExchangeRates")]
[NavigationName("Nav_SetExchangeRates")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(SetExchangeRatesPrintForm))]
[Description("Sets currency exchange rates to the base currency valid from a date.")]
public class SetExchangeRatesDto : DocumentDto
{
    [Caption("Description")]
    [ListFormColumn]
    [Description("Optional comment or description.")]
    public string? Description { get; set; }

    [Caption("Lines")]
    [Description("Exchange rate entries per currency.")]
    public ICollection<SetExchangeRatesLineDto> Lines { get; set; } = [];
}

[Description("A line specifying a currency and its exchange rate to the base currency.")]
public class SetExchangeRatesLineDto
{
    public Guid Id { get; set; }
    [Caption("SetExchangeRates_CurrencyId")]
    [Search(typeof(CurrencyDto))]
    [GridColumn(Order = 1, WidthPercent = 50)]
    [Description("The currency for which the rate is set.")]
    public Guid CurrencyId { get; set; }

    [Caption("SetExchangeRates_Rate")]
    [GridColumn(Order = 2, WidthPercent = 50)]
    [Description("Exchange rate of the currency to base currency.")]
    public decimal Rate { get; set; }
}

public class SetExchangeRatesQueryDto : DocumentQueryDto
{
}

public class SetExchangeRatesValidator : DocumentValidator<SetExchangeRatesDto>
{
    public SetExchangeRatesValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.Lines).NotEmpty().WithMessage(localizer["Validation_LinesRequired"]);
        RuleForEach(x => x.Lines).Must(l => l.CurrencyId != Guid.Empty).WithMessage(localizer["Validation_CurrencyRequired"]);
        RuleForEach(x => x.Lines).Must(l => l.Rate > 0).WithMessage(localizer["Validation_RateMustBePositive"]);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead, Drafts = DraftPrinting.Marked)]
[PrintTitle("SetExchangeRatesPrintTitle")]
public sealed class SetExchangeRatesPrintForm : DocumentPrintForm
{
    [Caption("Lines")]
    [PrintTable(LineNumbers = true)]
    public required ICollection<SetExchangeRatesPrintLine> Lines { get; set; }
}

public sealed class SetExchangeRatesPrintLine
{
    [PrintColumn]
    [Caption("SetExchangeRates_Currency")]
    public string CurrencyCode { get; set; } = string.Empty;

    [PrintColumn]
    [Caption("SetExchangeRates_Rate")]
    [Format("N4")]
    public decimal Rate { get; set; }
}
