using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Domain.Numbering;
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

[KandraDocumentBehavior(typeof(PolicyDto))]
[RegisterTransactions(typeof(VehicleCostsMovement))]
[RegisterTransactions(typeof(PolicyValidityRecord))]
public class PolicyBehavior(
    ISimpleNumberingSystem numbering,
    IConstantsService constants) : IDocumentBehavior<Policy>
{
    public async ValueTask OnNewAsync(Policy entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var yy = targetDate.ToString("yy");
        entity.Date = targetDate;
        entity.Code = await numbering.GetNextAsync(
            $"{NumberingBuckets.Policies}:{yy}",
            raw => $"POL-{yy}-{raw:D4}",
            requestorTag: "Policy:OnNew",
            cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(Policy entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var category = entity.Kind switch
        {
            PolicyKind.Tax => CostCategory.Tax,
            PolicyKind.Other => CostCategory.Other,
            _ => CostCategory.Insurance
        };

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
                        Category = category
                    },
                    Resources = new VehicleCostsResources { Amount = entity.Amount },
                    IsExpense = false,
                    Details = new VehicleCostsDetails()
                }
            ],
            cancellationToken);

        var validityWriter = registers.GetWriter<IDocumentInfoRegisterWriter<PolicyValidityRecord>>();
        await validityWriter.PostAsync(
            entity.Id,
            entity.Date,
            [
                new PolicyValidityRecord
                {
                    Dimensions = new PolicyValidityDimensions
                    {
                        VehicleId = entity.VehicleId,
                        PolicyKind = entity.Kind
                    },
                    Details = new PolicyValidityDetails
                    {
                        ValidTo = entity.ValidTo.ToDateTime(TimeOnly.MinValue)
                    }
                }
            ],
            cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(Policy entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>().UnpostAsync(entity.Id, cancellationToken);
        await registers.GetWriter<IDocumentInfoRegisterWriter<PolicyValidityRecord>>().UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        await registers.GetWriter<ITurnoverRegisterWriter<VehicleCostsMovement>>().DeleteAsync(id, cancellationToken);
        await registers.GetWriter<IDocumentInfoRegisterWriter<PolicyValidityRecord>>().DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(
        Policy entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var vehicles = await lookup.GetDictionaryAsync<Vehicle>([entity.VehicleId], cancellationToken);
        var providers = await lookup.GetDictionaryAsync<Provider>([entity.ProviderId], cancellationToken);

        var form = new PolicyPrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            Vehicle = vehicles.FirstOrDefault()?.Name ?? string.Empty,
            Kind = entity.Kind.GetCaption(localizer),
            Provider = providers.FirstOrDefault()?.Name ?? string.Empty,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Total = entity.Amount,
            CurrencyCode = (await constants.GetAsync<CurrencyCodeConstant>(entity.Date, cancellationToken)).Value
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class PolicyMapper : IEntityMapper<Policy, PolicyDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial PolicyDto ToDto(Policy entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Policy ToEntity(PolicyDto dto);
}

public sealed class PolicyQueryDtoValidator : PagedQueryDtoValidator<PolicyQueryDto>
{
}
