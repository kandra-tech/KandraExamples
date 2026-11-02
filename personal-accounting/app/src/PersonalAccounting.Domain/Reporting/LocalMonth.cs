namespace PersonalAccounting.Domain.Reporting;

/// <summary>
/// A calendar month in the user's time zone: its key in the budget plan register (the first of the month, UTC kind)
/// and its bounds as UTC instants for reading movements (<see cref="ToUtc"/> is exclusive).
/// </summary>
public readonly record struct LocalMonth(int Year, int Month, DateTime FromUtc, DateTime ToUtc)
{
    public DateTime PlanPeriod => new(Year, Month, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>The month that contains <paramref name="utcInstant"/> in <paramref name="zone"/> (UTC when unknown).</summary>
    public static LocalMonth Of(DateTime utcInstant, TimeZoneInfo? zone)
    {
        zone ??= TimeZoneInfo.Utc;
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcInstant, DateTimeKind.Utc), zone);
        var start = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return new LocalMonth(
            local.Year,
            local.Month,
            TimeZoneInfo.ConvertTimeToUtc(start, zone),
            TimeZoneInfo.ConvertTimeToUtc(start.AddMonths(1), zone));
    }
}
