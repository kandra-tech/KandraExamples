using System.Runtime.CompilerServices;
using System.Text.Json;
using Kandra.DataImport;
using Kandra.Enums;
using FamilyGarage.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyGarage.Tests.ImportPackages;

/// <summary>
/// The embedded demo package (a valid package with no steps yet) is registered, validates and applies on an empty database
/// with every record imported and every document posted, and the generated JSON Schema matches the committed snapshot the
/// package's <c>$schema</c> points at. Set UPDATE_IMPORT_SCHEMA=1 to rewrite
/// the snapshot after an intentional model change (a new entity or field).
/// </summary>
public sealed class DemoPackageTests : IDisposable
{
    private const string DemoPackage = "familygarage-demo";

    private readonly FamilyGarageTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public void DemoPackage_IsRegistered_AndValidates()
    {
        using var scope = _host.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IImportPackageRegistry>();

        var info = Assert.Single(registry.List(), p => p.Id == DemoPackage);
        Assert.Equal(ImportOrigin.DemoSeed, info.Origin);

        using var stream = registry.Open(DemoPackage)!;
        var read = ImportPackageReader.Read(new StreamReader(stream).ReadToEnd());
        Assert.False(read.HasErrors, string.Join("; ", read.Diagnostics.Select(d => d.Message)));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ImportPackageValidator>().Validate(read.Package!));
    }

    // Validating checks the shape; only applying it runs the posting rules, so a document the demo data cannot post (a
    // rule it breaks, a crash with two lines of the same kind) fails here instead of on the first demo installation.
    [Fact]
    public async Task DemoPackage_Applies_OnAnEmptyDatabase()
    {
        var report = await _host.InScopeAsync(async sp =>
        {
            using var stream = sp.GetRequiredService<IImportPackageRegistry>().Open(DemoPackage)!;
            return await sp.GetRequiredService<IDataImportService>().ImportAsync(stream, new ImportOptions(), CancellationToken.None);
        });

        var failures = report.Diagnostics.Where(d => d.Severity == ImportDiagnosticSeverity.Error).Select(d => $"{d.Path}: {d.Message}")
            .Concat(report.Items.Where(i => i.Outcome == ImportItemOutcome.Failed).Select(i => $"{i.Key}: {i.Message}"));
        Assert.False(report.HasErrors, "The demo package does not apply: " + string.Join("; ", failures));
    }

    // Demo data written with fixed dates ages: in a month the "overdue" item is no longer overdue, the policy no longer
    // expires soon, and the documents drop out of the lists' default "recent" filter. Date tokens resolve when the package
    // is applied: "$today-30d", "$today+14d", "$now". A constant's "effectiveFrom" is the one fixed date that belongs here.
    private static readonly System.Text.RegularExpressions.Regex FixedDate =
        new("""(?<property>"[A-Za-z0-9_]+")\s*:\s*"(?<value>\d{4}-\d{2}-\d{2}[^"]*)""");

    [Fact]
    public void DemoPackage_UsesDateTokens_NotFixedDates()
    {
        using var scope = _host.CreateScope();
        using var stream = scope.ServiceProvider.GetRequiredService<IImportPackageRegistry>().Open(DemoPackage)!;
        var fixedDates = FixedDate.Matches(new StreamReader(stream).ReadToEnd())
            .Where(m => m.Groups["property"].Value != "\"effectiveFrom\"")
            .Select(m => $"{m.Groups["property"].Value}: \"{m.Groups["value"].Value}\"")
            .ToList();

        Assert.True(fixedDates.Count == 0,
            $"The demo package has {fixedDates.Count} fixed date(s); use date tokens ($today-30d, see author-import-package): "
            + string.Join(", ", fixedDates.Take(10)));
    }

    [Fact]
    public void Schema_IsDraft2020_AndMatchesTheCheckedInSnapshot()
    {
        using var scope = _host.CreateScope();
        var schema = scope.ServiceProvider.GetRequiredService<IImportSchemaProvider>().GetSchema();

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", schema["$schema"]!.GetValue<string>());

        var actual = schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n";
        var path = SnapshotPath();

        if (Environment.GetEnvironmentVariable("UPDATE_IMPORT_SCHEMA") == "1" || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
        }

        Assert.Equal(File.ReadAllText(path).ReplaceLineEndings("\n"), actual.ReplaceLineEndings("\n"));
    }

    private static string SnapshotPath([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "FamilyGarage.Application", "ImportPackages", "familygarage.import.schema.json"));
}
