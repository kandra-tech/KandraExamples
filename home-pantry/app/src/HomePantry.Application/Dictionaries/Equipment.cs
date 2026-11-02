using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Abstractions.Services.Blobs;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Numbering;
using HomePantry.Forms.Dictionaries;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Dictionaries;

[KandraDictionaryBehavior(typeof(EquipmentDto))]
public sealed class EquipmentBehavior(ISimpleNumberingSystem numbering, IBlobService blobService) : IFlatDictionaryBehavior<Equipment>
{
    public async ValueTask OnNewAsync(Equipment entity, CancellationToken cancellationToken)
    {
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Equipment, raw => $"{raw:D4}", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSaveAsync(Equipment entity, bool isInsert, CancellationToken cancellationToken)
    {
        var blobIds = new List<Guid>();
        if (entity.ManualFileId.HasValue && entity.ManualFileId.Value != Guid.Empty)
            blobIds.Add(entity.ManualFileId.Value);
        if (entity.PhotoFileId.HasValue && entity.PhotoFileId.Value != Guid.Empty)
            blobIds.Add(entity.PhotoFileId.Value);

        await blobService.SyncEntityReferencesAsync(entity.Id, Equipment.TypeId, blobIds, cancellationToken, entity.Code);
    }

    public async ValueTask OnDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await blobService.DetachAllForEntityAsync(id, Equipment.TypeId, cancellationToken);
    }
}

[Mapper]
public sealed partial class EquipmentMapper : IEntityMapper<Equipment, EquipmentDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial EquipmentDto ToDto(Equipment entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Equipment ToEntity(EquipmentDto dto);
}

public class EquipmentQueryDtoValidator : PagedQueryDtoValidator<EquipmentQueryDto>;
