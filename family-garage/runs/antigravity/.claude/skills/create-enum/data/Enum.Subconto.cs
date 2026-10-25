using System.ComponentModel;       // [Description]
using Kandra.Attributes.Accounting;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;

namespace {{Config}}.Enums;

// Subconto-backing enum: usable as EnumSubconto<{{Name}}> in the chart of accounts. Needs a type-level
// [TypeId] (a constant from its own new file — see TypeIdConstant.cs) and a [SubcontoId] on every member
// that can be posted. Every GUID below must be freshly generated, unique, and — once posted against —
// permanent. The member-level [SubcontoId] GUIDs are unrelated to the enum-level [TypeId].
[TypeId({{TypeIdsClass}}.{{Name}})]
// [Description] is mandatory on every enum type the client can reference from a Dto — it's what the MCP
// server's describe_entity_type/list_entity_types tools surface to an AI agent as the enum's actual meaning
// (member-level [EnumCaption] is only a short UI label per value, not an explanation of the type itself; no
// per-member [Description] convention exists — see reference.md).
[Description("TODO — one or two sentences: what this enum represents and what its members mean collectively.")]
public enum {{Name}}
{
    [EnumCaption("Enum_{{Name}}_{{MemberA}}")]
    [SubcontoId("{{FreshGuidA}}")]
    {{MemberA}} = 0,

    [EnumCaption("Enum_{{Name}}_{{MemberB}}")]
    [SubcontoId("{{FreshGuidB}}")]
    {{MemberB}} = 1,
}
