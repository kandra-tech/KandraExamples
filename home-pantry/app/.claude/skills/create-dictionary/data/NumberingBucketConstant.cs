// TEMPLATE — one numbering-bucket constant, its own partial-class file.
// Target: {{Config}}.Domain/Numbering/{{NumberingBucketsClass}}.{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §7.
//
// {{NumberingBucketsClass}} (NumberingBuckets in this repo) is split one constant per file specifically so
// adding a new dictionary/document never requires editing the shared file — only adding a new file next to
// it. Do NOT add this constant into the class's own main file ({{NumberingBucketsClass}}.cs) — that file
// stays permanently empty of members (just the `partial` class declaration + its doc comment).
//
// For a HIERARCHICAL dictionary, create TWO instances of this file — one for the leaf bucket name, one for
// the folder bucket name (e.g. {{Name}} and {{Name}}Folder) — since leaf and folder usually number
// separately. Skip this file entirely if the dictionary hand-types its Code instead of auto-numbering (see
// Currency in reference.md §9).

namespace {{Config}}.Domain.Numbering;

public static partial class {{NumberingBucketsClass}}
{
    public const string {{Name}} = "{{Name}}";
}
