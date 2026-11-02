// TEMPLATE — lineless (header-only) document, Forms plane.
// Target: {{Config}}.Forms/Documents/{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §2, §3, §4, §9.

using System.ComponentModel;       // [Description]
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;  // [TypeId]
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Documents;
using Kandra.Forms.Querying;
using Kandra.Validators.Documents;
using {{Config}}.Enums;
using Microsoft.Extensions.Localization;

namespace {{Config}}.Forms.Documents;

[TypeId({{TypeIdsClass}}.{{Name}})]
[KandraDocumentForm(Name = "{{PluralName}}", ValidatorType = typeof({{Name}}Validator), QueryDtoType = typeof({{Name}}QueryDto))]
[EditFormTitle("Edit{{Name}}")]
[AddFormTitle("Create{{Name}}")]
[ViewFormTitle("View{{Name}}")]
[ListFormTitle("{{PluralName}}")]
[NavigationName("Nav_{{PluralName}}")]
// [Description] is mandatory on every Dto — it's what the MCP server's describe_entity_type/
// list_entity_types tools surface to an AI agent as the entity's actual meaning; [Caption] is only a
// short UI label, not an explanation. See reference.md §2/§9.
[Description("TODO — one or two sentences: what a {{Name}} represents and what submitting it does " +
    "(which registers it writes / what it posts, if anything).")]
public class {{Name}}Dto : DocumentDto
{
    // Add resx keys for every title/nav key above, PLUS {{Name}}AddedSuccessfully/
    // {{Name}}UpdatedSuccessfully — reference.md §9. The resx files (English required, uk/ru optional).

    [Caption("Description")]
    [ListFormColumn]
    [Description("Free-text note about this {{Name}}.")]
    public string? Description { get; set; }

    // Header-only fields go here — no Lines property at all for a lineless document. See
    // AccountTransaction (reference.md §10) for the real worked example: account-code + up to 5
    // subconto-value fields per side, via [AccountDialog]/[SubcontoEditor]
    // (add-account-subconto-field skill covers those two attributes, not templated here).

    // [Required] [Caption("SomeAccount")] [AccountDialog]
    // [Description("What this account reference means and how it's used.")]
    // public string? SomeAccountCode { get; set; }
}

public class {{Name}}QueryDto : DocumentQueryDto
{
}

public class {{Name}}Validator : DocumentValidator<{{Name}}Dto>
{
    public {{Name}}Validator(IStringLocalizer localizer) : base(localizer)
    {
        // base(localizer) already covers Code NotEmpty — don't duplicate that.
        // RuleFor(x => x.SomeAccountCode).NotEmpty().WithMessage(localizer["SomeAccountRequired"]);
    }
}

// {{Name}}QueryDtoValidator lives in the Application-plane file (Application.Lineless.cs), not here.
