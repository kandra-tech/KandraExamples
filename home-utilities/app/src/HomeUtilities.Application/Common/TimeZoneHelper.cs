namespace HomeUtilities.Application.Common;

internal static class TimeZoneHelper
{
    public static DateTime ToLocalDate(DateTime value, TimeZoneInfo? timeZone)
    {
        var tz = timeZone ?? TimeZoneInfo.Utc;
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, tz).Date;
    }

    public static DateTime ToMonthStartUtc(DateTime value, TimeZoneInfo? timeZone)
    {
        var local = ToLocalDate(value, timeZone);
        return new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public static DateTime ToMonthEndUtc(DateTime value, TimeZoneInfo? timeZone)
    {
        var local = ToLocalDate(value, timeZone);
        return new DateTime(local.Year, local.Month, DateTime.DaysInMonth(local.Year, local.Month), 0, 0, 0, DateTimeKind.Utc)
            .AddDays(1)
            .AddTicks(-1);
    }

    public static DateTime StartOfLocalDayUtc(DateTime value, TimeZoneInfo? timeZone)
    {
        var tz = timeZone ?? TimeZoneInfo.Utc;
        var localDate = ToLocalDate(value, tz);
        var unspecifiedStart = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecifiedStart, tz);
    }

    public static DateTime EndOfLocalDayUtc(DateTime value, TimeZoneInfo? timeZone)
    {
        var tz = timeZone ?? TimeZoneInfo.Utc;
        var localNextDay = ToLocalDate(value, tz).AddDays(1);
        var unspecifiedNextStart = DateTime.SpecifyKind(localNextDay, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecifiedNextStart, tz).AddTicks(-1);
    }

    public static (DateTime MonthStartUtc, DateTime MonthEndUtc) LocalMonthBoundsUtc(DateTime value, TimeZoneInfo? timeZone)
    {
        var tz = timeZone ?? TimeZoneInfo.Utc;
        var local = ToLocalDate(value, tz);
        var firstDayUnspecified = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var nextMonthUnspecified = firstDayUnspecified.AddMonths(1);
        return (
            TimeZoneInfo.ConvertTimeToUtc(firstDayUnspecified, tz),
            TimeZoneInfo.ConvertTimeToUtc(nextMonthUnspecified, tz).AddTicks(-1));
    }
}
