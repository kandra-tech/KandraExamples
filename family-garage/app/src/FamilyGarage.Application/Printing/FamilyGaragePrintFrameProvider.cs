using System.Collections.Concurrent;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Abstractions.Services.Blobs;
using Kandra.Printing;
using FamilyGarage.Application.Constants;
using Microsoft.Extensions.Logging;

namespace FamilyGarage.Application.Printing;

/// <summary>
/// The company letterhead of every letterhead-framed print form, from the company constants as of the document's date: the
/// name and the logo valid on that date (the requisites and contacts lines stay empty).
/// Registered after <c>AddKandraPrinting()</c>, replacing the engine's no-letterhead default. Extend it (and the
/// <c>Company*</c> constants) with whatever else your letterhead should carry: a tax code, an address, a phone, bank
/// requisites (see the extend-company-settings skill).
/// </summary>
public sealed class FamilyGaragePrintFrameProvider(
    IConstantsService constants,
    IBlobService blobs,
    ILogger<FamilyGaragePrintFrameProvider> logger) : IPrintFrameProvider
{
    /// <summary>Logo bytes by blob reference id. A reference's content never changes (a new logo is a new reference), so the
    /// cache needs no invalidation; it is static because the provider itself is scoped.</summary>
    private static readonly ConcurrentDictionary<Guid, byte[]> LogoCache = new();

    public async ValueTask<PrintLetterhead?> GetLetterheadAsync(DateTime asOf, CancellationToken cancellationToken)
    {
        var name = (await constants.GetAsync<CompanyNameConstant>(asOf, cancellationToken)).Value;
        if (string.IsNullOrWhiteSpace(name))
        {
            return null; // company not configured yet: print without a letterhead rather than an empty block
        }

        var logoId = (await constants.GetAsync<CompanyLogoConstant>(asOf, cancellationToken)).Value;

        // The two lines under the name (requisites: tax code; contacts: address, phone) stay empty until the
        // extend-company-settings skill adds those constants.
        return new PrintLetterhead(name.Trim(), null, null, await ReadLogoAsync(logoId, cancellationToken));
    }

    /// <summary>A missing or unreadable blob means no logo, with a warning in the log: a print never fails over its decoration.</summary>
    private async Task<byte[]?> ReadLogoAsync(Guid? logoId, CancellationToken cancellationToken)
    {
        if (logoId is not { } id)
        {
            return null;
        }

        if (LogoCache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        try
        {
            var (content, _, _) = await blobs.OpenReadAsync(id, cancellationToken);
            await using (content)
            {
                using var buffer = new MemoryStream();
                await content.CopyToAsync(buffer, cancellationToken);
                var bytes = buffer.ToArray();
                LogoCache[id] = bytes;
                return bytes;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "The company logo (blob reference {LogoId}) could not be read; printing without it.", id);
            return null;
        }
    }
}
