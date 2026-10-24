using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Kandra.Printing;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Domain.Entities.Documents;
using PersonalAccounting.Domain.Entities.Registers;
using PersonalAccounting.Domain.Numbering;
using PersonalAccounting.Forms.Documents;
using Riok.Mapperly.Abstractions;

namespace PersonalAccounting.Application.Documents;

[KandraDocumentBehavior(typeof(SetExchangeRatesDto))]
[RegisterTransactions(typeof(ExchangeRatesRecord))]
public class SetExchangeRatesBehavior(ISimpleNumberingSystem numbering) : IDocumentBehavior<SetExchangeRates>
{
    public async ValueTask OnNewAsync(SetExchangeRates entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        var yy = entity.Date.ToString("yy");
        entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.SetExchangeRates}:{yy}", raw => $"RATES.{yy}{raw:D4}",
            requestorTag: "SetExchangeRates:OnNew", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(SetExchangeRates entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var records = entity.Lines.Select(line => new ExchangeRatesRecord
        {
            LineId = line.Id,
            Dimensions = new ExchangeRatesDimensions { CurrencyId = line.CurrencyId },
            Details = new ExchangeRatesDetails { Rate = line.Rate }
        }).ToList();

        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<ExchangeRatesRecord>>();
        await writer.PostAsync(entity.Id, entity.Date, records, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(SetExchangeRates entity, ISubmitScope registers, CancellationToken cancellationToken) =>
        await registers.GetWriter<IDocumentInfoRegisterWriter<ExchangeRatesRecord>>().UnpostAsync(entity.Id, cancellationToken);

    public ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken) =>
        new(registers.GetWriter<IDocumentInfoRegisterWriter<ExchangeRatesRecord>>().DeleteAsync(id, cancellationToken));

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(SetExchangeRates entity, ExportFormat format, string? layoutTag,
        ILookupRepository lookup, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var currencyIds = entity.Lines.Select(l => l.CurrencyId).Distinct().ToList();
        var currencies = (await lookup.GetDictionaryAsync<Currency>(currencyIds, cancellationToken)).ToDictionary(c => c.Id);

        var lines = entity.Lines.Select(l => new SetExchangeRatesPrintLine
        {
            CurrencyCode = currencies.TryGetValue(l.CurrencyId, out var cur) ? cur.Code : l.CurrencyId.ToString(),
            Rate = l.Rate
        }).ToList();

        var form = new SetExchangeRatesPrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            GeneratedAt = DateTime.UtcNow,
            Lines = lines
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class SetExchangeRatesMapper : IEntityMapper<SetExchangeRates, SetExchangeRatesDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial SetExchangeRatesDto ToDto(SetExchangeRates entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial SetExchangeRates ToEntity(SetExchangeRatesDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial SetExchangeRatesLineDto ToDto(SetExchangeRatesLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreTarget(nameof(SetExchangeRatesLine.SetExchangeRates))]
    [MapperIgnoreTarget(nameof(SetExchangeRatesLine.SetExchangeRatesId))]
    [MapperIgnoreTarget(nameof(SetExchangeRatesLine.Currency))]
    private partial SetExchangeRatesLine ToEntity(SetExchangeRatesLineDto line);
}

public class SetExchangeRatesQueryDtoValidator : PagedQueryDtoValidator<SetExchangeRatesQueryDto>;
