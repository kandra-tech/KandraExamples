using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Persistence.Abstractions.Registers;
using HomePantry.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Registers;
using HomePantry.Domain.Numbering;
using HomePantry.Forms.Documents;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Documents;

[KandraDocumentBehavior(typeof(ChoreDoneDto))]
[RegisterTransactions(typeof(ChoreLogRecord))]
public sealed class ChoreDoneBehavior(ISimpleNumberingSystem numbering) : IDocumentBehavior<ChoreDone>
{
    public async ValueTask OnNewAsync(ChoreDone entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.ChoreDone, raw => $"CHD-{raw:D4}", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(ChoreDone entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var record = new ChoreLogRecord
        {
            LineId = entity.Id,
            Dimensions = new ChoreLogDimensions { ChoreId = entity.ChoreId },
            Details = new ChoreLogDetails { DoneById = entity.DoneById }
        };

        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<ChoreLogRecord>>();
        await writer.PostAsync(entity.Id, entity.Date, [record], cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(ChoreDone entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<ChoreLogRecord>>();
        await writer.UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<ChoreLogRecord>>();
        await writer.DeleteAsync(id, cancellationToken);
    }
}

[Mapper]
public partial class ChoreDoneMapper : IEntityMapper<ChoreDone, ChoreDoneDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial ChoreDoneDto ToDto(ChoreDone entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial ChoreDone ToEntity(ChoreDoneDto dto);
}

public class ChoreDoneQueryDtoValidator : PagedQueryDtoValidator<ChoreDoneQueryDto>;
