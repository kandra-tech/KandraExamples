using System.ComponentModel;       // [Description]
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using {{Config}}.Enums;

namespace {{Config}}.Application.Constants;

[KandraConstant]
// [Description] is mandatory on every Constant class — it's what the MCP server's describe_entity_type/
// list_entity_types tools surface to an AI agent as the constant's actual meaning (there's no [Caption]/Dto
// here to fall back on for a Constant). See ../reference.md.
[Description("TODO — one or two sentences: what this constant controls and what reads/uses it.")]
public sealed class {{Name}}Constant() : Constant<{{T}}>({{CompiledDefault}}), IConstant
{
    public static string Name => "{{Name}}";

    public static Guid TypeId { get; } = new({{TypeIdsClass}}.{{Name}});
}
