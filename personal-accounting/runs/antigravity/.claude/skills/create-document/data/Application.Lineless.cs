// TEMPLATE — lineless (header-only) document, Application plane.
// Target: {{Config}}.Application/Documents/{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §5, §6, §8, §10 (AccountTransaction).

using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using {{Config}}.Domain.Entities.Documents;
using {{Config}}.Domain.Numbering;
using {{Config}}.Forms.Documents;
using Riok.Mapperly.Abstractions;

namespace {{Config}}.Application.Documents;

[KandraDocumentBehavior(typeof({{Name}}Dto))]
// OPTIONAL — only if OnSubmitAsync below posts to the chart-of-accounts ledger (post-to-accounting skill):
// [AccountTransactions]
public class {{Name}}Behavior(ISimpleNumberingSystem numbering) : IDocumentBehavior<{{Name}}>
{
    public async ValueTask OnNewAsync({{Name}} entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        var yy = entity.Date.ToString("yy");
        entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.{{Name}}}:{yy}", raw => $"{{NumberingPrefix}}.{yy}{raw:D{{NumberingDigits}}}",
            requestorTag: "{{Name}}:OnNew", cancellationToken: cancellationToken);
    }

    // A lineless document typically has nothing to recompute in OnSaveAsync — delete this override
    // entirely unless there's a genuinely computed header field.

    // REQUIRED if this document posts to accounting when submitted (the common reason to build a
    // lineless document at all — see AccountTransaction, reference.md §10, for the full real example
    // with user-picked account/subconto values via [AccountDialog]/[SubcontoEditor]).
    public async ValueTask OnSubmitAsync({{Name}} entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        // var postingService = registers.GetPostingService();
        // var transaction = new Kandra.Domain.Accounting.PostingTransaction(
        //     LineId: entity.Id,
        //     Source: postingService.AsOf(entity.SomeAccountCode).With(/* subconto values */).Build(),
        //     Destination: postingService.AsOf(/* ... */).With(/* ... */).Build(),
        //     Amount: /* ... */);
        // await postingService.PostAsync(entity.Id, [transaction], entity.Date, entity.Description, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync({{Name}} entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        // await registers.GetPostingService().UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        // await registers.GetPostingService().UnpostAsync(id, cancellationToken);
    }

    // OPTIONAL — the rest of IDocumentBehavior<{{Name}}> (reference.md §5), defaulted to a no-op.

    // public ValueTask OnOpenAsync({{Name}} entity, CancellationToken cancellationToken)
    // {
    // }
}

// Hand-written Mapperly stub: Mapperly generates the bodies at compile time, and Kandra.Generators.Di.Application
// finds this class by its IEntityMapper<,> interface and registers it (KANDRAGEN014 if it's missing). Target on
// ToDto: every Dto member must be filled; Source on ToEntity: every Dto member must land on the entity (audit
// fields/navigations the Dto doesn't carry stay default). An unmapped member is a build error (RMG012/RMG020).
[Mapper]
public partial class {{Name}}Mapper : IEntityMapper<{{Name}}, {{Name}}Dto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial {{Name}}Dto ToDto({{Name}} entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial {{Name}} ToEntity({{Name}}Dto dto);
}

public class {{Name}}QueryDtoValidator : PagedQueryDtoValidator<{{Name}}QueryDto>;
