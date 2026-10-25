namespace FamilyGarage.Domain.Reporting;

/// <summary>
/// Calendar days and months in the user's time zone. Documents store their date as a UTC instant (local midnight of
/// the day the user picked), so the day or month of a document is the local one, never the UTC one: for a user east of
/// UTC, the 1st of a month at local midnight is still the previous month in UTC.
/// </summary>
public static class LocalCalendar
{
    public static DateOnly LocalDate(DateTime utcInstant, TimeZoneInfo? zone)
    {
        var instant = DateTime.SpecifyKind(utcInstant, DateTimeKind.Utc);
        return DateOnly.FromDateTime(zone is null ? instant : TimeZoneInfo.ConvertTimeFromUtc(instant, zone));
    }

    /// <summary>The UTC instant at which <paramref name="date"/> starts in <paramref name="zone"/>.</summary>
    public static DateTime StartUtc(DateOnly date, TimeZoneInfo? zone)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        if (zone is null)
        {
            return DateTime.SpecifyKind(local, DateTimeKind.Utc);
        }

        while (zone.IsInvalidTime(local))
        {
            local = local.AddMinutes(15);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }

    /// <summary>The first instant after the local day of <paramref name="utcInstant"/>: the exclusive end of "up to that day".</summary>
    public static DateTime EndOfLocalDayUtc(DateTime utcInstant, TimeZoneInfo? zone)
        => StartUtc(LocalDate(utcInstant, zone).AddDays(1), zone);

    public static DateOnly MonthStart(DateOnly date) => new(date.Year, date.Month, 1);

    /// <summary>The local month of an instant as <c>yyyy-MM</c>, the column key of the costs pivot and chart.</summary>
    public static string MonthKey(DateTime utcInstant, TimeZoneInfo? zone) => LocalDate(utcInstant, zone).ToString("yyyy-MM");
}
