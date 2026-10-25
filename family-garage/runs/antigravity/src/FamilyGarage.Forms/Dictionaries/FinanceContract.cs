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
using FamilyGarage.Enums;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Forms.Dictionaries;

[TypeId(FamilyGarageTypeIds.FinanceContract)]
[KandraDictionaryForm(Name = "FinanceContracts", ValidatorType = typeof(FinanceContractValidator), QueryDtoType = typeof(FinanceContractQueryDto))]
[EditFormTitle("EditFinanceContract")]
[AddFormTitle("CreateFinanceContract")]
[ViewFormTitle("ViewFinanceContract")]
[ListFormTitle("FinanceContracts")]
[NavigationName("Nav_FinanceContracts")]
[Description("A bank loan or leasing finance contract for a vehicle.")]
public class FinanceContractDto : DictionaryDto
{
    [Caption("ContractNumber")]
    [ListFormColumn]
    [Description("Contract reference number.")]
    public string Number { get; set; } = string.Empty;

    [Caption("Vehicle")]
    [Search(typeof(VehicleDto))]
    [ListFormColumn(Filterable = false)]
    [Description("Vehicle financed under this contract.")]
    public Guid VehicleId { get; set; }

    [Caption("Kind")]
    [ListFormColumn(Filterable = false)]
    [Description("Kind of financing: bank loan or leasing with buyout.")]
    public FinanceContractKind Kind { get; set; }

    [Caption("Lender")]
    [Search(typeof(ProviderDto))]
    [ListFormColumn(Filterable = false)]
    [Description("Provider acting as lender or leasing company.")]
    public Guid LenderId { get; set; }

    [Caption("SignedOn")]
    [ListFormColumn(Filterable = false)]
    [Description("Date the contract was signed.")]
    public DateTime SignedOn { get; set; }

    [Caption("AnnualInterestRatePercent")]
    [ListFormColumn(Filterable = false)]
    [Description("Annual interest rate in percent.")]
    public decimal AnnualInterestRatePercent { get; set; }

    [Caption("TermMonths")]
    [ListFormColumn(Filterable = false)]
    [Description("Term of the contract in months.")]
    public int TermMonths { get; set; }

    [Caption("MonthlyPayment")]
    [ListFormColumn(Filterable = false)]
    [Description("Contractual monthly payment amount.")]
    public decimal MonthlyPayment { get; set; }

    [Caption("PaymentDayOfMonth")]
    [ListFormColumn(Filterable = false)]
    [Description("Day of the month on which payment is due.")]
    public int PaymentDayOfMonth { get; set; }

    [Caption("Comment")]
    [Description("Optional notes or terms description.")]
    public string? Comment { get; set; }
}

public class FinanceContractQueryDto : DictionaryQueryDto
{
}

public class FinanceContractValidator : DictionaryValidator<FinanceContractDto>
{
    public FinanceContractValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.Number).NotEmpty().WithMessage(localizer["FieldIsRequired", localizer["ContractNumber"]]);
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage(localizer["FieldIsRequired", localizer["Vehicle"]]);
        RuleFor(x => x.LenderId).NotEmpty().WithMessage(localizer["FieldIsRequired", localizer["Lender"]]);
        RuleFor(x => x.TermMonths).GreaterThan(0).WithMessage(localizer["MustBePositive"]);
        RuleFor(x => x.MonthlyPayment).GreaterThanOrEqualTo(0).WithMessage(localizer["MustBeNonNegative"]);
        RuleFor(x => x.PaymentDayOfMonth).InclusiveBetween(1, 31).WithMessage(localizer["InvalidDayOfMonth"]);
    }
}
