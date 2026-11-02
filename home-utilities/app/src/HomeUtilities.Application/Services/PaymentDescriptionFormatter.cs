using HomeUtilities.Domain.Entities.Dictionaries;
using Microsoft.Extensions.Localization;

namespace HomeUtilities.Application.Services;

public static class PaymentDescriptionFormatter
{
    public static string Format(
        string? template,
        Provider? provider,
        Property? property,
        DateTime month,
        IStringLocalizer localizer,
        string? invoiceNumber = null,
        string? meterReadingsSummary = null)
    {
        var monthStr = month.Month.ToString("D2");
        var yearStr = month.Year.ToString();
        var monthName = month.ToString("MMMM");
        var owner = property?.Owner ?? string.Empty;
        var account = provider?.PersonalAccountNumber ?? string.Empty;
        var propAddr = property?.Address ?? string.Empty;
        var propName = property?.Name ?? string.Empty;
        var provName = provider?.Name ?? string.Empty;
        var meters = meterReadingsSummary ?? string.Empty;
        var invNo = invoiceNumber ?? string.Empty;

        if (string.IsNullOrWhiteSpace(template))
        {
            // No template on the provider: the purpose is built from the resources, in the user's language.
            var parts = new List<string>
            {
                localizer["PaymentPurpose_Services", monthStr, yearStr].Value
            };
            if (!string.IsNullOrWhiteSpace(account))
                parts.Add(localizer["PaymentPurpose_Account", account].Value);
            if (!string.IsNullOrWhiteSpace(owner))
                parts.Add(owner);
            if (!string.IsNullOrWhiteSpace(propAddr))
                parts.Add(propAddr);
            if (!string.IsNullOrWhiteSpace(meters))
                parts.Add(localizer["PaymentPurpose_Readings", meters].Value);

            return string.Join("; ", parts);
        }

        var result = template
            .Replace("{Month}", monthStr, StringComparison.OrdinalIgnoreCase)
            .Replace("{Year}", yearStr, StringComparison.OrdinalIgnoreCase)
            .Replace("{MonthName}", monthName, StringComparison.OrdinalIgnoreCase)
            .Replace("{Owner}", owner, StringComparison.OrdinalIgnoreCase)
            .Replace("{PersonalAccountNumber}", account, StringComparison.OrdinalIgnoreCase)
            .Replace("{Account}", account, StringComparison.OrdinalIgnoreCase)
            .Replace("{PropertyAddress}", propAddr, StringComparison.OrdinalIgnoreCase)
            .Replace("{PropertyName}", propName, StringComparison.OrdinalIgnoreCase)
            .Replace("{ProviderName}", provName, StringComparison.OrdinalIgnoreCase)
            .Replace("{MeterReadings}", meters, StringComparison.OrdinalIgnoreCase)
            .Replace("{InvoiceNumber}", invNo, StringComparison.OrdinalIgnoreCase);

        return result.Trim();
    }
}
