// TEMPLATE — line-bearing document, Application plane.
// Target: {{Config}}.Application/Documents/{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §5, §6, §8.

using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Persistence.Abstractions.Registers;
using {{Config}}.Domain.Entities.Documents;
using {{Config}}.Domain.Numbering;
using {{Config}}.Forms.Documents;
using Riok.Mapperly.Abstractions;

namespace {{Config}}.Application.Documents;

[KandraDocumentBehavior(typeof({{Name}}Dto))]
// OPTIONAL — declarative-only, no behavioral effect on their own (reference.md §2). Add one
// [RegisterTransactions(typeof(...))] per register row type this document's OnSubmitAsync actually
// writes to, and [AccountTransactions] only if OnSubmitAsync also posts to the chart-of-accounts ledger.
// [RegisterTransactions(typeof(SomeMovementType))]
// [AccountTransactions]
public class {{Name}}Behavior(ISimpleNumberingSystem numbering) : IDocumentBehavior<{{Name}}>
{
    public async ValueTask OnNewAsync({{Name}} entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        var yy = entity.Date.ToString("yy");
        entity.Code = await numbering.GetNextAsync($"{NumberingBuckets.{{Name}}}:{yy}", raw => $"{{NumberingPrefix}}.{yy}{raw:D{{NumberingDigits}}}",
            requestorTag: "{{Name}}:OnNew", cancellationToken: cancellationToken);

        // OPTIONAL — only if this Dto is a [SourceFor] target on some other document's Dto: resolve
        // sourceDocumentId via IDocumentReaderService (constructor-inject it) and pattern-match to the
        // concrete source type you expect, no-op gracefully if it doesn't match. sourceDocumentId is
        // null for an ordinary new document — always handle that case first.
    }

    // OPTIONAL — server-authoritative recalculation, not trust-the-client - runs on both insert and
    // update. Only needed if this document has computed line/header fields (e.g. a line Sum, a header
    // Total) — delete entirely if it doesn't. The Quantity/Price/Sum shape below is the "order line"
    // example from the Domain/Forms templates; adjust it to whatever fields this line actually has.
    // public ValueTask OnSaveAsync({{Name}} entity, bool isInsert, CancellationToken cancellationToken)
    // {
    //     foreach (var line in entity.Lines)
    //     {
    //         line.Sum = line.Quantity * line.Price;
    //     }
    //     entity.Total = entity.Lines.Sum(line => line.Sum);
    //     return ValueTask.CompletedTask;
    // }

    // REQUIRED if this document writes any register or posts to accounting when submitted — the only
    // three hooks allowed to (reference.md §6). Delete this override (leave the interface's no-op
    // default) only if this document genuinely never moves a register or posts a ledger entry.
    public async ValueTask OnSubmitAsync({{Name}} entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        // var writer = registers.GetWriter<IBalanceRegisterWriter<SomeMovementType, SomeBalanceType>>();
        // var movements = entity.Lines.Select(line => new SomeMovementType { /* ... */ }).ToList();
        // await writer.PostAsync(entity.Id, entity.Date, movements, cancellationToken);

        // If this document also posts to the chart-of-accounts ledger (post-to-accounting skill):
        // var postingService = registers.GetPostingService();
        // var transactions = entity.Lines.Select(line => new PostingTransaction(
        //     LineId: line.Id,
        //     Source: postingService.AsOf(63).With<SomeSubconto1, SomeSubconto2>(id1, id2).Build(),
        //     Destination: postingService.AsOf(28).With<SomeSubconto1, SomeSubconto2>(id1, id2).Build(),
        //     Amount: line.Quantity * line.Price)).ToList();
        // await postingService.PostAsync(entity.Id, transactions, entity.Date, ct: cancellationToken);
    }

    // OPTIONAL — the rest of IDocumentBehavior<{{Name}}> (reference.md §5), each defaulted to a no-op by
    // the interface itself. Uncomment and fill in only the ones this document actually needs.

    // public ValueTask OnOpenAsync({{Name}} entity, CancellationToken cancellationToken)
    // {
    // }

    // Should exactly reverse OnSubmitAsync above — mandatory (not optional) if OnSubmitAsync writes
    // anything, since the engine calls this on every Unsubmit action.
    // public async ValueTask OnUnsubmitAsync({{Name}} entity, ISubmitScope registers, CancellationToken cancellationToken)
    // {
    //     await registers.GetWriter<IBalanceRegisterWriter<SomeMovementType, SomeBalanceType>>().UnpostAsync(entity.Id, cancellationToken);
    // }

    // Handles "delete an already-unposted document" — the engine blocks deleting a still-posted
    // (IsActive) document until it's unsubmitted first, so this only ever runs post-unsubmit.
    // public ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken) =>
    //     new(registers.GetWriter<IBalanceRegisterWriter<SomeMovementType, SomeBalanceType>>().DeleteAsync(id, cancellationToken));

    // Only meaningful if the Dto carries [SupportedPrintForm] (create-print-form skill) — leave
    // undeclared/default otherwise, don't stub this out speculatively. Build the print form from the saved entity
    // (display names through `lookup`, never nav properties) and render with RenderAsync: it reads the company
    // letterhead as of the form's Date, which the synchronous Render does not.
    // public async ValueTask<Kandra.Printing.PrintRenderOutput?> OnPrintAsync({{Name}} entity, Kandra.Attributes.Printable.ExportFormat format, string? layoutTag, Kandra.Persistence.Abstractions.ILookupRepository lookup, Kandra.Printing.IPrintRendererResolver renderer, Microsoft.Extensions.Localization.IStringLocalizer localizer, TimeZoneInfo? timeZone, CancellationToken cancellationToken)
    // {
    //     var form = new {{Name}}PrintForm
    //     {
    //         Number = entity.Code,
    //         Date = entity.Date,
    //         IsSubmitted = entity.IsActive,
    //         GeneratedAt = DateTime.UtcNow,   // UTC, converted to the client's time zone on render
    //         GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken),
    //         Counterparty = await printParties.GetCounterpartyAsync(entity.CounterpartyId, cancellationToken),   // the configuration's IPrintPartyService
    //         Lines = entity.Lines.Select(line => new {{Name}}PrintLine { /* names resolved through lookup */ Quantity = line.Quantity, Price = line.Price, Sum = line.Sum }).ToList(),
    //         Total = entity.Lines.Sum(line => line.Sum),
    //         Signatures = [new PrintSignature("ShippedBy", Stamp: true), new PrintSignature("ReceivedBy", WithProxy: true)],
    //     };
    //
    //     return await renderer.RenderAsync(form, localizer, timeZone, format, layoutTag ?? "default", cancellationToken);
    // }
}

// Hand-written Mapperly stub (see create-dictionary's Application.Flat.cs for the strategy rules). The line
// methods are declared explicitly so they carry the same Target/Source strategies - Mapperly reuses them for
// the Lines collection in both directions.
[Mapper]
public partial class {{Name}}Mapper : IEntityMapper<{{Name}}, {{Name}}Dto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial {{Name}}Dto ToDto({{Name}} entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial {{Name}} ToEntity({{Name}}Dto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial {{Name}}LineDto ToDto({{Name}}Line line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private partial {{Name}}Line ToEntity({{Name}}LineDto line);
}

public class {{Name}}QueryDtoValidator : PagedQueryDtoValidator<{{Name}}QueryDto>;
