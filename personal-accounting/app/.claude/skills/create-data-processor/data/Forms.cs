using System.ComponentModel;       // [Description]
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Naming;
using Kandra.Forms.DataProcessors;
using Microsoft.Extensions.Localization;

namespace {{Config}}.Forms.DataProcessors;

// ── Input Dto — ValidatorType is NOT optional in practice, see ../reference.md §1: omit it and the generated
// page silently never appears (no error, no diagnostic). ──────────────────────────────────────────────────
[KandraDataProcessorForm(Name = "{{Name}}", ValidatorType = typeof({{Name}}Validator), ResultDtoType = typeof({{Name}}Result))]
[ExecuteFormTitle("{{Name}}")] // declared for convention/forward-compat — currently unconsumed, see reference.md §1/§5
// [Description] is mandatory on both this class and {{Name}}Result below — it's what the MCP server's
// describe_entity_type/list_entity_types tools surface to an AI agent as the entity's actual meaning;
// [Caption] is only a short UI label, not an explanation. See reference.md §1/§5.
[Description("TODO — one or two sentences: what running this data processor does.")]
public class {{Name}}Dto : IDataProcessorInputDto
{
    // Input parameters, if any — [Caption("...")] + the usual editor attributes (same vocabulary as any other
    // entity kind's fields — [Dropdown]/[Search]/[MultiSelect]/[DateEditor]/[DateRangeEditor]/...). It's fine
    // for this to have zero properties: the generated page still renders with just an Execute button. Add
    // [Description] whenever a field's meaning isn't obvious from its name/caption alone:
    [Caption("{{InputFieldCaption}}")]
    [DateEditor(DateEditorKind.Date)]
    [Description("What this input parameter controls.")]
    public DateTime? {{InputField}} { get; set; }

    // OPTIONAL — a plain bool flag with a default, no editor attribute needed:
    // [Caption("{{BoolFieldCaption}}")]
    // [Description("What this flag controls when true/false.")]
    // public bool {{BoolField}} { get; set; } = true;
}

// ── Result Dto — only [Caption]-carrying properties are ever rendered (opposite default from the input Dto
// above); a property with no [Caption] (e.g. a technical id) stays off the generated result panel entirely. ──
[Description("TODO — one sentence: what running {{Name}} produces.")]
public class {{Name}}Result : IDataProcessorResultDto
{
    // No [Caption] on purpose — a technical id the result panel shouldn't show. [Description] is still worth
    // adding since it's read independently of [Caption].
    [Description("What this id refers to.")]
    public Guid {{CreatedEntityIdField}} { get; set; }

    [Order(1)]
    [Caption("{{ResultFieldCaption}}")]
    [Description("What this result field means.")]
    public string {{ResultField}} { get; set; }
}

// ── Validator — plain FluentValidation AbstractValidator<T>, mandatory even if the input has no fields ──────
public class {{Name}}Validator : AbstractValidator<{{Name}}Dto>
{
    public {{Name}}Validator(IStringLocalizer localizer)
    {
        // constrain input parameters here, if any — e.g.:
        // RuleFor(x => x.{{InputField}})
        //     .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1))
        //     .When(x => x.{{InputField}}.HasValue)
        //     .WithMessage(localizer["DateCannotBeInFuture"]);
    }
}
