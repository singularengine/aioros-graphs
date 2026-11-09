using Aioros.Graphs.Formatting;
using Aioros.Graphs.Time;

namespace Aioros.Graphs.Scales;

/// <summary>
/// A linear scale over UTC time, as d3-scale's <c>scaleUtc</c>: the domain holds JavaScript time values (milliseconds
/// since the Unix epoch, whole and within ±8.64e15), ticks fall on calendar boundaries and labels use d3's
/// multi-scale format. Immutable: the <c>With</c> methods and <see cref="Nice(double)"/> return new scales.
/// </summary>
public sealed class UtcScale
{
    private readonly LinearScale linear;

    private UtcScale(LinearScale linear)
    {
        this.linear = linear;
    }

    /// <summary>The domain's stops, as time values.</summary>
    public ReadOnlySpan<double> Domain => linear.Domain;

    /// <summary>The range's stops.</summary>
    public ReadOnlySpan<double> Range => linear.Range;

    /// <summary>Whether instants outside the domain are clamped to it.</summary>
    public bool IsClamped => linear.IsClamped;

    /// <summary>Creates a scale from a time domain to <paramref name="range"/>.</summary>
    /// <param name="domain">At least two time values; fractions of a millisecond are truncated.</param>
    /// <param name="range">At least two range stops.</param>
    /// <returns>The scale.</returns>
    public static UtcScale Create(ReadOnlySpan<double> domain, ReadOnlySpan<double> range) => new(LinearScale.Create(Clip(domain), range));

    /// <summary>Creates a scale from the instants <paramref name="start"/> to <paramref name="end"/>.</summary>
    /// <param name="start">The domain's start.</param>
    /// <param name="end">The domain's end.</param>
    /// <param name="r0">The range's start.</param>
    /// <param name="r1">The range's end.</param>
    /// <returns>The scale.</returns>
    public static UtcScale Create(DateTimeOffset start, DateTimeOffset end, double r0, double r1) =>
        Create([start.ToUnixTimeMilliseconds(), end.ToUnixTimeMilliseconds()], [r0, r1]);

    /// <summary>Returns this scale with another domain.</summary>
    /// <param name="stops">At least two time values.</param>
    /// <returns>The new scale.</returns>
    public UtcScale WithDomain(ReadOnlySpan<double> stops) => new(linear.WithDomain(Clip(stops)));

    /// <summary>Returns this scale with another range, not rounded.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public UtcScale WithRange(ReadOnlySpan<double> stops) => new(linear.WithRange(stops));

    /// <summary>Returns this scale with another range whose mapped values are rounded.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public UtcScale WithRangeRound(ReadOnlySpan<double> stops) => new(linear.WithRangeRound(stops));

    /// <summary>Returns this scale with clamping on or off.</summary>
    /// <param name="clamp">Whether instants outside the domain are clamped.</param>
    /// <returns>The new scale.</returns>
    public UtcScale WithClamp(bool clamp) => new(linear.WithClamp(clamp));

    /// <summary>Maps an instant to the range.</summary>
    /// <param name="time">The time value; fractions of a millisecond are truncated.</param>
    /// <returns>The range value, or NaN for an invalid time.</returns>
    public double Map(double time) => linear.Map(UtcCalendar.Clip(time));

    /// <summary>Maps instants to the range without allocating.</summary>
    /// <param name="times">The time values.</param>
    /// <param name="destination">At least as many values as <paramref name="times"/>.</param>
    public void Map(ReadOnlySpan<double> times, Span<double> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, times.Length);
        for (var i = 0; i < times.Length; i++)
        {
            destination[i] = Map(times[i]);
        }
    }

    /// <summary>Maps a range value back to an instant.</summary>
    /// <param name="y">The range value.</param>
    /// <returns>The time value, truncated to the millisecond, or NaN.</returns>
    public double Invert(double y) => UtcCalendar.Clip(linear.Invert(y));

    /// <summary>Returns about <paramref name="count"/> instants on calendar boundaries within the domain.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <returns>The ticks as time values.</returns>
    public double[] Ticks(double count = 10) => UtcTicks.Ticks(Domain[0], Domain[^1], count);

    /// <summary>Writes about <paramref name="count"/> instants on calendar boundaries within the domain without allocating.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="destination">Where the ticks go; <see cref="MaxTickCount"/> values are always enough.</param>
    /// <returns>The number of ticks written.</returns>
    public int Ticks(double count, Span<double> destination) => UtcTicks.Ticks(Domain[0], Domain[^1], count, destination);

    /// <summary>Returns the instants of <paramref name="interval"/> within the domain.</summary>
    /// <param name="interval">The interval, such as <see cref="UtcInterval.Month"/>.</param>
    /// <returns>The ticks as time values.</returns>
    public double[] Ticks(UtcInterval interval) => UtcTicks.Ticks(Domain[0], Domain[^1], interval);

    /// <summary>Returns an upper bound of the number of ticks <see cref="Ticks(double)"/> returns, to size a buffer.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <returns>The bound.</returns>
    public int MaxTickCount(double count = 10) => UtcTicks.MaxCount(Domain[0], Domain[^1], count);

    /// <summary>Returns this scale with its first and last domain stops extended to the boundaries of the tick interval.</summary>
    /// <param name="count">The approximate number of ticks the interval is chosen for.</param>
    /// <returns>The new scale.</returns>
    public UtcScale Nice(double count = 10)
    {
        var stops = Domain.ToArray();
        (stops[0], stops[^1]) = UtcTicks.Nice(stops[0], stops[^1], count);
        return new UtcScale(linear.WithDomain(stops));
    }

    /// <summary>Returns this scale with its first and last domain stops extended to boundaries of <paramref name="interval"/>.</summary>
    /// <param name="interval">The interval.</param>
    /// <returns>The new scale.</returns>
    public UtcScale Nice(UtcInterval interval)
    {
        var stops = Domain.ToArray();
        var (first, last) = (stops[0], stops[^1]);
        (stops[0], stops[^1]) = last < first ? (interval.Ceil(first), interval.Floor(last)) : (interval.Floor(first), interval.Ceil(last));
        return new UtcScale(linear.WithDomain(stops));
    }

    /// <summary>
    /// Returns the labels of a UTC scale's ticks, as d3's <c>scale.tickFormat(count, specifier)</c>, which ignores the
    /// count for time scales.
    /// </summary>
    /// <param name="specifier">A d3-time-format specifier, or null for the multi-scale format.</param>
    /// <returns>The label of a time value.</returns>
    public static Func<double, string> TickFormat(string? specifier = null) =>
        specifier is null ? UtcTimeFormat.MultiScale : UtcTimeFormat.Create(specifier).Format;

    /// <summary>Returns the labels of a UTC scale's ticks in <paramref name="locale"/>, as d3's <c>scale.tickFormat</c> with that locale as the default.</summary>
    /// <param name="specifier">A d3-time-format specifier, or null for the multi-scale format.</param>
    /// <param name="locale">The locale.</param>
    /// <returns>The label of a time value.</returns>
    public static Func<double, string> TickFormat(string? specifier, TimeLocale locale)
    {
        ArgumentNullException.ThrowIfNull(locale);
        return specifier is null ? t => UtcTimeFormat.MultiScale(t, locale) : UtcTimeFormat.Create(specifier, locale).Format;
    }

    private static double[] Clip(ReadOnlySpan<double> stops)
    {
        var clipped = stops.ToArray();
        for (var i = 0; i < clipped.Length; i++)
        {
            clipped[i] = UtcCalendar.Clip(clipped[i]);
        }

        return clipped;
    }
}
