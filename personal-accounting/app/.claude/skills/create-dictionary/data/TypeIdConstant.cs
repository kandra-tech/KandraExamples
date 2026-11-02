// TEMPLATE — one type-id constant, its own partial-class file.
// Target: {{Config}}.Enums/{{TypeIdsClass}}/{{TypeIdsClass}}.{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §6.
//
// {{TypeIdsClass}} (PersonalAccountingTypeIds in this repo) is split one constant per file specifically so adding a new
// dictionary/document/enum never requires editing the shared file — only adding a new file next to it. Do
// NOT add this constant into the class's own main file ({{TypeIdsClass}}.cs) — that file stays permanently
// empty of members (just the `partial` class declaration + its doc comment).
//
// For a HIERARCHICAL dictionary, create TWO instances of this file — one with {{Name}} = the leaf name, one
// with {{Name}} = the leaf name + "Folder" — since the leaf and folder each mint their own identity.

namespace {{Config}}.Enums;

public static partial class {{TypeIdsClass}}
{
    public const string {{Name}} = "{{FreshGuid}}";
}
