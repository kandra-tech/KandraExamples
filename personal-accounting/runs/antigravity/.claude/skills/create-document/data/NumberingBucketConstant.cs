// TEMPLATE — one numbering-bucket constant, its own partial-class file.
// Target: {{Config}}.Domain/Numbering/{{NumberingBucketsClass}}.{{Name}}.cs
// Placeholder legend: data/README.md. Concept reference: ../reference.md §8.
//
// {{NumberingBucketsClass}} (NumberingBuckets in this repo) is split one constant per file specifically so
// adding a new dictionary/document never requires editing the shared file — only adding a new file next to
// it. Do NOT add this constant into the class's own main file ({{NumberingBucketsClass}}.cs) — that file
// stays permanently empty of members (just the `partial` class declaration + its doc comment).
//
// This is just the bucket's stable NAME — every current document appends a ":{yy}" year suffix at the
// GetNextAsync call site itself (reference.md §8), not encoded here. Skip this file entirely if this
// document hand-types its Code instead of auto-numbering.

namespace {{Config}}.Domain.Numbering;

public static partial class {{NumberingBucketsClass}}
{
    public const string {{Name}} = "{{Name}}";
}
