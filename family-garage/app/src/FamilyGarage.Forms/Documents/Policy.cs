using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.DtoCommon.Printing;
using Kandra.Forms.Documents;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Validators.Documents;
using FamilyGarage.Enums;
using FamilyGarage.Forms.Dictionaries;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Forms.Documents;

[TypeId(FamilyGarageTypeIds.Policy)]
[KandraDocumentForm(Name = "Policies", ValidatorType = typeof(PolicyValidator), QueryDtoType = typeof(PolicyQueryDto))]
[EditFormTitle("EditPolicy")]
[AddFormTitle("CreatePolicy")]
[ViewFormTitle("ViewPolicy")]
[ListFormTitle("Policies")]
[NavigationName("Nav_Policies")]
[SupportedExportFormats(ExportFormat.Pdf)]
[SupportedPrintForm(typeof(PolicyPrintForm))]
[Description("Records insurance policies and vehicle taxes, tracking validity dates and costs.")]
public class PolicyDto : DocumentDto
{
    [Caption("Vehicle")]
    [Dropdown(typeof(VehicleDto))]
    [ListFormColumn(Filterable = false)]
    [Description("The insured vehicle.")]
    public Guid VehicleId { get; set; }

    [Caption("Kind")]
    [ListFormColumn(Filterable = false)]
    [Description("Policy kind: compulsory insurance, comprehensive insurance, tax, or other.")]
    public PolicyKind Kind { get; set; } = PolicyKind.CompulsoryInsurance;

    [Caption("Provider")]
    [Dropdown(typeof(ProviderDto))]
    [Description("Insurance or tax provider.")]
    public Guid ProviderId { get; set; }

    [Caption("ValidFrom")]
    [ListFormColumn(Filterable = false)]
    [Description("Start date of policy validity.")]
    public DateOnly ValidFrom { get; set; }

    [Caption("ValidTo")]
    [ListFormColumn(Filterable = false)]
    [Description("End date of policy validity.")]
    public DateOnly ValidTo { get; set; }

    [Caption("Amount")]
    [ListFormColumn(Filterable = false)]
    [Description("Cost of the policy.")]
    public decimal Amount { get; set; }

    [Caption("ScannedDocument")]
    [Description("Scanned copy or file attachment.")]
    public string? ScannedDocument { get; set; }
}

public class PolicyQueryDto : DocumentQueryDto
{
}

public class PolicyValidator : DocumentValidator<PolicyDto>
{
    public PolicyValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage(localizer["VehicleRequired"]);
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage(localizer["ProviderRequired"]);
        RuleFor(x => x.ValidFrom).NotEmpty().WithMessage(localizer["ValidFromRequired"]);
        RuleFor(x => x.ValidTo).NotEmpty().WithMessage(localizer["ValidToRequired"]);
        RuleFor(x => x.ValidTo).GreaterThanOrEqualTo(x => x.ValidFrom)
            .WithMessage(localizer["ValidToMustBeAfterValidFrom"]);
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage(localizer["AmountPositive"]);
    }
}

[PrintForm(Frame = PrintFrame.Letterhead)]
[PrintTitle("PolicyPrintTitle")]
public sealed class PolicyPrintForm : DocumentPrintForm
{
    [Caption("Vehicle")] public string Vehicle { get; set; } = string.Empty;
    [Caption("Kind")] public string Kind { get; set; } = string.Empty;
    [Caption("Provider")] public string Provider { get; set; } = string.Empty;
    [Caption("ValidFrom")] public DateOnly ValidFrom { get; set; }
    [Caption("ValidTo")] public DateOnly ValidTo { get; set; }

    public PrintVatMode VatMode { get; set; } = PrintVatMode.NotPayer;
    public IReadOnlyList<PrintTax> Taxes { get; set; } = [];

    [PrintRole(PrintRole.GrandTotal)]
    public decimal Total { get; set; }

    [PrintRole(PrintRole.Currency)]
    public string CurrencyCode { get; set; } = string.Empty;
}
