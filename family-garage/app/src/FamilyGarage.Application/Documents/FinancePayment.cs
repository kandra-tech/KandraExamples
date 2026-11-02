using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Domain.Exceptions;
using Kandra.Domain.Numbering;
using Kandra.Domain.Registers;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Kandra.Printing;
using FamilyGarage.Domain.Commons;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Domain.Entities.Documents;
using FamilyGarage.Domain.Entities.Registers;
using FamilyGarage.Domain.Numbering;
using FamilyGarage.Enums;
using FamilyGarage.Forms.Documents;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;
using Kandra.Application.Abstractions.Constants;
using Kandra.Localization;
using FamilyGarage.Application.Constants;

namespace FamilyGarage.Application.Documents;

[KandraDocumentBehavior(typeof(FinancePaymentDto))]
[RegisterTransactions(typeof(FinanceDebtMovement))]
[RegisterTransactions(typeof(VehicleCostsMovement))]
public class FinancePaymentBehavior(
    ISimpleNumberingSystem numbering,
    IConstantsService constants,
    IReportsRepository reportsRepo,
    IStringLocalizer localizer) : IDocumentBehavior<FinancePayment>
{
    public async ValueTask OnNewAsync(FinancePayment entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var yy = targetDate.ToString("yy");
        entity.Date = targetDate;
        entity.Code = await numbering.GetNextAsync(
            $"{NumberingBuckets.FinancePayments}:{yy}",
            raw => $"FP-{yy}-{raw:D4}",
            requestorTag: "FinancePayment:OnNew",
            cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(FinancePayment entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var debtReader = registers.GetReader<IBalanceReader<FinanceDebtMovement, FinanceDebtBalance>>();
        var balances = await debtReader.Balances(
            q => q.Where(b => b.Dimensions.ContractId == entity.FinanceContractId),
            cancellationToken);

        // On a re-post the balance already has this payment's own principal taken off: add it back, or saving a posted
        // payment that pays off the rest of the debt (the leasing buyout) would refuse.
        var ownPrincipal = (await debtReader.Movements(
                DateTime.MinValue,
                DateTime.MaxValue,
                q => q.Where(m => m.DocumentId == entity.Id && m.IsExpense && m.Dimensions.ContractId == entity.FinanceContractId),
                cancellationToken))
            .Sum(m => m.Resources.Amount);
        var currentDebt = (balances.FirstOrDefault()?.Resources.Amount ?? 0m) + ownPrincipal;
        if (entity.Principal > currentDebt)
        {
            throw new InvalidStateException(localizer["FinancePayment_PrincipalExceedsDebt", entity.Principal, currentDebt].Value);
        }

        var debtWriter = registers.GetWriter<IBalanceRegisterWriter<FinanceDebtMovement, FinanceDebtBalance>>();
        var debtMovements = new List<FinanceDebtMovement>();
        if (entity.Principal > 0)
        {
            debtMovements.Add(new FinanceDebtMovement
            {
                Dimensions = new FinanceDebtDimensions
                {
                    ContractId = entity.FinanceContractId
                },
                Resources = new FinanceDebtResources { Amount = entity.Principal },
                IsExpense = true, // Outflow: principal paid reduces remaining debt
                Details = new NoDetails()
            });
        }
        await debtWriter.PostAsync(entity.Id, entity.Date, debtMovements, cancellationToken);

        var contract = await reportsRepo.GetFinanceContractAsync(entity.FinanceContractId, cancellationToken);
        var vehicleId = contract?.VehicleId ?? Guid.Empty;

        var interestAndFees = entity.Interest + entity.Fees;
        var costsWriter = registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>();
        var costMovements = new List<VehicleCostsMovement>();
        if (interestAndFees > 0 && vehicleId != Guid.Empty)
        {
            costMovements.Add(new VehicleCostsMovement
            {
                Dimensions = new VehicleCostsDimensions
                {
                    VehicleId = vehicleId,
                    Category = CostCategory.InterestAndFees
                },
                Resources = new VehicleCostsResources { Amount = interestAndFees },
                IsExpense = false,
                Details = new VehicleCostsDetails()
            });
        }
        await costsWriter.PostAsync(entity.Id, entity.Date, costMovements, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(FinancePayment entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<IBalanceRegisterWriter<FinanceDebtMovement, FinanceDebtBalance>>().UnpostAsync(entity.Id, cancellationToken);
        await registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>().UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<IBalanceRegisterWriter<FinanceDebtMovement, FinanceDebtBalance>>().DeleteAsync(id, cancellationToken);
        await registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>().DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(
        FinancePayment entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var contracts = await lookup.GetDictionaryAsync<FinanceContract>([entity.FinanceContractId], cancellationToken);
        var total = entity.Principal + entity.Interest + entity.Fees;

        var form = new FinancePaymentPrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            FinanceContract = contracts.FirstOrDefault()?.Name ?? string.Empty,
            Principal = entity.Principal,
            Interest = entity.Interest,
            Fees = entity.Fees,
            Comment = entity.Comment,
            Total = total,
            CurrencyCode = (await constants.GetAsync<CurrencyCodeConstant>(entity.Date, cancellationToken)).Value
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class FinancePaymentMapper : IEntityMapper<FinancePayment, FinancePaymentDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial FinancePaymentDto ToDto(FinancePayment entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial FinancePayment ToEntity(FinancePaymentDto dto);
}

public sealed class FinancePaymentQueryDtoValidator : PagedQueryDtoValidator<FinancePaymentQueryDto>
{
}
