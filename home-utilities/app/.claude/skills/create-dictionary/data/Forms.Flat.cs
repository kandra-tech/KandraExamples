// TEMPLATE — flat dictionary, Forms plane.
// Target: {{Config}}.Forms/Dictionaries/{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §2, §3, §4, §6, §8.

using System.ComponentModel;       // [Description]
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;   // only needed if you add [Search]/[Dropdown] fields
using Kandra.Attributes.Entities;  // [TypeId]
using Kandra.Attributes.Layout;    // only needed if you add [ListFormColumn]/[TabRef]/[FormTab] fields
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using {{Config}}.Enums;
using Microsoft.Extensions.Localization;

namespace {{Config}}.Forms.Dictionaries;

[TypeId({{TypeIdsClass}}.{{Name}})]
[KandraDictionaryForm(Name = "{{PluralName}}", ValidatorType = typeof({{Name}}Validator), QueryDtoType = typeof({{Name}}QueryDto))]
[EditFormTitle("Edit{{Name}}")]
[AddFormTitle("Create{{Name}}")]
[ViewFormTitle("View{{Name}}")]
[ListFormTitle("{{PluralName}}")]
[NavigationName("Nav_{{PluralName}}")]
// [Description] is mandatory on every Dto — it's what the MCP server's describe_entity_type/
// list_entity_types tools surface to an AI agent as the entity's actual meaning; [Caption] is only a
// short UI label, not an explanation. See reference.md §2/§8.
[Description("TODO — one or two sentences: what a {{Name}} represents in the domain and where/how it's used.")]
public class {{Name}}Dto : DictionaryDto
{
    // Add resx keys for every title/nav key above (Edit{{Name}}, Create{{Name}}, View{{Name}},
    // {{PluralName}}, Nav_{{PluralName}}) to the resx files (English required, uk/ru optional) — reference.md §8.

    // OPTIONAL — only if Code/Name need their own caption or list-column behavior; most dictionaries
    // should just inherit both from DictionaryDto (Loc["Code"]/Loc["Name"]) unmodified.
    // [Caption("SomeResourceKey")]
    // [ListFormColumn(Sortable = false, Filterable = false)]
    // public override string Code { get; set; }

    // Field example — server-required, filterable/sortable list column. Add [Description] whenever the
    // field's meaning isn't obvious from its name/caption alone (skip it for a truly self-explanatory
    // field — not every property needs one):
    // [Required] [Caption("SomeField")] [ListFormColumn]
    // [Description("What this field means and how it's used.")]
    // public string SomeField { get; set; }

    // Field example — FK to another (large) dictionary via server-filtered autocomplete:
    // [Required] [Caption("OtherThing")] [Search(typeof(OtherThingDto))]
    // [Description("What this reference means and how it's used.")]
    // public Guid OtherThingId { get; set; }

    // Field example — FK to a small, fixed-list dictionary via plain dropdown:
    // [Required] [Caption("Category")] [Dropdown(typeof(CategoryDto))]
    // [Description("What this reference means and how it's used.")]
    // public Guid CategoryId { get; set; }
}

public class {{Name}}QueryDto : DictionaryQueryDto
{
}

public class {{Name}}Validator : DictionaryValidator<{{Name}}Dto>
{
    public {{Name}}Validator(IStringLocalizer localizer) : base(localizer)
    {
        // FluentValidation RuleFor(...) calls here, using localizer[...] for messages.
        // base(localizer) already covers Code/Name NotEmpty — don't duplicate those.
    }
}

// {{Name}}QueryDtoValidator lives in the Application-plane file (Application.Flat.cs), not here —
// PagedQueryDtoValidator<T> is in Kandra.Application.Validation, and query-dto validators follow the
// Behavior/Mapper file, not the Dto/Validator file, in this engine's existing examples (Branch, Counterparty).
