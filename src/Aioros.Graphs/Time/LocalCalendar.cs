namespace Aioros.Graphs.Time;

/// <summary>
/// The local fields of JavaScript time values in a time zone, read and composed with <c>Date</c>'s local semantics
/// (<c>getFullYear</c>, <c>getTimezoneOffset</c>, <c>new Date(year, month, ...)</c>): a local time that falls in a
/// daylight-saving gap is read with the offset before the transition, and a repeated local time resolves to its
/// first occurrence, as ECMAScript's <c>UTC(t)</c> does.
/// </summary>
public static class LocalCalendar
{
    /// <summary>Returns the minutes to add to local time to get UTC, as <c>getTimezoneOffset</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <param name="zone">The time zone.</param>
    /// <returns>The offset in minutes, or NaN for an invalid time.</returns>
    public static double TimezoneOffset(double t, TimeZoneInfo zone)
    {
        t = UtcCalendar.Clip(t);
        return double.IsNaN(t) ? double.NaN : (t - LocalTime(t, zone)) / 60_000;
    }

    /// <summary>Returns the local full year, as <c>getFullYear</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <param name="zone">The time zone.</param>
    /// <returns>The year, or NaN.</returns>
    public static double Year(double t, TimeZoneInfo zone) => TryFields(t, zone, out var f) ? f.Year : double.NaN;

    /// <summary>Returns the local month from 0 to 11, as <c>getMonth</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <param name="zone">The time zone.</param>
    /// <returns>The month, or NaN.</returns>
    public static double Month(double t, TimeZoneInfo zone) => TryFields(t, zone, out var f) ? f.Month : double.NaN;

    /// <summary>Returns the local day of the month from 1, as <c>getDate</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <param name="zone">The time zone.</param>
    /// <returns>The day, or NaN.</returns>
    public static double Day(double t, TimeZoneInfo zone) => TryFields(t, zone, out var f) ? f.Day : double.NaN;

    /// <summary>Returns the local day of the week, 0 for Sunday, as <c>getDay</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <param name="zone">The time zone.</param>
    /// <returns>The weekday, or NaN.</returns>
    public static double Weekday(double t, TimeZoneInfo zone) => TryFields(t, zone, out var f) ? f.Weekday : double.NaN;

    /// <summary>Returns the local hours, as <c>getHours</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <param name="zone">The time zone.</param>
    /// <returns>The hours, or NaN.</returns>
    public static double Hours(double t, TimeZoneInfo zone) => TryFields(t, zone, out var f) ? f.Hours : double.NaN;

    /// <summary>Returns the local minutes, as <c>getMinutes</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <param name="zone">The time zone.</param>
    /// <returns>The minutes, or NaN.</returns>
    public static double Minutes(double t, TimeZoneInfo zone) => TryFields(t, zone, out var f) ? f.Minutes : double.NaN;

    /// <summary>Returns the local seconds, as <c>getSeconds</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <param name="zone">The time zone.</param>
    /// <returns>The seconds, or NaN.</returns>
    public static double Seconds(double t, TimeZoneInfo zone) => TryFields(t, zone, out var f) ? f.Seconds : double.NaN;

    /// <summary>Returns the local milliseconds, as <c>getMilliseconds</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <param name="zone">The time zone.</param>
    /// <returns>The milliseconds, or NaN.</returns>
    public static double Milliseconds(double t, TimeZoneInfo zone) => TryFields(t, zone, out var f) ? f.Milliseconds : double.NaN;

    /// <summary>
    /// Composes a time value from local fields that may overflow, as <c>new Date(year, month, day, hours, minutes,
    /// seconds, milliseconds)</c>: a whole year from 0 to 99 is taken as 1900 plus it.
    /// </summary>
    /// <param name="zone">The time zone.</param>
    /// <param name="year">The full year.</param>
    /// <param name="month">The month from 0, any integer.</param>
    /// <param name="day">The day of the month from 1, any integer.</param>
    /// <param name="hours">The hours.</param>
    /// <param name="minutes">The minutes.</param>
    /// <param name="seconds">The seconds.</param>
    /// <param name="milliseconds">The milliseconds.</param>
    /// <returns>The clipped time value, or NaN.</returns>
    public static double Compose(TimeZoneInfo zone, double year, double month, double day = 1, double hours = 0, double minutes = 0, double seconds = 0, double milliseconds = 0)
    {
        ArgumentNullException.ThrowIfNull(zone);
        var y = Math.Truncate(year);
        if (!double.IsNaN(y) && y is >= 0 and <= 99)
        {
            year = 1900 + y;
        }

        var wall = MakeDate(UtcCalendar.MakeDay(year, month, day), MakeTime(hours, minutes, seconds, milliseconds));
        return UtcCalendar.Clip(Utc(wall, zone));
    }

