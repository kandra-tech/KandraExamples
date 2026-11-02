using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Registers;
using HomePantry.Domain.Numbering;
using HomePantry.Enums;
using HomePantry.Forms.Documents;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Documents;

[KandraDocumentBehavior(typeof(BatteryChargedDto))]
[RegisterTransactions(typeof(BatteryLogRecord))]
[RegisterTransactions(typeof(BatteryCyclesMovement))]
public sealed class BatteryChargedBehavior(
    ISimpleNumberingSystem numbering,
    IEntityRepository<Battery> batteryRepository) : IDocumentBehavior<BatteryCharged>
{
    public async ValueTask OnNewAsync(BatteryCharged entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.BatteryCharged, raw => $"BAT-{raw:D4}", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSaveAsync(BatteryCharged entity, bool isInsert, CancellationToken cancellationToken)
    {
        if (entity.BatteryId != Guid.Empty)
        {
            var battery = await batteryRepository.GetAsync(entity.BatteryId, true, cancellationToken);
            if (battery?.BatteryKind == BatteryKind.Disposable)
            {
                entity.Replaced = true;
            }
        }
    }

    public async ValueTask OnSubmitAsync(BatteryCharged entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        if (entity.BatteryId != Guid.Empty)
        {
            var battery = await batteryRepository.GetAsync(entity.BatteryId, true, cancellationToken);
            if (battery?.BatteryKind == BatteryKind.Disposable)
            {
                entity.Replaced = true;
            }
        }

        var logWriter = registers.GetWriter<IDocumentInfoRegisterWriter<BatteryLogRecord>>();
        var cyclesWriter = registers.GetWriter<ITurnoverRegisterWriter<BatteryCyclesMovement>>();

        var logRecord = new BatteryLogRecord
        {
            LineId = entity.Id,
            Dimensions = new BatteryLogDimensions { BatteryId = entity.BatteryId },
            Details = new BatteryLogDetails { Replaced = entity.Replaced }
        };

        var cyclesMovement = new BatteryCyclesMovement
        {
            LineId = entity.Id,
            IsExpense = false,
            Dimensions = new BatteryCyclesDimensions { BatteryId = entity.BatteryId },
            Resources = new BatteryCyclesResources
            {
                Cycles = 1m,
                Replacements = entity.Replaced ? 1m : 0m
            }
        };

        await logWriter.PostAsync(entity.Id, entity.Date, [logRecord], cancellationToken);
        await cyclesWriter.PostAsync(entity.Id, entity.Date, [cyclesMovement], cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(BatteryCharged entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var logWriter = registers.GetWriter<IDocumentInfoRegisterWriter<BatteryLogRecord>>();
        var cyclesWriter = registers.GetWriter<ITurnoverRegisterWriter<BatteryCyclesMovement>>();

        await logWriter.UnpostAsync(entity.Id, cancellationToken);
        await cyclesWriter.UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var logWriter = registers.GetWriter<IDocumentInfoRegisterWriter<BatteryLogRecord>>();
        var cyclesWriter = registers.GetWriter<ITurnoverRegisterWriter<BatteryCyclesMovement>>();

        await logWriter.DeleteAsync(id, cancellationToken);
        await cyclesWriter.DeleteAsync(id, cancellationToken);
    }
}

[Mapper]
public partial class BatteryChargedMapper : IEntityMapper<BatteryCharged, BatteryChargedDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial BatteryChargedDto ToDto(BatteryCharged entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial BatteryCharged ToEntity(BatteryChargedDto dto);
}

public class BatteryChargedQueryDtoValidator : PagedQueryDtoValidator<BatteryChargedQueryDto>;
