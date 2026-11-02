using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using HomeUtilities.Enums;
using Microsoft.Extensions.Localization;

namespace HomeUtilities.Forms.Dictionaries;

[TypeId(HomeUtilitiesTypeIds.Provider)]
[KandraDictionaryForm(Name = "Providers", ValidatorType = typeof(ProviderValidator), QueryDtoType = typeof(ProviderQueryDto))]
[EditFormTitle("EditProvider")]
[AddFormTitle("CreateProvider")]
[ViewFormTitle("ViewProvider")]
[ListFormTitle("Providers")]
[NavigationName("Nav_Providers")]
[Description("A utility provider or servicing company with its service kind and account details.")]
public class ProviderDto : DictionaryDto
{
    [Caption("Provider_Kind")]
    [ListFormColumn]
    [Description("Kind of utility supplied by this provider.")]
    public ProviderKind Kind { get; set; }

    [Caption("Provider_PersonalAccountNumber")]
    [ListFormColumn]
    [Description("Personal customer account number at the provider.")]
    public string? PersonalAccountNumber { get; set; }

    [Caption("Provider_Phone")]
    [ListFormColumn]
    [Description("Support or contact phone number.")]
    public string? Phone { get; set; }

    [Caption("Provider_Iban")]
    [ListFormColumn]
    [Description("International Bank Account Number (IBAN).")]
    public string? Iban { get; set; }

    [Caption("Provider_Edrpou")]
    [ListFormColumn]
    [Description("National enterprise code (EDRPOU / Tax ID).")]
    public string? Edrpou { get; set; }

    [Caption("Provider_BankName")]
    [ListFormColumn]
    [Description("Name of the bank servicing this account.")]
    public string? BankName { get; set; }

    [Caption("Provider_Address")]
    [Multiline]
    [Description("Legal or postal address of the provider.")]
    public string? Address { get; set; }

    [Caption("Provider_PaymentDescriptionTemplate")]
    [Multiline]
    [Description("Template for payment purpose / description. Supported placeholders: {Month}, {Year}, {MonthName}, {Owner}, {PersonalAccountNumber}, {Account}, {PropertyAddress}, {PropertyName}, {ProviderName}, {MeterReadings}, {InvoiceNumber}")]
    public string? PaymentDescriptionTemplate { get; set; }
}

public class ProviderQueryDto : DictionaryQueryDto
{
}

public class ProviderValidator : DictionaryValidator<ProviderDto>
{
    public ProviderValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}
