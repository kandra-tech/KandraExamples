using System.Globalization;
using System.Text.Json;
using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Constants;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Abstractions.Services.Blobs;
using Kandra.Attributes.Application;
using Kandra.Domain.Exceptions;
using FamilyGarage.Application.Constants;
using FamilyGarage.Forms.DataProcessors;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Application.DataProcessors;

/// <summary>
/// One form over the company's constants (<c>Company*</c>: name and logo; the extend-company-settings skill adds more): New fills
/// it with the values valid now, Execute writes only the ones that changed, as new dated values from the effective date. A value counts as
/// changed when it differs from the one valid <i>on the effective date</i> or from the one valid <i>now</i> (what the form showed): the first
/// makes a backdated save of an unchanged value write it (a company configured today, applied to documents of last month), the second catches a
/// change back to an older value while a later row exists. A value that matches both is left alone.
/// </summary>
/// <remarks>
/// The logo is a blob reference, and the constant is history-aware: a reprint of a document dated before a replacement must still find the logo
/// that was valid on its date. So every logo ever set stays attached to the constant (<c>entityTag = "CompanyLogo"</c>) and only the new one is
/// promoted; a blob nothing references would be purged. Emptying the logo writes a null from the date on and deletes nothing.
/// </remarks>
[KandraDataProcessorBehavior(typeof(CompanySettingsDto))]
public class CompanySettingsBehavior(
    IBlobService blobs,
    IConstantsManageService constants,
    ICurrentTimeZone currentTimeZone,
    IStringLocalizer localizer)
    : IDataProcessorBehavior<CompanySettingsDto, CompanySettingsResult>
{
    public const string LogoEntityTag = "CompanyLogo";

    /// <summary>The largest logo accepted: it is read into memory on every cold print, so a letterhead image has no business being bigger.</summary>
    public const int MaxLogoBytes = 1024 * 1024;

    public string ObjectName => "CompanySettings";

    public async ValueTask OnNewAsync(CompanySettingsDto input, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        input.EffectiveFrom = now.Date;
        input.Name = await ReadAsync<CompanyNameConstant>(now, cancellationToken);
        input.Logo = (await constants.GetAsync<CompanyLogoConstant>(now, cancellationToken)).Value;
    }

    public async Task<CompanySettingsResult> OnProcessAsync(CompanySettingsDto input, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var at = input.EffectiveFrom == default ? now.Date : input.EffectiveFrom;
        var changed = new List<string>();

        // The image is checked before the first write; a stale reference is only discovered by the sync, which is why the logo is written first.
        var logoChanged = input.Logo != (await constants.GetAsync<CompanyLogoConstant>(at, cancellationToken)).Value
            || input.Logo != (await constants.GetAsync<CompanyLogoConstant>(now, cancellationToken)).Value;
        if (logoChanged && input.Logo is { } newLogo)
        {
            await ValidateImageAsync(newLogo, cancellationToken);
        }

        // The logo first: it is the write most likely to be refused (a stale upload), and the text fields should not be half-saved around it.
        if (logoChanged)
        {
            await WriteLogoAsync(input.Logo, at, cancellationToken);
            changed.Add(localizer["CompanySettings_LogoLabel"].Value);
        }

        await WriteAsync<CompanyNameConstant>("CompanyName", input.Name, at, now, changed, cancellationToken);

        return new CompanySettingsResult
        {
            Message = changed.Count == 0
                ? localizer["CompanySettings_NothingChanged"].Value
                : localizer["CompanySettings_Saved", LocalDate(at), string.Join(", ", changed)].Value,
        };
    }

    /// <summary>The effective instant as the caller's calendar date: the picker sent local midnight as UTC, which is the previous day for a user east of Greenwich.</summary>
    private string LocalDate(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), currentTimeZone.TimeZone ?? TimeZoneInfo.Utc).ToString("d", CultureInfo.CurrentCulture);

    // ------------------------------------------------------------------------------------------------------------------ text constants

    private async Task<string?> ReadAsync<C>(DateTime asOf, CancellationToken cancellationToken) where C : Constant<string>, IConstant, new() =>
        NullIfBlank((await constants.GetAsync<C>(asOf, cancellationToken)).Value);

    /// <summary>Writes the constant at <paramref name="at"/> when the wanted value differs from the one valid on that date or from the one valid now.</summary>
    private async Task WriteAsync<C>(string labelKey, string? wanted, DateTime at, DateTime now, List<string> changed, CancellationToken cancellationToken)
        where C : Constant<string>, IConstant, new()
    {
        var wantedValue = NullIfBlank(wanted);
        if (wantedValue == await ReadAsync<C>(at, cancellationToken) && wantedValue == await ReadAsync<C>(now, cancellationToken))
        {
            return;
        }

        await constants.SetAsync<string>(C.Name, wantedValue ?? string.Empty, at, cancellationToken);
        changed.Add(localizer[labelKey].Value);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // ------------------------------------------------------------------------------------------------------------------ logo

    private async Task WriteLogoAsync(Guid? logoId, DateTime at, CancellationToken cancellationToken)
    {
        if (logoId is { } id)
        {
            // Every value ever set stays attached (see the class remarks), plus the new one, which this call promotes to permanent.
            var history = await constants.GetHistoryAsync(CompanyLogoConstant.Name, cancellationToken);
            var keep = history.Select(record => ReadLogoId(record.ValueJson)).OfType<Guid>().Append(id).Distinct().ToList();
            var sync = await blobs.SyncEntityReferencesAsync(CompanyLogoConstant.TypeId, CompanyLogoConstant.TypeId, keep, cancellationToken, LogoEntityTag);
            if (sync.Rejected.Contains(id))
            {
                // An unknown, tombstoned or already-attached-elsewhere reference. (A rejected *older* logo is not fatal: it was
                // already gone before this run, and refusing a new logo over it would leave the company unable to change it.)
                throw new InvalidStateException(localizer["CompanySettings_LogoRejected"]);
            }
        }

        // Writes (and saves) the constant, together with the reference changes above, which share its unit of work.
        await constants.SetAsync<Guid?>(CompanyLogoConstant.Name, logoId, at, cancellationToken);
    }

    /// <summary>PNG or JPEG by the file's own bytes (the declared content type and the file name are the client's word), at most 1 MB.</summary>
    private async Task ValidateImageAsync(Guid logoId, CancellationToken cancellationToken)
    {
        var (content, _, _) = await blobs.OpenReadAsync(logoId, cancellationToken);
        await using (content)
        {
            var buffer = new byte[MaxLogoBytes + 1];
            var read = 0;
            int n;
            while (read < buffer.Length && (n = await content.ReadAsync(buffer.AsMemory(read), cancellationToken)) > 0)
            {
                read += n;
            }

            if (read > MaxLogoBytes)
            {
                throw new InvalidStateException(localizer["CompanySettings_LogoTooLarge"]);
            }

            if (!IsPng(buffer.AsSpan(0, read)) && !IsJpeg(buffer.AsSpan(0, read)))
            {
                throw new InvalidStateException(localizer["CompanySettings_LogoNotAnImage"]);
            }
        }
    }

    private static bool IsPng(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

    private static bool IsJpeg(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;

    /// <summary>The logo id of a stored <c>{"Value": "..."}</c> row; null for a cleared row or an unreadable one.</summary>
    private static Guid? ReadLogoId(string valueJson)
    {
        try
        {
            using var document = JsonDocument.Parse(valueJson);
            return document.RootElement.TryGetProperty("Value", out var value) && value.ValueKind == JsonValueKind.String && value.TryGetGuid(out var id)
                ? id
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
