using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Domain.Exceptions;
using Kandra.Forms.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Kandra.Printing;
using HomeUtilities.Application.Common;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Domain.Entities.Documents;
using HomeUtilities.Domain.Entities.Registers;
using HomeUtilities.Domain.Numbering;
using HomeUtilities.Forms.Documents;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomeUtilities.Application.Documents;

[KandraDocumentBehavior(typeof(MeterReadingDto))]
[RegisterTransactions(typeof(MeterReadingRecord))]
public class MeterReadingBehavior(
    ISimpleNumberingSystem numbering,
    IInfoRegisterReader<MeterReadingRecord> readingsReader,
    IStringLocalizer localizer,
    ICurrentTimeZone? currentTimeZone = null,
    IAuthorizationChecker? authorizationChecker = null) : IDocumentBehavior<MeterReading>
{
    public async ValueTask OnNewAsync(MeterReading entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        var localDate = TimeZoneHelper.ToLocalDate(entity.Date, currentTimeZone?.TimeZone);
        var yy = localDate.ToString("yy");
        entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.MeterReading}:{yy}",
            raw => $"MR-{yy}-{raw:D4}",
            requestorTag: "MeterReading:OnNew",
            cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(MeterReading entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        foreach (var line in entity.Lines)
        {
            var prevSlice = await readingsReader.SliceOfLast(entity.Date.AddTicks(-1),
                q => q.Where(r => r.DocumentId != entity.Id && r.Dimensions.MeterId == line.MeterId && r.Dimensions.Zone == line.Zone),
                cancellationToken);

            if (prevSlice.Count > 0 && line.Reading < prevSlice[0].Details.Reading)
            {
                throw new InvalidStateException(localizer["ReadingLowerThanPrevious", line.Reading, prevSlice[0].Details.Reading].Value);
            }
        }

        var records = entity.Lines.Select(line => new MeterReadingRecord
        {
            LineId = line.Id,
            Dimensions = new MeterReadingDimensions
            {
                MeterId = line.MeterId,
                Zone = line.Zone,
            },
            Details = new MeterReadingDetails
            {
                Reading = line.Reading,
            },
        }).ToList();

        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<MeterReadingRecord>>();
        await writer.PostAsync(entity.Id, entity.Date, records, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(MeterReading entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<MeterReadingRecord>>();
        await writer.UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<MeterReadingRecord>>();
        await writer.DeleteAsync(id, cancellationToken);
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(MeterReading entity, ExportFormat format, string? layoutTag,
        ILookupRepository lookup, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var property = await lookup.GetDictionaryAsync<Property>([entity.PropertyId], cancellationToken);
        var propertyName = property.Count > 0 ? property[0].Name : entity.PropertyId.ToString();

        var meterIds = entity.Lines.Select(l => l.MeterId).Distinct().ToList();
        var meters = (await lookup.GetDictionaryAsync<Meter>(meterIds, cancellationToken))
            .ToDictionary(m => m.Id, m => m.Name);

        var form = new MeterReadingPrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = authorizationChecker is not null
                ? await Reports.PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken)
                : null,
            PropertyName = propertyName,
            Lines = entity.Lines.Select(l => new MeterReadingPrintLine
            {
                MeterName = meters.TryGetValue(l.MeterId, out var mName) ? mName : l.MeterId.ToString(),
                Zone = l.Zone,
                PreviousReading = l.PreviousReading,
                Reading = l.Reading,
            }).ToList(),
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class MeterReadingMapper : IEntityMapper<MeterReading, MeterReadingDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    [MapperIgnoreTarget(nameof(MeterReadingDto.FillTrigger))]
    public partial MeterReadingDto ToDto(MeterReading entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreSource(nameof(MeterReadingDto.FillTrigger))]
    public partial MeterReading ToEntity(MeterReadingDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial MeterReadingLineDto ToLineDto(MeterReadingLine entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial MeterReadingLine ToLineEntity(MeterReadingLineDto dto);
}

public class MeterReadingQueryDtoValidator : PagedQueryDtoValidator<MeterReadingQueryDto>;
