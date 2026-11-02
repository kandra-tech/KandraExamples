using Kandra.Application.Abstractions.Services;
using Kandra.Persistence.Abstractions.Registers;
using PersonalAccounting.Domain.Entities.Registers;          // {{Register}} types
using PersonalAccounting.Forms.Dictionaries;                  // {{Dictionary}}Dto
using PersonalAccounting.Forms.Documents;                     // {{Document}}Dto
using PersonalAccounting.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace PersonalAccounting.Tests.Documents;

/// <summary>
/// Posting rules of {{Document}}: submit writes the register, an edit of a posted document re-posts it, unsubmit reverses it,
/// delete is refused while posted. Entity names below are placeholders - replace them (and the fields) with yours.
/// Every step runs in its own scope (<see cref="PersonalAccountingTestHost.InScopeAsync{T}"/>): updating a document in the
/// scope that inserted or read it fails with a change-tracker conflict. This shape was verified against a real document.
/// </summary>
public sealed class {{Document}}PostingTests : IDisposable
{
    private readonly PersonalAccountingTestHost _host = new();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public void Dispose() => _host.Dispose();

    private static DateTime Day(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static IDocumentCrudService<{{Document}}Dto, {{Document}}QueryDto> Documents(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<{{Document}}Dto, {{Document}}QueryDto>>();

    private Task<decimal> Total(Guid thingId) => _host.InScopeAsync(async sp =>
    {
        var rows = await sp.GetRequiredService<IBalanceReader<{{Register}}Movement, {{Register}}>>()
            .Balances(q => q.Where(b => b.Dimensions.{{Dictionary}}Id == thingId), Ct);
        return rows.Sum(r => r.Resources.Amount);
    });

    private async Task<(Guid Thing, Guid Document)> PostedDocument(decimal amount)
    {
        var thing = await _host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<{{Dictionary}}Dto, {{Dictionary}}QueryDto>>()
            .InsertAsync(new {{Dictionary}}Dto { Code = "T1", Name = "Thing 1" }, Ct));
        var created = await _host.InScopeAsync(sp => Documents(sp).InsertAsync(
            new {{Document}}Dto { Code = "D1", Date = Day(2026, 10, 5), {{Dictionary}}Id = thing.Id, Amount = amount }, Ct));
        await _host.InScopeAsync(sp => Documents(sp).SubmitAsync(created.Id, Ct));
        return (thing.Id, created.Id);
    }

    [Fact]
    public async Task Submit_PostsTheRegister_EditRePosts_UnsubmitReverses()
    {
        var (thing, document) = await PostedDocument(100m);
        Assert.Equal(-100m, await Total(thing));

        // Edit while posted: read it back (IsActive = true), change it, save it - the engine re-posts with the new values.
        var posted = await _host.InScopeAsync(sp => Documents(sp).GetByIdAAsync(document, Ct));
        posted.Amount = 250m;
        await _host.InScopeAsync(sp => Documents(sp).UpdateAsync(posted, Ct));
        Assert.Equal(-250m, await Total(thing));

        await _host.InScopeAsync(sp => Documents(sp).UnsubmitAsync(document, Ct));
        Assert.Equal(0m, await Total(thing));

        await _host.InScopeAsync(sp => Documents(sp).DeleteAsync(document, Ct));   // allowed once unposted
    }

    [Fact]
    public async Task Delete_IsRefusedWhilePosted()
    {
        var (_, document) = await PostedDocument(100m);

        await _host.InScopeAsync(sp =>
            Assert.ThrowsAsync<Kandra.Domain.Exceptions.InvalidStateException>(() => Documents(sp).DeleteAsync(document, Ct)));
    }
}
