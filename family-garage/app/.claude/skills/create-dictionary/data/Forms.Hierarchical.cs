// TEMPLATE — hierarchical dictionary (folder/leaf TPH tree), Forms plane.
// Target: {{Config}}.Forms/Dictionaries/{{Name}}.cs
// {{Name}} names the LEAF type — the abstract root becomes {{Name}}NodeDto, the folder {{Name}}FolderDto.
// Placeholder legend: data/README.md. Concept reference: ../reference.md §2, §3, §4, §6, §8, §9.

using System.ComponentModel;       // [Description]
using System.Text.Json.Serialization;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;   // only needed if you add [Search]/[Dropdown] fields
using Kandra.Attributes.Entities;  // [TypeId]
using Kandra.Attributes.Layout;    // [TabRef]/[FormTab] — used below
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using {{Config}}.Enums;
using Microsoft.Extensions.Localization;

namespace {{Config}}.Forms.Dictionaries;

// Discriminator values are strings ("1"/"0"), not ints — the client's generated JsonInheritanceConverter
// reads the discriminator via JsonElement.GetString(), which throws on a JSON number token. This is a
// wire/JSON-layer detail only; unrelated to the entity-side EF discriminator column. See reference.md §9.
[TypeId({{TypeIdsClass}}.{{Name}})]  // the dictionary's one TypeId - on the root, never on the leaf/folder Dtos
[JsonPolymorphic(TypeDiscriminatorPropertyName = "isFolder")]
[JsonDerivedType(typeof({{Name}}FolderDto), "1")]
[JsonDerivedType(typeof({{Name}}Dto), "0")]
// The full [KandraDictionaryForm]/title/nav attribute set goes on the ABSTRACT ROOT, not the leaf.
[KandraDictionaryForm(Name = "{{PluralName}}", ValidatorType = typeof({{Name}}NodeValidator), QueryDtoType = typeof({{Name}}QueryDto))]
[EditFormTitle("Edit{{Name}}")]
[AddFormTitle("Create{{Name}}")]
[ViewFormTitle("View{{Name}}")]
[ListFormTitle("{{PluralName}}")]
[NavigationName("Nav_{{PluralName}}")]
// [Description] is mandatory on the abstract root — it's what the MCP server's describe_entity_type/
// list_entity_types tools surface to an AI agent as the entity's actual meaning; [Caption] is only a
// short UI label, not an explanation. DescriptionAttribute is inherited (default Inherited = true), so
// the leaf ({{Name}}Dto) picks this up automatically unless it declares its own override. See reference.md §2/§8.
[Description("TODO — one or two sentences: what a {{Name}} represents, and how the folder/leaf split works " +
    "(e.g. \"a row is either a group folder ({{Name}}FolderDto, no extra fields) or a real {{Name}} " +
    "({{Name}}Dto, with its own fields); only leaves are usable on a Document.\").")]
public abstract class {{Name}}NodeDto : HierarchicalDictionaryItemDto
{
}

// OPTIONAL — delete [FormTab]/[TabRef] entirely if this leaf's fields all belong on the one default tab.
[FormTab("Details")]
public class {{Name}}Dto : {{Name}}NodeDto
{
    // Add resx keys for every title/nav/tab key above (Edit{{Name}}, Create{{Name}}, View{{Name}},
    // {{PluralName}}, Nav_{{PluralName}}, Details) plus every field [Caption] to the resx files (English required,
    // uk/ru optional - localize-configuration) — reference.md §8.

    // Leaf-only field example. Add [Description] whenever the field's meaning isn't obvious from its
    // name/caption alone (skip it for a truly self-explanatory field — not every property needs one):
    // [TabRef("Details")] [Caption("SomeField")] [ListFormColumn(Filterable = false)]
    // [Description("What this field means and how it's used.")]
    // public string? SomeField { get; set; }
}

// Override [Description] here too — it's a genuinely different meaning from the leaf's ("organizational
// grouping only, not usable on a Document"), not just inherited boilerplate.
[Description("A grouping folder in the {{Name}} hierarchy - organizational only, not usable on a Document.")]
public class {{Name}}FolderDto : {{Name}}NodeDto
{
    // Folder-only fields (rare) go here — most hierarchical dictionaries leave this empty, matching Counterparty.
}

public class {{Name}}QueryDto : HierarchicalDictionaryQueryDto
{
}

/// Leaf-only rules. Not registered as a top-level Dto validator — InsertAsync/UpdateAsync always
/// validate against {{Name}}NodeDto (the polymorphic type actually flowing through the hierarchical
/// CRUD service), so this is wired in conditionally by {{Name}}NodeValidator below.
public class {{Name}}Validator : DictionaryValidator<{{Name}}Dto>
{
    public {{Name}}Validator(IStringLocalizer localizer) : base(localizer)
    {
        // Leaf-specific FluentValidation rules here.
    }
}

/// Folder-only rules — often identical to the leaf's; kept separate so a future folder-specific rule
/// has somewhere to go without touching {{Name}}Validator.
public class {{Name}}FolderValidator : DictionaryValidator<{{Name}}FolderDto>
{
    public {{Name}}FolderValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}

/// The validator actually registered for the hierarchical CRUD service's Dto ({{Name}}NodeDto), and the
/// one wired to <FluentValidator/> on the shared Form.razor. Covers the shared Code/Name rules directly
/// (via DictionaryValidator<T>'s base ctor), then dispatches to the folder/leaf validator by runtime type.
public class {{Name}}NodeValidator : DictionaryValidator<{{Name}}NodeDto>
{
    public {{Name}}NodeValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x as {{Name}}Dto)
            .SetValidator(new {{Name}}Validator(localizer))
            .When(x => x is {{Name}}Dto);

        RuleFor(x => x as {{Name}}FolderDto)
            .SetValidator(new {{Name}}FolderValidator(localizer))
            .When(x => x is {{Name}}FolderDto);
    }
}
