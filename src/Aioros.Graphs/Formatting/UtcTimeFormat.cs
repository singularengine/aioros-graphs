using Aioros.Graphs.Time;

namespace Aioros.Graphs.Formatting;

/// <summary>
/// A UTC date format of d3-time-format (<c>utcFormat</c>), compiled once from its specifier in a locale (d3's default
/// en-US unless given): <c>%a %A %b %B %c %d %e %f %g %G %H %I %j %L %m %M %p %q %Q %s %S %u %U %V %w %W %x %X %y %Y
/// %Z %%</c>, each with the optional <c>-</c> (no padding), <c>_</c> (spaces) or <c>0</c> (zeros) modifier. Other
/// directives are written without their <c>%</c>, as d3 writes the ones it does not know.
/// </summary>
public sealed class UtcTimeFormat
{
    private readonly TimeFormatProgram program;

    private UtcTimeFormat(TimeFormatProgram program)
    {
        this.program = program;
    }

    /// <summary>Gets the longest text this format can write, to size a buffer for <see cref="TryFormat"/>.</summary>
    public int MaxLength => program.MaxLength;

    /// <summary>Compiles <paramref name="specifier"/> in d3's default en-US locale.</summary>
    /// <param name="specifier">The format, such as <c>%Y-%m-%d</c>.</param>
    /// <returns>The format.</returns>
    public static UtcTimeFormat Create(string specifier) => new(TimeLocale.EnUs.Program(specifier));

    /// <summary>Compiles <paramref name="specifier"/> in <paramref name="locale"/>, as d3's <c>timeFormatLocale(locale).utcFormat</c>.</summary>
    /// <param name="specifier">The format, such as <c>%c</c>.</param>
    /// <param name="locale">The locale.</param>
    /// <returns>The format.</returns>
    public static UtcTimeFormat Create(string specifier, TimeLocale locale)
    {
        ArgumentNullException.ThrowIfNull(locale);
        return new(locale.Program(specifier));
    }

    /// <summary>
    /// Formats a tick of a UTC time scale as d3's <c>scaleUtc().tickFormat()</c> does: milliseconds, seconds,
    /// minutes, hours, weekday and day, month and day, month or year, whichever is the finest unit the time is not on a
    /// boundary of.
    /// </summary>
    /// <param name="milliseconds">The time value.</param>
    /// <returns>The label.</returns>
    public static string MultiScale(double milliseconds) => MultiScale(milliseconds, TimeLocale.EnUs);

    /// <summary>Formats a tick of a UTC time scale with the names and formats of <paramref name="locale"/>.</summary>
    /// <param name="milliseconds">The time value.</param>
    /// <param name="locale">The locale.</param>
    /// <returns>The label.</returns>
    public static string MultiScale(double milliseconds, TimeLocale locale)
    {
        ArgumentNullException.ThrowIfNull(locale);
        var t = UtcCalendar.Clip(milliseconds);
        return locale.Labels[Label(t)].Format(t, double.IsNaN(t) ? 0 : (long)t, 0);
    }

    /// <summary>Writes the label of <see cref="MultiScale(double)"/> without allocating.</summary>
    /// <param name="milliseconds">The time value.</param>
    /// <param name="destination">Where the label goes.</param>
    /// <param name="written">The number of characters written.</param>
    /// <returns>Whether the label fit.</returns>
    public static bool TryMultiScale(double milliseconds, Span<char> destination, out int written)
    {
        var t = UtcCalendar.Clip(milliseconds);
        return TimeLocale.EnUs.Labels[Label(t)].TryFormat(t, double.IsNaN(t) ? 0 : (long)t, 0, destination, out written);
    }

    /// <summary>Formats the time value <paramref name="milliseconds"/>.</summary>
    /// <param name="milliseconds">Milliseconds since the Unix epoch, clipped as <c>new Date(milliseconds)</c>.</param>
    /// <returns>The text.</returns>
    public string Format(double milliseconds)
    {
        var t = UtcCalendar.Clip(milliseconds);
        return program.Format(t, double.IsNaN(t) ? 0 : (long)t, 0);
    }

    /// <summary>Writes the text of <see cref="Format"/> without allocating.</summary>
    /// <param name="milliseconds">Milliseconds since the Unix epoch.</param>
    /// <param name="destination">Where the text goes; <see cref="MaxLength"/> characters are always enough.</param>
    /// <param name="written">The number of characters written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormat(double milliseconds, Span<char> destination, out int written)
    {
        var t = UtcCalendar.Clip(milliseconds);
        return program.TryFormat(t, double.IsNaN(t) ? 0 : (long)t, 0, destination, out written);
    }

    private static int Label(double t)
    {
        if (!UtcCalendar.TryTime(t, out var ms))
        {
            return 7;
        }

        var time = UtcCalendar.TimeOf(ms);
        if (time % 1000 != 0)
        {
            return 0;
        }

        if (time % 60_000 != 0)
        {
            return 1;
        }

        if (time % 3_600_000 != 0)
        {
            return 2;
        }

        if (time != 0)
        {
            return 3;
        }

        var f = UtcCalendar.FieldsOf(ms);
        if (f.Day != 1 && !double.IsNaN(UtcCalendar.FromDays(UtcCalendar.DaysFromCivil(f.Year, f.Month + 1))))
        {
            return f.Weekday != 0 ? 4 : 5;
        }

        var startOfYear = f.Month == 0 && f.Day == 1;
        return !startOfYear && !double.IsNaN(UtcCalendar.FromDays(UtcCalendar.DaysFromCivil(f.Year, 1))) ? 6 : 7;
    }
}