    internal static double OffsetAt(double t, TimeZoneInfo zone) => ZoneOffsets.For(zone).At(t);

    internal static double LocalTime(double t, TimeZoneInfo zone) => t + OffsetAt(t, zone);

    internal static double Utc(double wall, TimeZoneInfo zone)
    {
        if (!double.IsFinite(wall) || Math.Abs(wall) > UtcCalendar.MaxTime + 2 * UtcCalendar.MillisecondsPerDay)
        {
            return double.NaN;
        }

        var before = OffsetAt(wall - UtcCalendar.MillisecondsPerDay, zone);
        var after = OffsetAt(wall + UtcCalendar.MillisecondsPerDay, zone);
        if (before == after && OffsetAt(wall - before, zone) == before)
        {
            return wall - before;
        }

        var first = wall - before;
        var second = wall - after;
        var firstValid = OffsetAt(first, zone) == before;
        var secondValid = OffsetAt(second, zone) == after;
        if (firstValid && secondValid)
        {
            return Math.Min(first, second);
        }

        return secondValid && !firstValid ? second : first;
    }

    internal static CalendarFields WallFields(double wall) => UtcCalendar.FieldsOf((long)wall);

    internal static bool TryFields(double t, TimeZoneInfo zone, out CalendarFields fields)
    {
        ArgumentNullException.ThrowIfNull(zone);
        t = UtcCalendar.Clip(t);
        if (double.IsNaN(t))
        {
            fields = default;
            return false;
        }

        fields = WallFields(LocalTime(t, zone));
        return true;
    }

    internal static double SetHours(double t, TimeZoneInfo zone, double hours, double minutes, double seconds, double milliseconds)
    {
        if (double.IsNaN(t))
        {
            return double.NaN;
        }

        var wall = LocalTime(t, zone);
        var day = Math.Floor(wall / UtcCalendar.MillisecondsPerDay);
        return UtcCalendar.Clip(Utc(MakeDate(day, MakeTime(hours, minutes, seconds, milliseconds)), zone));
    }

    internal static double SetDate(double t, TimeZoneInfo zone, double date)
    {
        if (double.IsNaN(t))
        {
            return double.NaN;
        }

        var wall = LocalTime(t, zone);
        var f = WallFields(wall);
        return UtcCalendar.Clip(Utc(MakeDate(UtcCalendar.MakeDay(f.Year, f.Month, date), f.Time), zone));
    }

    internal static double SetMonth(double t, TimeZoneInfo zone, double month, double? date = null)
    {
        if (double.IsNaN(t))
        {
            return double.NaN;
        }

        var wall = LocalTime(t, zone);
        var f = WallFields(wall);
        return UtcCalendar.Clip(Utc(MakeDate(UtcCalendar.MakeDay(f.Year, month, date ?? f.Day), f.Time), zone));
    }

    internal static double SetFullYear(double t, TimeZoneInfo zone, double year)
    {
        var wall = double.IsNaN(t) ? 0 : LocalTime(t, zone);
        var f = WallFields(wall);
        return UtcCalendar.Clip(Utc(MakeDate(UtcCalendar.MakeDay(year, f.Month, f.Day), f.Time), zone));
    }

    private static double MakeTime(double hours, double minutes, double seconds, double milliseconds) =>
        Math.Truncate(hours) * 3_600_000 + Math.Truncate(minutes) * 60_000 + Math.Truncate(seconds) * 1000 + Math.Truncate(milliseconds);

    private static double MakeDate(double day, double time)
    {
        var date = day * UtcCalendar.MillisecondsPerDay + time;
        return double.IsFinite(date) ? date : double.NaN;
    }
}
