namespace Aioros.Graphs.Time;

/// <summary>
/// The UTC fields of JavaScript time values (milliseconds since the Unix epoch, as <c>Date</c> holds them), read and
/// composed with <c>Date</c>'s semantics: fields overflow into the next unit and results are clipped to whole
/// milliseconds within ±8.64e15, NaN outside.
/// </summary>
public static class UtcCalendar
{
    /// <summary>Milliseconds in a day.</summary>
    public const double MillisecondsPerDay = 86_400_000;

    internal const long DayLength = 86_400_000;
    internal const double MaxTime = 8.64e15;

    /// <summary>Clips a time value as JavaScript's <c>TimeClip</c>: truncated to a whole millisecond, NaN beyond ±8.64e15.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The clipped value.</returns>
    public static double Clip(double t) => !double.IsFinite(t) || Math.Abs(t) > MaxTime ? double.NaN : Math.Truncate(t) + 0.0;

    /// <summary>Returns the full year, as <c>getUTCFullYear</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The year, or NaN.</returns>
    public static double Year(double t) => TryFields(t, out var f) ? f.Year : double.NaN;

    /// <summary>Returns the month from 0 to 11, as <c>getUTCMonth</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The month, or NaN.</returns>
    public static double Month(double t) => TryFields(t, out var f) ? f.Month : double.NaN;

    /// <summary>Returns the day of the month from 1, as <c>getUTCDate</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The day, or NaN.</returns>
    public static double Day(double t) => TryFields(t, out var f) ? f.Day : double.NaN;

    /// <summary>Returns the day of the week, 0 for Sunday, as <c>getUTCDay</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The weekday, or NaN.</returns>
    public static double Weekday(double t) => TryTime(t, out var ms) ? WeekdayOf(DayOf(ms)) : double.NaN;

    /// <summary>Returns the hours, as <c>getUTCHours</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The hours, or NaN.</returns>
    public static double Hours(double t) => TryTime(t, out var ms) ? TimeOf(ms) / 3_600_000 : double.NaN;

    /// <summary>Returns the minutes, as <c>getUTCMinutes</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The minutes, or NaN.</returns>
    public static double Minutes(double t) => TryTime(t, out var ms) ? TimeOf(ms) / 60_000 % 60 : double.NaN;

    /// <summary>Returns the seconds, as <c>getUTCSeconds</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The seconds, or NaN.</returns>
    public static double Seconds(double t) => TryTime(t, out var ms) ? TimeOf(ms) / 1000 % 60 : double.NaN;

    /// <summary>Returns the milliseconds, as <c>getUTCMilliseconds</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The milliseconds, or NaN.</returns>
    public static double Milliseconds(double t) => TryTime(t, out var ms) ? TimeOf(ms) % 1000 : double.NaN;

    /// <summary>
    /// Composes a time value from fields that may overflow, as <c>setUTCFullYear(year, month, day)</c> followed by
    /// <c>setUTCHours(hours, minutes, seconds, milliseconds)</c>: years below 100 are not shifted to the 1900s.
    /// </summary>
    /// <param name="year">The full year.</param>
    /// <param name="month">The month from 0, any integer.</param>
    /// <param name="day">The day of the month from 1, any integer.</param>
    /// <param name="hours">The hours.</param>
    /// <param name="minutes">The minutes.</param>
    /// <param name="seconds">The seconds.</param>
    /// <param name="milliseconds">The milliseconds.</param>
    /// <returns>The clipped time value, or NaN.</returns>
    public static double Compose(double year, double month, double day, double hours = 0, double minutes = 0, double seconds = 0, double milliseconds = 0)
    {
        var date = MakeDay(year, month, day);
        var time = Math.Truncate(hours) * 3_600_000 + Math.Truncate(minutes) * 60_000 + Math.Truncate(seconds) * 1000 + Math.Truncate(milliseconds);
        return Clip(date * MillisecondsPerDay + time);
    }

    internal static bool TryTime(double t, out long ms)
    {
        if (!double.IsFinite(t) || Math.Abs(t) > MaxTime)
        {
            ms = 0;
            return false;
        }

        ms = (long)t;
        return true;
    }

    internal static bool TryFields(double t, out CalendarFields fields)
    {
        if (!TryTime(t, out var ms))
        {
            fields = default;
            return false;
        }

        fields = FieldsOf(ms);
        return true;
    }

    internal static CalendarFields FieldsOf(long ms)
    {
        var day = DayOf(ms);
        var (year, month, date) = Civil(day);
        return new CalendarFields(year, month, date, TimeOf(ms), day);
    }

    internal static long DayOf(long ms) => ms >= 0 ? ms / DayLength : (ms + 1) / DayLength - 1;

    internal static long TimeOf(long ms) => ms - DayOf(ms) * DayLength;

    internal static int WeekdayOf(long day)
    {
        var w = (int)((day + 4) % 7);
        return w < 0 ? w + 7 : w;
    }

    internal static double DayNumber(double t) => Math.Floor(t / MillisecondsPerDay);

    internal static double FromDays(long day, long time = 0)
    {
        var t = (double)(day * DayLength + time);
        return Math.Abs(t) > MaxTime ? double.NaN : t;
    }

    internal static double ComposeFields(long year, long month, long day, long time)
    {
        var y = year + FloorDiv(month, 12);
        if (Math.Abs(y) > 400_000)
        {
            return double.NaN;
        }

        return FromDays(DaysFromCivil(y, (int)(month - FloorDiv(month, 12) * 12) + 1) + day - 1, time);
    }

    internal static double MakeDay(double year, double month, double day)
    {
        if (!double.IsFinite(year) || !double.IsFinite(month) || !double.IsFinite(day))
        {
            return double.NaN;
        }

        var m = Math.Truncate(month);
        var y = Math.Truncate(year) + Math.Floor(m / 12);
        if (Math.Abs(y) > 400_000)
        {
            return double.NaN;
        }

        var r = m % 12;
        return DaysFromCivil((long)y, (int)(r < 0 ? r + 12 : r) + 1) + Math.Truncate(day) - 1;
    }

    internal static (int Year, int Month, int Day) Civil(long dayNumber)
    {
        var z = dayNumber + 719_468;
        var era = (z >= 0 ? z : z - 146_096) / 146_097;
        var dayOfEra = z - era * 146_097;
        var yearOfEra = (dayOfEra - dayOfEra / 1460 + dayOfEra / 36_524 - dayOfEra / 146_096) / 365;
        var dayOfYear = dayOfEra - (365 * yearOfEra + yearOfEra / 4 - yearOfEra / 100);
        var mp = (5 * dayOfYear + 2) / 153;
        var day = dayOfYear - (153 * mp + 2) / 5 + 1;
        var month = mp < 10 ? mp + 3 : mp - 9;
        return ((int)(yearOfEra + era * 400 + (month <= 2 ? 1 : 0)), (int)month - 1, (int)day);
    }

    internal static long DaysFromCivil(long year, int month)
    {
        year -= month <= 2 ? 1 : 0;
        var era = (year >= 0 ? year : year - 399) / 400;
        var yearOfEra = year - era * 400;
        var dayOfYear = (153 * (month > 2 ? month - 3 : month + 9) + 2) / 5;
        var dayOfEra = yearOfEra * 365 + yearOfEra / 4 - yearOfEra / 100 + dayOfYear;
        return era * 146_097 + dayOfEra - 719_468;
    }

    internal static long FloorDiv(long value, long divisor)
    {
        var q = value / divisor;
        return (value % divisor != 0 && (value < 0) != (divisor < 0)) ? q - 1 : q;
    }
}
