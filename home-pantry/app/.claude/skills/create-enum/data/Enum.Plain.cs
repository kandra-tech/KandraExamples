using System.ComponentModel;       // [Description]
using Kandra.Attributes.Naming;

namespace {{Config}}.Enums;

// Explicit numeric values, always — EF stores the underlying integer, so reordering or renumbering
// silently re-labels every existing row. Append new members with the next unused number; never reuse one.
// [Description] is mandatory on every enum type the client can reference from a Dto — it's what the MCP
// server's describe_entity_type/list_entity_types tools surface to an AI agent as the enum's actual meaning
// (member-level [EnumCaption] is only a short UI label per value, not an explanation of the type itself; no
// per-member [Description] convention exists — see reference.md).
[Description("TODO — one or two sentences: what this enum represents and what its members mean collectively.")]
public enum {{Name}}
{
    // Model "not chosen" as a member rather than a nullable Dto property (the generated enum select has
    // no nullable branch). OPTIONAL — delete if every row always has a real value.
    [EnumCaption("Enum_{{Name}}_None")]
    None = 0,

    [EnumCaption("Enum_{{Name}}_{{MemberA}}")]
    {{MemberA}} = 1,

    [EnumCaption("Enum_{{Name}}_{{MemberB}}")]
    {{MemberB}} = 2,
}
