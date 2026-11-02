using HomeUtilities.Domain.Entities.Dictionaries;

namespace HomeUtilities.Application.Services;

public static class PaymentDescriptionFormatter
{
    public static string Format(
        string? template,
        Provider? provider,
        Property? property,
        DateTime month,
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
            var parts = new List<string>
            {
                $"Комунальні послуги за {monthStr}.{yearStr}"
            };
            if (!string.IsNullOrWhiteSpace(account))
                parts.Add($"о/р {account}");
            if (!string.IsNullOrWhiteSpace(owner))
                parts.Add(owner);
            if (!string.IsNullOrWhiteSpace(propAddr))
                parts.Add(propAddr);
            if (!string.IsNullOrWhiteSpace(meters))
                parts.Add($"показники: {meters}");

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
