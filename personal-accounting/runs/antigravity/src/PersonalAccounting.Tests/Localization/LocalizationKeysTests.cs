using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Kandra.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace PersonalAccounting.Tests.Localization;

/// <summary>
/// A localization key with no resx entry is not an error anywhere: the page just renders the raw key (or its upper-cased form).
/// These tests look every key up through the module chain production uses (the platform's strings, then this configuration's), so
/// they list what is missing in the English resx. Only the neutral (English) file is required: a key missing from the uk/ru
/// files falls back to it. Run them after every entity you add: <c>dotnet test --filter LocalizationKeysTests</c>.
/// With <c>UPDATE_RESX=1</c> they also append each missing key to the English resx, with English text guessed from the key
/// (<c>Validation_AmountMustBePositive</c> becomes "Amount must be positive"), one formatted entry per key; the run still fails
/// and lists what it wrote, so read the guesses, fix the ones that are wrong, and run again.
/// <list type="bullet">
/// <item><description>Generated UI: every <c>Loc["..."]</c> in the client's <c>Generated.Net</c> (captions, titles, buttons, the
/// <c>Add{Name}</c>/<c>{Name}AddedSuccessfully</c>/<c>{Name}UpdatedSuccessfully</c> keys the pages derive from a Dto's name).</description></item>
/// <item><description>Source: the keys written in this configuration's own code (<c>[Caption]</c>, the form titles, <c>[FormTab]</c>,
/// <c>[FormSection]</c>, <c>[RegisterCaption]</c>, <c>[EnumCaption]</c>, <c>localizer["..."]</c> in validators and behaviors, the
/// <c>Nav_*</c> folder captions of the Interface). Keys built at runtime are not covered.</description></item>
/// </list>
/// </summary>
public sealed class LocalizationKeysTests
{
    private static readonly Regex GeneratedKey = new("""Loc\["(?<key>[^"]+)"\]""", RegexOptions.Compiled);

    // A key as the first string argument. Followed by ")", "]" or a further argument - but not by "true"/"isLiteral: true",
    // which marks literal text rather than a key.
    private static readonly Regex SourceKey = new(
        """(?:\[(?:Caption|RegisterCaption|EnumCaption|NavigationName|ListFormTitle|AddFormTitle|EditFormTitle|ViewFormTitle|ExecuteFormTitle|FormTab|FormSection)\(|[Ll]ocalizer\[|InterfaceNodeDefinition\.Folder\()"(?<key>[A-Za-z_][A-Za-z0-9_]*)"\s*(?:[\)\]]|,(?!\s*(?:isLiteral\s*:\s*)?true\b))""",
        RegexOptions.Compiled);

    [Fact]
    public void EveryLocKeyInTheGeneratedClientUiResolves()
    {
        var directory = FindSrcDirectory("PersonalAccounting.Client.MudBlazor", "Generated.Net");
        if (directory is null)
        {
            return; // built without the client next to the tests: nothing to scan
        }

        var keys = Scan(directory, GeneratedKey, "*.g.cs");
        Assert.NotEmpty(keys);
        AssertResolves(keys, "Generated UI references localization keys with no resx entry");
    }

    [Fact]
    public void EveryKeyWrittenInTheConfigurationsSourceResolves()
    {
        var src = FindSrcDirectory("PersonalAccounting.Forms", null);
        if (src is null)
        {
            return;
        }

        var keys = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var project in new[] { "PersonalAccounting.Domain", "PersonalAccounting.Forms", "PersonalAccounting.Application", "PersonalAccounting.Enums" })
        {
            var root = Path.Combine(Path.GetDirectoryName(src)!, project);
            foreach (var pair in Scan(root, SourceKey, "*.cs"))
            {
                keys.TryAdd(pair.Key, pair.Value);
            }
        }

