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

[KandraDocumentBehavior(typeof(BudgetDto))]
[RegisterTransactions(typeof(BudgetPlanRecord))]
public class BudgetBehavior(ISimpleNumberingSystem numbering) : IDocumentBehavior<Budget>
{
    public async ValueTask OnNewAsync(Budget entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        var yy = entity.Date.ToString("yy");
        entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.Budget}:{yy}", raw => $"BDG.{yy}{raw:D4}",
            requestorTag: "Budget:OnNew", cancellationToken: cancellationToken);
    }

    public async ValueTask OnSubmitAsync(Budget entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(entity.Date.Year, entity.Date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var records = entity.Lines.Select(line => new BudgetPlanRecord
        {
            LineId = line.Id,
            Dimensions = new BudgetPlanDimensions { CategoryId = line.CategoryId },
            Details = new BudgetPlanDetails { PlannedAmount = line.PlannedAmount }
        }).ToList();

        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<BudgetPlanRecord>>();
        await writer.PostAsync(entity.Id, monthStart, records, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(Budget entity, ISubmitScope registers, CancellationToken cancellationToken) =>
        await registers.GetWriter<IDocumentInfoRegisterWriter<BudgetPlanRecord>>().UnpostAsync(entity.Id, cancellationToken);

    public ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken) =>
        new(registers.GetWriter<IDocumentInfoRegisterWriter<BudgetPlanRecord>>().DeleteAsync(id, cancellationToken));

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(Budget entity, ExportFormat format, string? layoutTag,
        ILookupRepository lookup, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        var categoryIds = entity.Lines.Select(l => l.CategoryId).Distinct().ToList();
        var categories = (await lookup.GetDictionaryAsync<CategoryNode>(categoryIds, cancellationToken)).ToDictionary(c => c.Id);

        var lines = entity.Lines.Select(l => new BudgetPrintLine
        {
            CategoryName = categories.TryGetValue(l.CategoryId, out var cat) ? cat.Name : l.CategoryId.ToString(),
            PlannedAmount = l.PlannedAmount
        }).ToList();

        var form = new BudgetPrintForm
        {
            Number = entity.Code,
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            GeneratedAt = DateTime.UtcNow,
            Lines = lines,
            Total = lines.Sum(l => l.PlannedAmount),
            CurrencyCode = ""
        };

        return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class BudgetMapper : IEntityMapper<Budget, BudgetDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial BudgetDto ToDto(Budget entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Budget ToEntity(BudgetDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial BudgetLineDto ToDto(BudgetLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapperIgnoreTarget(nameof(BudgetLine.Budget))]
    [MapperIgnoreTarget(nameof(BudgetLine.BudgetId))]
    [MapperIgnoreTarget(nameof(BudgetLine.Category))]
    private partial BudgetLine ToEntity(BudgetLineDto line);
}

public class BudgetQueryDtoValidator : PagedQueryDtoValidator<BudgetQueryDto>;
