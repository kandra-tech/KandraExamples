// TEMPLATE — flat dictionary, Application plane.
// Target: {{Config}}.Application/Dictionaries/{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §5, §7.

using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using {{Config}}.Domain.Entities.Dictionaries;
using {{Config}}.Domain.Numbering;
using {{Config}}.Forms.Dictionaries;
using Riok.Mapperly.Abstractions;

namespace {{Config}}.Application.Dictionaries;

[KandraDictionaryBehavior(typeof({{Name}}Dto))]
public class {{Name}}Behavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<{{Name}}>
{
    public async ValueTask OnNewAsync({{Name}} entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync({{NumberingBucketsClass}}.{{Name}}, raw => $"{{NumberingPrefix}}-{raw:D{{NumberingDigits}}}",
            requestorTag: "{{Name}}:OnNew", cancellationToken: cancellationToken);

    // If Code is hand-typed instead of auto-numbered (see Currency in reference.md §9), replace the
    // body above with: => ValueTask.CompletedTask; and drop the `numbering` constructor parameter.

    // OPTIONAL — the rest of IDictionaryBehavior<{{Name}}> (reference.md §5), each defaulted to a no-op by
    // the interface itself. Uncomment and fill in only the ones this dictionary actually needs; delete the
    // rest rather than leaving an empty override.

    // public ValueTask OnOpenAsync({{Name}} entity, CancellationToken cancellationToken)
    // {
    // }

    // public ValueTask OnSaveAsync({{Name}} entity, bool isInsert, CancellationToken cancellationToken)
    // {
    // }

    // public ValueTask OnSubmitAsync({{Name}} entity, bool isInsert, CancellationToken cancellationToken)
    // {
    // }

    // public ValueTask OnDeleteAsync(Guid id, CancellationToken cancellationToken)
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