        AssertResolves(keys, "Source code references localization keys with no resx entry");
    }

    private static SortedDictionary<string, string> Scan(string root, Regex pattern, string filePattern)
    {
        var keys = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(root, filePattern, SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.StartsWith("obj", StringComparison.Ordinal) || relative.StartsWith("bin", StringComparison.Ordinal)
                || relative.Contains("Generated.Net", StringComparison.Ordinal))
            {
                continue; // build output, and the generators' own files (the generated-UI test scans those on purpose)
            }

            foreach (Match match in pattern.Matches(File.ReadAllText(file)))
            {
                keys.TryAdd(match.Groups["key"].Value, Path.GetFileName(file));
            }
        }

        return keys;
    }

    private static void AssertResolves(SortedDictionary<string, string> keys, string message)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddLocalizationModule<Kandra.Localization.Kandra>();
        services.AddLocalizationModule<PersonalAccounting.Localization.PersonalAccounting>();
        var localizer = services.BuildServiceProvider().GetRequiredService<IStringLocalizer>();

        var missingKeys = keys.Where(k => localizer[k.Key].ResourceNotFound).ToList();
        var missing = missingKeys.Select(k => $"{k.Key} (first used in {k.Value})").ToList();

        if (missing.Count > 0 && Environment.GetEnvironmentVariable("UPDATE_RESX") == "1")
        {
            var written = AppendToEnglishResx(missingKeys.Select(k => k.Key));
            Assert.Fail(written.Count > 0
                ? $"UPDATE_RESX=1 added {written.Count} key(s) to the English resx with guessed text (placeholders such as {{0}} "
                    + "are not guessed). Check them, then run again:" + Environment.NewLine + string.Join(Environment.NewLine, written)
                : "UPDATE_RESX=1: these keys are already in the English resx (written earlier in this run); run again:"
                    + Environment.NewLine + string.Join(Environment.NewLine, missing));
        }

        Assert.True(missing.Count == 0, message + " (UPDATE_RESX=1 dotnet test --filter LocalizationKeysTests adds them):"
            + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    /// <summary>Appends the keys the English resx does not have yet before its closing tag, keeping its layout and line endings.</summary>
    private static List<string> AppendToEnglishResx(IEnumerable<string> keys)
    {
        var project = FindSrcDirectory("PersonalAccounting.Localization", null)
            ?? throw new InvalidOperationException("PersonalAccounting.Localization not found next to the tests.");
        var path = Path.Combine(project, "PersonalAccounting.resx");
        var text = File.ReadAllText(path);
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

        var entries = new StringBuilder();
        var written = new List<string>();
        foreach (var key in keys.Where(k => !text.Contains($"name=\"{k}\"", StringComparison.Ordinal)))
        {
            var value = GuessEnglish(key);
            entries.Append($"  <data name=\"{SecurityElement.Escape(key)}\" xml:space=\"preserve\">{newline}")
                .Append($"    <value>{SecurityElement.Escape(value)}</value>{newline}")
                .Append($"  </data>{newline}");
            written.Add($"{key} = {value}");
        }

        var end = text.LastIndexOf("</root>", StringComparison.Ordinal);
        var hasBom = File.ReadAllBytes(path) is [0xEF, 0xBB, 0xBF, ..];
        File.WriteAllText(path, text[..end] + entries + text[end..], new UTF8Encoding(encoderShouldEmitUTF8Identifier: hasBom));
        return written;
    }

    /// <summary>"ExpenseDto_CategoryId" -> "Category", "AddExpense" -> "Add expense": the last segment, split into lower-case words.</summary>
    private static string GuessEnglish(string key)
    {
        var name = key[(key.LastIndexOf('_') + 1)..];
        if (name.Length > 2 && name.EndsWith("Id", StringComparison.Ordinal))
        {
            name = name[..^2];
        }

        var words = Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ").ToLowerInvariant();
        return words.Length == 0 ? key : char.ToUpperInvariant(words[0]) + words[1..];
    }

    /// <summary>The <c>src</c> folder's child <paramref name="project"/> (with <paramref name="sub"/> under it, when given), found by walking up from the test binaries.</summary>
    private static string? FindSrcDirectory(string project, string? sub)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, project);
            if (Directory.Exists(candidate))
            {
                return sub is null ? candidate : Directory.Exists(Path.Combine(candidate, sub)) ? Path.Combine(candidate, sub) : null;
            }
        }

        return null;
    }
}
