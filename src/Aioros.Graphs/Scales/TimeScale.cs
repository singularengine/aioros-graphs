using Aioros.Graphs.Formatting;
using Aioros.Graphs.Time;

namespace Aioros.Graphs.Scales;

/// <summary>
/// A linear scale over local time, as d3-scale's <c>scaleTime</c>: the domain holds JavaScript time values
/// (milliseconds since the Unix epoch, whole and within ±8.64e15), ticks fall on the boundaries of the local calendar
/// of a time zone (<see cref="TimeZoneInfo.Local"/> unless another is given) and labels use d3's local multi-scale
/// format. Immutable: the <c>With</c> methods and <see cref="Nice(double)"/> return new scales.
/// </summary>
public sealed class TimeScale
{
    private readonly LinearScale linear;
    private readonly TimeZoneInfo? zone;

    private TimeScale(LinearScale linear, TimeZoneInfo? zone)
    {
        this.linear = linear;
        this.zone = zone;
    }

    /// <summary>Gets the time zone of the ticks and labels.</summary>
    public TimeZoneInfo Zone => zone ?? TimeZoneInfo.Local;

    /// <summary>The domain's stops, as time values.</summary>
    public ReadOnlySpan<double> Domain => linear.Domain;

    /// <summary>The range's stops.</summary>
    public ReadOnlySpan<double> Range => linear.Range;

    /// <summary>Whether instants outside the domain are clamped to it.</summary>
    public bool IsClamped => linear.IsClamped;

    /// <summary>Creates a scale from a time domain to <paramref name="range"/>.</summary>
    /// <param name="domain">At least two time values; fractions of a millisecond are truncated.</param>
    /// <param name="range">At least two range stops.</param>
    /// <param name="zone">The time zone; <see cref="TimeZoneInfo.Local"/> when null.</param>
    /// <returns>The scale.</returns>
    public static TimeScale Create(ReadOnlySpan<double> domain, ReadOnlySpan<double> range, TimeZoneInfo? zone = null) => new(LinearScale.Create(Clip(domain), range), zone);

    /// <summary>Creates a scale from the instants <paramref name="start"/> to <paramref name="end"/>.</summary>
    /// <param name="start">The domain's start.</param>
    /// <param name="end">The domain's end.</param>
    /// <param name="r0">The range's start.</param>
    /// <param name="r1">The range's end.</param>
    /// <param name="zone">The time zone; <see cref="TimeZoneInfo.Local"/> when null.</param>
    /// <returns>The scale.</returns>
    public static TimeScale Create(DateTimeOffset start, DateTimeOffset end, double r0, double r1, TimeZoneInfo? zone = null) =>
        Create([start.ToUnixTimeMilliseconds(), end.ToUnixTimeMilliseconds()], [r0, r1], zone);

    /// <summary>Returns this scale in another time zone.</summary>
    /// <param name="timeZone">The time zone.</param>
    /// <returns>The new scale.</returns>
    public TimeScale WithZone(TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        return new(linear, timeZone);
    }

    /// <summary>Returns this scale with another domain.</summary>
    /// <param name="stops">At least two time values.</param>
    /// <returns>The new scale.</returns>
    public TimeScale WithDomain(ReadOnlySpan<double> stops) => new(linear.WithDomain(Clip(stops)), zone);

    /// <summary>Returns this scale with another range, not rounded.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public TimeScale WithRange(ReadOnlySpan<double> stops) => new(linear.WithRange(stops), zone);

    /// <summary>Returns this scale with another range whose mapped values are rounded.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public TimeScale WithRangeRound(ReadOnlySpan<double> stops) => new(linear.WithRangeRound(stops), zone);

    /// <summary>Returns this scale with clamping on or off.</summary>
    /// <param name="clamp">Whether instants outside the domain are clamped.</param>
    /// <returns>The new scale.</returns>
    public TimeScale WithClamp(bool clamp) => new(linear.WithClamp(clamp), zone);

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
    public double[] Ticks(double count = 10) => TimeTicks.Ticks(Domain[0], Domain[^1], count, zone);

    /// <summary>Writes about <paramref name="count"/> instants on calendar boundaries within the domain without allocating.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="destination">Where the ticks go; <see cref="MaxTickCount"/> values are always enough.</param>
    /// <returns>The number of ticks written.</returns>
    public int Ticks(double count, Span<double> destination) => TimeTicks.Ticks(Domain[0], Domain[^1], count, destination, zone);

    /// <summary>Returns the instants of <paramref name="interval"/> within the domain.</summary>
    /// <param name="interval">The interval, such as <see cref="TimeInterval.Month"/>.</param>
    /// <returns>The ticks as time values.</returns>
    public double[] Ticks(TimeInterval interval) => TimeTicks.Ticks(Domain[0], Domain[^1], interval);

    /// <summary>Returns an upper bound of the number of ticks <see cref="Ticks(double)"/> returns, to size a buffer.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <returns>The bound.</returns>
    public int MaxTickCount(double count = 10) => TimeTicks.MaxCount(Domain[0], Domain[^1], count, zone);

    /// <summary>Returns this scale with its first and last domain stops extended to the boundaries of the tick interval.</summary>
    /// <param name="count">The approximate number of ticks the interval is chosen for.</param>
    /// <returns>The new scale.</returns>
    public TimeScale Nice(double count = 10)
    {
        var stops = Domain.ToArray();
        (stops[0], stops[^1]) = TimeTicks.Nice(stops[0], stops[^1], count, zone);
        return new TimeScale(linear.WithDomain(stops), zone);
    }

    /// <summary>Returns this scale with its first and last domain stops extended to boundaries of <paramref name="interval"/>.</summary>
    /// <param name="interval">The interval.</param>
    /// <returns>The new scale.</returns>
    public TimeScale Nice(TimeInterval interval)
    {
        var stops = Domain.ToArray();
        var (first, last) = (stops[0], stops[^1]);
        (stops[0], stops[^1]) = last < first ? (interval.Ceil(first), interval.Floor(last)) : (interval.Floor(first), interval.Ceil(last));
        return new TimeScale(linear.WithDomain(stops), zone);
    }

    /// <summary>
    /// Returns the labels of this scale's ticks, as d3's <c>scale.tickFormat(count, specifier)</c>, which ignores the
    /// count for time scales: the local multi-scale format, or <paramref name="specifier"/>, in the scale's zone.
    /// </summary>
    /// <param name="specifier">A d3-time-format specifier, or null for the multi-scale format.</param>
    /// <param name="locale">The locale; d3's default en-US when null.</param>
    /// <returns>The label of a time value.</returns>
    public Func<double, string> TickFormat(string? specifier = null, TimeLocale? locale = null)
    {
        var timeZone = Zone;
        return specifier is null ? t => TimeFormat.MultiScale(t, timeZone, locale) : TimeFormat.Create(specifier, timeZone, locale).Format;
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
