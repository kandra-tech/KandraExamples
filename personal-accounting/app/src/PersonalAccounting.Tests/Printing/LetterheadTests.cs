using Kandra.Application.Abstractions.Services;
using Kandra.Printing;
using PersonalAccounting.Application.Constants;
using PersonalAccounting.Application.Printing;
using PersonalAccounting.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace PersonalAccounting.Tests.Printing;

/// <summary>
/// A fresh configuration prints with a letterhead once the company constants are set: the frame provider registered by
/// <c>AddPersonalAccountingApplication</c> replaces the engine's no-letterhead default and reads the Company* constants as
/// of the document's date. (The template has no document of its own to render; the PDF itself is covered by the engine's
/// and KandraWms's print tests.)
/// </summary>
public sealed class LetterheadTests : IDisposable
{
    private readonly PersonalAccountingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task TheTemplateFrameProvider_ReplacesTheEnginesDefault()
    {
        using var scope = _host.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IPrintFrameProvider>();

        Assert.IsType<PersonalAccountingPrintFrameProvider>(provider);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task WithoutACompanyName_ThereIsNoLetterhead()
    {
        using var scope = _host.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IPrintFrameProvider>();

        Assert.Null(await provider.GetLetterheadAsync(new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None));
    }

    [Fact]
    public async Task WithTheCompanyConstantsSet_TheLetterheadCarriesThem_AsOfTheDocumentDate()
    {
        using var scope = _host.CreateScope();
        var constants = scope.ServiceProvider.GetRequiredService<IConstantsManageService>();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await constants.SetAsync<string>(CompanyNameConstant.Name, "Acme Ltd", from, CancellationToken.None);

        var provider = scope.ServiceProvider.GetRequiredService<IPrintFrameProvider>();
        var letterhead = await provider.GetLetterheadAsync(new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);

        Assert.NotNull(letterhead);
        Assert.Equal("Acme Ltd", letterhead.CompanyName);
        Assert.Null(letterhead.Logo);

        // A document dated before the company was configured prints no letterhead: constants are read as of its date.
        Assert.Null(await provider.GetLetterheadAsync(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None));
    }
}
