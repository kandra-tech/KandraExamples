using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Attributes.Application;
using {{Config}}.Forms.DataProcessors;
using {{Config}}.Forms.Documents;

namespace {{Config}}.Application.DataProcessors;

// The common shape: create/submit an ordinary document through IDocumentCrudService<,> (or a dictionary/other
// entity through the matching CRUD service), exactly like a human user would through the UI. This is the
// recommended, indirect way to affect registers — the normal posting pipeline (audit trail, reversal-on-
// unsubmit) stays intact. See ../reference.md §6 for the direct-register-write alternative (rare).
[KandraDataProcessorBehavior(typeof({{Name}}Dto))]
public class {{Name}}Behavior(
    IDocumentCrudService<{{TargetDocumentDto}}, {{TargetDocumentQueryDto}}> targetDocuments
    /* + any plugin client interfaces / IEntityRepository<T> / other ordinary services you need */)
    : IDataProcessorBehavior<{{Name}}Dto, {{Name}}Result>
{
    // MUST equal {{Name}}Dto's own simple name minus "Dto" — i.e. literally "{{Name}}" — nothing else.
    // This is read at runtime by DataProcessorService<,>.Authorize(); a mismatch here silently makes the
    // processor permanently unauthorized for everyone, since the auto-generated claim/policy are seeded from
    // the Dto name, not from this string. See ../reference.md §8.
    public string ObjectName => "{{Name}}";

    // OPTIONAL — delete if the input Dto has no field that needs a server-side default.
    public ValueTask OnNewAsync({{Name}}Dto input, CancellationToken cancellationToken)
    {
        input.{{InputField}} = DateTime.UtcNow.Date;
        return ValueTask.CompletedTask;
    }

    public async Task<{{Name}}Result> OnProcessAsync({{Name}}Dto input, CancellationToken cancellationToken)
    {
        // do the work — call a plugin/external service for data if needed, then use the ordinary
        // I{Kind}CrudService<TDto,TQueryDto> to create/insert/submit a real document (or dictionary entry).
        var dto = await targetDocuments.NewAsync(/* seed fields from input */ cancellationToken);
        var created = await targetDocuments.InsertAsync(dto, cancellationToken);

        return new {{Name}}Result
        {
            {{CreatedEntityIdField}} = created.Id,
            {{ResultField}} = dto.Code,
        };
    }
}

// ── OPTIONAL — the direct-register-write alternative (rare; most processors should use the shape above
// instead). Implement IRegisterWritingDataProcessorBehavior<,> INSTEAD OF IDataProcessorBehavior<,> above —
// it's an interface to implement, not a base class to derive from. Delete this whole block if unused; never
// declare both on the same class. See ../reference.md §6. ──────────────────────────────────────────────────
// [KandraDataProcessorBehavior(typeof({{Name}}Dto))]
// public class {{Name}}Behavior(/* ordinary services */)
//     : IRegisterWritingDataProcessorBehavior<{{Name}}Dto, {{Name}}Result>
// {
//     public string ObjectName => "{{Name}}";
//
//     public async Task<{{Name}}Result> OnProcessAsync({{Name}}Dto input, ISubmitScope registers, CancellationToken cancellationToken)
//     {
//         var writer = registers.GetWriter<IBalanceRegisterWriter<TMovement, TBalance>>(); // or whichever writer
//         // ... build movements/records, await writer.PostAsync(...) ...
//         return new {{Name}}Result { /* ... */ };
//     }
// }
