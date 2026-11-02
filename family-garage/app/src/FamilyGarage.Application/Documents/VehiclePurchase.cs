using Kandra.Application.Abstractions.Behaviors;
using FamilyGarage.Domain.Commons;
using Kandra.Domain.Exceptions;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Domain.Numbering;
using Kandra.Domain.Registers;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Kandra.Printing;
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

[KandraDocumentBehavior(typeof(VehiclePurchaseDto))]
[RegisterTransactions(typeof(OdometerRecord))]
[RegisterTransactions(typeof(VehicleCostsMovement))]
[RegisterTransactions(typeof(FinanceDebtMovement))]
public class VehiclePurchaseBehavior(
    ISimpleNumberingSystem numbering,
    IConstantsService constants,
    IReportsRepository reportsRepo,
    IStringLocalizer localizer) : IDocumentBehavior<VehiclePurchase>
{
    public async ValueTask OnNewAsync(VehiclePurchase entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var yy = targetDate.ToString("yy");
        entity.Date = targetDate;
        entity.Code = await numbering.GetNextAsync(
            $"{NumberingBuckets.VehiclePurchases}:{yy}",
            raw => $"PUR-{yy}-{raw:D4}",
            requestorTag: "VehiclePurchase:OnNew",
            cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(VehiclePurchase entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await OdometerPostingGuard.EnsureOdometerValidAsync(
            registers,
            entity.VehicleId,
            entity.Date,
            entity.Id,
            entity.Odometer,
            localizer,
            cancellationToken);

        var odoWriter = registers.GetWriter<IDocumentInfoRegisterWriter<OdometerRecord>>();
        await odoWriter.PostAsync(
            entity.Id,
            entity.Date,
            [
                new OdometerRecord
                {
                    Dimensions = new OdometerDimensions { VehicleId = entity.VehicleId },
                    Details = new OdometerDetails { Kilometres = entity.Odometer }
                }
            ],
            cancellationToken);

        var costsWriter = registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>();
        await costsWriter.PostAsync(
            entity.Id,
            entity.Date,
            [
                new VehicleCostsMovement
                {
                    Dimensions = new VehicleCostsDimensions
                    {
                        VehicleId = entity.VehicleId,
                        Category = CostCategory.Purchase
                    },
                    Resources = new VehicleCostsResources { Amount = entity.Price },
                    IsExpense = false,
                    Details = new VehicleCostsDetails()
                }
            ],
            cancellationToken);

        var financed = entity.Payment != VehiclePurchasePaymentKind.Cash && entity.FinanceContractId.HasValue;
        if (financed)
        {
            await EnsureContractMatchesAsync(entity, cancellationToken);
        }

        // The contract must keep at least what has been paid on it: re-posting with another contract, a smaller financed
        // amount or as a cash purchase is refused while payments are posted (the debt would go below zero).
        await EnsurePaymentsStayCoveredAsync(
            entity,
            registers,
            financed ? entity.FinanceContractId : null,
            financed ? entity.Price - entity.DownPayment : 0m,
            cancellationToken);

        var debtWriter = registers.GetWriter<IBalanceRegisterWriter<FinanceDebtMovement, FinanceDebtBalance>>();
        if (!financed)
        {
            // A purchase changed to cash takes back the debt it posted before.
            await debtWriter.PostAsync(entity.Id, entity.Date, [], cancellationToken);
        }
        else
        {
            var financedAmount = entity.Price - entity.DownPayment;
            await debtWriter.PostAsync(
                entity.Id,
                entity.Date,
                [
                    new FinanceDebtMovement
                    {
                        Dimensions = new FinanceDebtDimensions
                        {
                            ContractId = entity.FinanceContractId!.Value
                        },
                        Resources = new FinanceDebtResources { Amount = financedAmount },
                        IsExpense = false,
                        Details = new NoDetails()
                    }
                ],
                cancellationToken);
        }
    }

    public async ValueTask OnUnsubmitAsync(VehiclePurchase entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await EnsurePaymentsStayCoveredAsync(entity, registers, null, 0m, cancellationToken);
        await registers.GetWriter<IBalanceRegisterWriter<FinanceDebtMovement, FinanceDebtBalance>>().UnpostAsync(entity.Id, cancellationToken);
        await registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>().UnpostAsync(entity.Id, cancellationToken);
        await registers.GetWriter<IDocumentInfoRegisterWriter<OdometerRecord>>().UnpostAsync(entity.Id, cancellationToken);
    }

    private async Task EnsureContractMatchesAsync(VehiclePurchase entity, CancellationToken cancellationToken)
    {
        var contract = await reportsRepo.GetFinanceContractAsync(entity.FinanceContractId!.Value, cancellationToken);
        var kind = entity.Payment == VehiclePurchasePaymentKind.Leasing ? FinanceContractKind.Leasing : FinanceContractKind.BankLoan;
        if (contract is null || contract.VehicleId != entity.VehicleId || contract.Kind != kind)
        {
            throw new InvalidStateException(localizer["VehiclePurchase_ContractMismatch"].Value);
        }
    }

    private async Task EnsurePaymentsStayCoveredAsync(
        VehiclePurchase entity,
        ISubmitScope registers,
        Guid? newContractId,
        decimal newFinancedAmount,
        CancellationToken cancellationToken)
    {
        var debtReader = registers.GetReader<IBalanceReader<FinanceDebtMovement, FinanceDebtBalance>>();
        var posted = (await debtReader.Movements(
                DateTime.MinValue,
                DateTime.MaxValue,
                q => q.Where(m => m.DocumentId == entity.Id && !m.IsExpense),
                cancellationToken))
            .FirstOrDefault();
        if (posted is null)
        {
            return;
        }

        var contractId = posted.Dimensions.ContractId;
        var paid = (await debtReader.Movements(
                DateTime.MinValue,
                DateTime.MaxValue,
                q => q.Where(m => m.Dimensions.ContractId == contractId && m.IsExpense),
                cancellationToken))
            .Sum(m => m.Resources.Amount);
        var stillFinanced = newContractId == contractId ? newFinancedAmount : 0m;
        if (paid > stillFinanced)
        {
            throw new InvalidStateException(localizer["VehiclePurchase_HasPayments", paid].Value);
        }
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<IBalanceRegisterWriter<FinanceDebtMovement, FinanceDebtBalance>>().DeleteAsync(id, cancellationToken);
        await registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>().DeleteAsync(id, cancellationToken);
        await registers.GetWriter<IDocumentInfoRegisterWriter<OdometerRecord>>().DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(
        VehiclePurchase entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var vehicles = await lookup.GetDictionaryAsync<Vehicle>([entity.VehicleId], cancellationToken);
        var sellers = await lookup.GetDictionaryAsync<Provider>([entity.SellerId], cancellationToken);
        string? contractName = null;
        if (entity.FinanceContractId.HasValue)
        {
            var contracts = await lookup.GetDictionaryAsync<FinanceContract>([entity.FinanceContractId.Value], cancellationToken);
            contractName = contracts.FirstOrDefault()?.Name;
        }

        var form = new VehiclePurchasePrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            Vehicle = vehicles.FirstOrDefault()?.Name ?? string.Empty,
            Seller = sellers.FirstOrDefault()?.Name ?? string.Empty,
            Odometer = entity.Odometer,
            Payment = entity.Payment.GetCaption(localizer),
            DownPayment = entity.Payment != VehiclePurchasePaymentKind.Cash ? entity.DownPayment : null,
            FinancedAmount = entity.Payment != VehiclePurchasePaymentKind.Cash ? (entity.Price - entity.DownPayment) : null,
            FinanceContract = contractName,
            Total = entity.Price,
            CurrencyCode = (await constants.GetAsync<CurrencyCodeConstant>(entity.Date, cancellationToken)).Value
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class VehiclePurchaseMapper : IEntityMapper<VehiclePurchase, VehiclePurchaseDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial VehiclePurchaseDto ToDto(VehiclePurchase entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial VehiclePurchase ToEntity(VehiclePurchaseDto dto);
}

public sealed class VehiclePurchaseQueryDtoValidator : PagedQueryDtoValidator<VehiclePurchaseQueryDto>
{
}
