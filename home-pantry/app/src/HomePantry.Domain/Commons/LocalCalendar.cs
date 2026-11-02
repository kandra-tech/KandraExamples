namespace HomePantry.Domain.Commons;

/// <summary>
/// Calendar days in the user's time zone. Every <see cref="DateTime"/> the client sends is a UTC instant: a date picked in
/// Kyiv as 5 November arrives as 4 November 22:00 UTC, so <c>.Date</c> of it is the previous day. Three forms of a day are
/// used here:
/// <list type="bullet">
/// <item>the <b>local date</b> (<see cref="DateOf"/>): the calendar date the user sees, as an unzoned date;</item>
/// <item>the <b>key</b> (<see cref="Key"/>): midnight UTC of that date, the register dimension for a day (a batch's
/// best-before date, a planned meal's day), so the same day is the same key whoever posted it;</item>
/// <item><b>local midnight</b> (<see cref="MidnightUtc"/>): the UTC instant the client shows as that date, for a date
/// returned to the user (a report column, a default the form fills).</item>
/// </list>
/// </summary>
public static class LocalCalendar
{
    /// <summary>The best-before date of a product that does not spoil.</summary>
    public static readonly DateTime NoExpiry = new(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Whether the date stands for "does not spoil" (any date in 9999, so a zone shift of it still counts).</summary>
    public static bool IsNoExpiry(DateTime date) => date.Year >= 9999;

    /// <summary>The calendar date of a UTC instant in <paramref name="zone"/> (UTC when null).</summary>
    public static DateTime DateOf(DateTime utc, TimeZoneInfo? zone) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone ?? TimeZoneInfo.Utc).Date;

    /// <summary>Today's calendar date in <paramref name="zone"/>.</summary>
    public static DateTime Today(DateTime utcNow, TimeZoneInfo? zone) => DateOf(utcNow, zone);

    /// <summary>The register key of a calendar date: midnight UTC of it.</summary>
    public static DateTime Key(DateTime date) => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

    /// <summary>The UTC instant of local midnight of a calendar date: what the client shows as that date.</summary>
    public static DateTime MidnightUtc(DateTime date, TimeZoneInfo? zone) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified), zone ?? TimeZoneInfo.Utc);

    /// <summary>The key of a best-before date as a form sends it (local midnight, or the no-expiry date).</summary>
    public static DateTime BestBeforeKey(DateTime picked, TimeZoneInfo? zone) =>
        IsNoExpiry(picked) ? NoExpiry : Key(DateOf(picked, zone));

    /// <summary>A best-before key as the user sees it (local midnight; the no-expiry date stays as it is).</summary>
    public static DateTime BestBeforeShown(DateTime key, TimeZoneInfo? zone) =>
        IsNoExpiry(key) ? NoExpiry : MidnightUtc(key, zone);

    /// <summary>The best-before date of a product with <paramref name="shelfLifeDays"/> bought at <paramref name="boughtUtc"/>, as local midnight.</summary>
    public static DateTime DefaultBestBefore(DateTime boughtUtc, int shelfLifeDays, TimeZoneInfo? zone) =>
        shelfLifeDays > 0 ? MidnightUtc(DateOf(boughtUtc, zone).AddDays(shelfLifeDays), zone) : NoExpiry;

    /// <summary>The Monday of the week of a calendar date.</summary>
    public static DateTime MondayOf(DateTime date) =>
        date.Date.AddDays(-((7 + (int)date.DayOfWeek - (int)DayOfWeek.Monday) % 7));
}
