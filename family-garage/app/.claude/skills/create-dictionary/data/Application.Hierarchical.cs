// TEMPLATE — hierarchical dictionary (folder/leaf TPH tree), Application plane.
// Target: {{Config}}.Application/Dictionaries/{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §5, §7, §9.

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

[KandraDictionaryBehavior(typeof({{Name}}NodeDto))]
public class {{Name}}Behavior(ISimpleNumberingSystem numbering) : IHierarchicalDictionaryBehavior<{{Name}}Node>
{
    public async ValueTask OnFolderNewAsync({{Name}}Node entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync({{NumberingBucketsClass}}.{{Name}}Folder, raw => $"{{FolderNumberingPrefix}}-{raw:D{{FolderNumberingDigits}}}",
            requestorTag: "{{Name}}Folder:OnNew", cancellationToken: cancellationToken);

    public async ValueTask OnLeafNewAsync({{Name}}Node entity, CancellationToken cancellationToken) =>
        entity.Code = await numbering.GetNextAsync({{NumberingBucketsClass}}.{{Name}}, raw => $"{{NumberingPrefix}}-{raw:D{{NumberingDigits}}}",
            requestorTag: "{{Name}}:OnNew", cancellationToken: cancellationToken);

    // OPTIONAL — the rest of IDictionaryBehavior<{{Name}}Node> (reference.md §5), each defaulted to a no-op
    // by the interface itself. Uncomment and fill in only the ones this dictionary actually needs; delete
    // the rest rather than leaving an empty override. Fires for both folder and leaf instances alike — branch
    // on `entity is {{Name}}Folder` / `entity is {{Name}}` inside if the two need different handling.

    // public ValueTask OnOpenAsync({{Name}}Node entity, CancellationToken cancellationToken)
    // {
    // }

    // public ValueTask OnSaveAsync({{Name}}Node entity, bool isInsert, CancellationToken cancellationToken)
    // {
    // }

    // public ValueTask OnSubmitAsync({{Name}}Node entity, bool isInsert, CancellationToken cancellationToken)
    // {
    // }

    // public ValueTask OnDeleteAsync(Guid id, CancellationToken cancellationToken)
    // {
    // }
}

// Hand-written Mapperly stub over the abstract roots: the hierarchical CRUD service maps against
// {{Name}}Node/{{Name}}NodeDto, and [MapDerivedType] dispatches to the folder/leaf pair in each direction.
// Depth/Path/audit fields aren't on the Dtos - Source on ToEntity leaves them default, and the service
// restores Path/Depth on update. See reference.md §9.
[Mapper]
public partial class {{Name}}Mapper : IEntityMapper<{{Name}}Node, {{Name}}NodeDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    [MapDerivedType<{{Name}}Folder, {{Name}}FolderDto>]
    [MapDerivedType<{{Name}}, {{Name}}Dto>]
    public partial {{Name}}NodeDto ToDto({{Name}}Node entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    [MapDerivedType<{{Name}}FolderDto, {{Name}}Folder>]
    [MapDerivedType<{{Name}}Dto, {{Name}}>]
    public partial {{Name}}Node ToEntity({{Name}}NodeDto dto);
}

public class {{Name}}QueryDtoValidator : PagedQueryDtoValidator<{{Name}}QueryDto>;
