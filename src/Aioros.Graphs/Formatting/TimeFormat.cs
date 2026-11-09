using Aioros.Graphs.Time;

namespace Aioros.Graphs.Formatting;

/// <summary>
/// A local date format of d3-time-format (<c>timeFormat</c>), compiled once from its specifier and written in a time
/// zone (<see cref="TimeZoneInfo.Local"/> unless another is given): the directives of <see cref="UtcTimeFormat"/> read
/// the local fields, and <c>%Z</c> writes the zone's offset.
/// </summary>
public sealed class TimeFormat
{
    private readonly TimeFormatProgram program;
    private readonly TimeZoneInfo? zone;

    private TimeFormat(TimeFormatProgram program, TimeZoneInfo? zone)
    {
        this.program = program;
        this.zone = zone;
    }

    /// <summary>Gets the time zone the format writes in.</summary>
    public TimeZoneInfo Zone => zone ?? TimeZoneInfo.Local;

    /// <summary>Gets the longest text this format can write, to size a buffer for <see cref="TryFormat"/>.</summary>
    public int MaxLength => program.MaxLength;

    /// <summary>Compiles <paramref name="specifier"/>, as d3's <c>timeFormatLocale(locale).format</c>.</summary>
    /// <param name="specifier">The format, such as <c>%Y-%m-%d %H:%M</c>.</param>
    /// <param name="zone">The time zone; <see cref="TimeZoneInfo.Local"/> when null.</param>
    /// <param name="locale">The locale; d3's default en-US when null.</param>
    /// <returns>The format.</returns>
    public static TimeFormat Create(string specifier, TimeZoneInfo? zone = null, TimeLocale? locale = null) => new((locale ?? TimeLocale.EnUs).Program(specifier), zone);

    /// <summary>
    /// Formats a tick of a local time scale as d3's <c>scaleTime().tickFormat()</c> does: milliseconds, seconds,
    /// minutes, hours, weekday and day, month and day, month or year, whichever is the finest local unit the time is
    /// not on a boundary of.
    /// </summary>
    /// <param name="milliseconds">The time value.</param>
    /// <param name="zone">The time zone; <see cref="TimeZoneInfo.Local"/> when null.</param>
    /// <param name="locale">The locale; d3's default en-US when null.</param>
    /// <returns>The label.</returns>
    public static string MultiScale(double milliseconds, TimeZoneInfo? zone = null, TimeLocale? locale = null)
    {
        var t = UtcCalendar.Clip(milliseconds);
        var timeZone = zone ?? TimeZoneInfo.Local;
        return Write((locale ?? TimeLocale.EnUs).Labels[Label(t, timeZone)], t, timeZone);
    }

    /// <summary>Writes the label of <see cref="MultiScale"/> in d3's default en-US locale without allocating.</summary>
    /// <param name="milliseconds">The time value.</param>
    /// <param name="destination">Where the label goes.</param>
    /// <param name="written">The number of characters written.</param>
    /// <param name="zone">The time zone; <see cref="TimeZoneInfo.Local"/> when null.</param>
    /// <returns>Whether the label fit.</returns>
    public static bool TryMultiScale(double milliseconds, Span<char> destination, out int written, TimeZoneInfo? zone = null)
    {
        var t = UtcCalendar.Clip(milliseconds);
        var timeZone = zone ?? TimeZoneInfo.Local;
        return TryWrite(TimeLocale.EnUs.Labels[Label(t, timeZone)], t, timeZone, destination, out written);
    }

    /// <summary>Formats the time value <paramref name="milliseconds"/>.</summary>
    /// <param name="milliseconds">Milliseconds since the Unix epoch, clipped as <c>new Date(milliseconds)</c>.</param>
    /// <returns>The text.</returns>
    public string Format(double milliseconds) => Write(program, UtcCalendar.Clip(milliseconds), Zone);

    /// <summary>Writes the text of <see cref="Format(double)"/> without allocating.</summary>
    /// <param name="milliseconds">Milliseconds since the Unix epoch.</param>
    /// <param name="destination">Where the text goes; <see cref="MaxLength"/> characters are always enough.</param>
    /// <param name="written">The number of characters written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormat(double milliseconds, Span<char> destination, out int written) =>
        TryWrite(program, UtcCalendar.Clip(milliseconds), Zone, destination, out written);

    private static int Label(double t, TimeZoneInfo zone)
    {
        if (double.IsNaN(t))
        {
            return 7;
        }

        var wall = LocalCalendar.LocalTime(t, zone);
        if (Before(t - Modulo(wall, 1000), t))
        {
            return 0;
        }

        if (Before(t - Modulo(wall, 60_000), t))
        {
            return 1;
        }

        if (Before(t - Modulo(wall, 3_600_000), t))
        {
            return 2;
        }

        if (Before(TimeInterval.Day.WithZone(zone).Floor(t), t))
        {
            return 3;
        }

        if (Before(TimeInterval.Month.WithZone(zone).Floor(t), t))
        {
            return Before(TimeInterval.Week.WithZone(zone).Floor(t), t) ? 4 : 5;
        }

        return Before(TimeInterval.Year.WithZone(zone).Floor(t), t) ? 6 : 7;
    }

    private static bool Before(double floor, double t) => UtcCalendar.Clip(floor) < t;

    private static double Modulo(double value, double divisor)
    {
        var r = value % divisor;
        return r < 0 ? r + divisor : r;
    }

    private static string Write(TimeFormatProgram program, double t, TimeZoneInfo zone)
    {
        if (double.IsNaN(t))
        {
            return program.Format(t, 0, double.NaN);
        }

        var wall = LocalCalendar.LocalTime(t, zone);
        return program.Format(t, (long)wall, (t - wall) / 60_000);
    }

    private static bool TryWrite(TimeFormatProgram program, double t, TimeZoneInfo zone, Span<char> destination, out int written)
    {
        if (double.IsNaN(t))
        {
            return program.TryFormat(t, 0, double.NaN, destination, out written);
        }

        var wall = LocalCalendar.LocalTime(t, zone);
        return program.TryFormat(t, (long)wall, (t - wall) / 60_000, destination, out written);
    }
}
