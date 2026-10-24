using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace PersonalAccounting.Tests.Localization;

/// <summary>
/// <c>LocalizationKeysTests</c> checks keys; an English literal passes it silently. These tests catch the two places where
/// literal UI text keeps turning up: report repositories (they return data - ids, numbers, dates, kinds - and the report
/// behavior turns kinds into words through the localizer) and notifications (<c>NotificationText.ForKey</c> is resolved in
/// the reader's language, <c>ForLiteral</c> is stored verbatim). See the localize-configuration and create-report skills.
/// </summary>
public sealed class CodeTextTests
{
    // A plain or interpolated string literal on one line.
    private static readonly Regex StringLiteral = new("""(?<!@)\$?"(?<text>(?:[^"\\\n]|\\.)*)"(?!")""", RegexOptions.Compiled);

    // Words a reader would see: two words with a space between them, or one capitalized word ("Unknown", "Work").
    private static readonly Regex Words = new("""[A-Za-z]{2,}\s+[A-Za-z]{2,}|^[A-Z][a-z]+$""", RegexOptions.Compiled);

    private static readonly Regex Interpolation = new("""\{[^{}]*\}""", RegexOptions.Compiled);

    private static readonly Regex LiteralNotification = new("""NotificationText\.ForLiteral\(\s*[$@]*\x22""", RegexOptions.Compiled);

    [Fact]
    public void ReportRepositoriesReturnData_NotText()
    {
        var repositories = Path.Combine(SrcDirectory(), "PersonalAccounting.Persistence", "Repositories");
        var found = new List<string>();
        foreach (var (file, number, line) in SourceLines(repositories))
        {
            foreach (Match literal in StringLiteral.Matches(line))
            {
                var text = Interpolation.Replace(literal.Groups["text"].Value, " ").Trim();
                if (Words.IsMatch(text))
                {
                    found.Add($"{file}:{number}: \"{literal.Groups["text"].Value}\"");
                }
            }
        }

        Assert.True(found.Count == 0,
            "A report repository writes text a user reads. Return a kind (an enum or flag) or an id instead and turn it into words "
            + "in the report behavior with IStringLocalizer:" + Environment.NewLine + string.Join(Environment.NewLine, found));
    }

    [Fact]
    public void NotificationsAreWordedThroughResourceKeys()
    {
        var src = SrcDirectory();
        var found = new List<string>();
        foreach (var project in new[] { "PersonalAccounting.Application", "PersonalAccounting.Persistence" })
        {
            foreach (var (file, number, line) in SourceLines(Path.Combine(src, project)))
            {
                if (LiteralNotification.IsMatch(line))
                {
                    found.Add($"{file}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(found.Count == 0,
            "A notification is written as a literal; use NotificationText.ForKey(\"Key\", args) so each reader sees it in their "
            + "own language:" + Environment.NewLine + string.Join(Environment.NewLine, found));
    }

    private static IEnumerable<(string File, int Number, string Line)> SourceLines(string root)
    {
        if (!Directory.Exists(root))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.StartsWith("obj", StringComparison.Ordinal) || relative.StartsWith("bin", StringComparison.Ordinal)
                || relative.Contains("Generated.Net", StringComparison.Ordinal))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                var code = line.TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal) || code.StartsWith("///", StringComparison.Ordinal)
                    || code.StartsWith('*') || code.StartsWith('['))
                {
                    continue; // comments and attributes (an attribute argument is a key or a format, not text)
                }

                yield return (relative, number, line);
            }
        }
    }

    private static string SrcDirectory([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", ".."));
}
