using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Time;

/// <summary>
/// Local calendar ticks between two time values in a time zone, as d3-time's <c>timeTicks</c> and
/// <c>timeTickInterval</c>: the interval among 1, 5, 15 and 30 seconds and minutes, 1, 3, 6 and 12 hours, 1 and 2 days
/// of the month, weeks, 1 and 3 months and years whose duration is closest to the span divided by the count. The zone
/// is <see cref="TimeZoneInfo.Local"/> when none is given.
/// </summary>
public static class TimeTicks
{
    private static readonly (UtcUnit Unit, double Step, double Duration)[] Intervals =
    [
        (UtcUnit.Second, 1, UtcUnits.Second),
        (UtcUnit.Second, 5, 5 * UtcUnits.Second),
        (UtcUnit.Second, 15, 15 * UtcUnits.Second),
        (UtcUnit.Second, 30, 30 * UtcUnits.Second),
        (UtcUnit.Minute, 1, UtcUnits.Minute),
        (UtcUnit.Minute, 5, 5 * UtcUnits.Minute),
        (UtcUnit.Minute, 15, 15 * UtcUnits.Minute),
        (UtcUnit.Minute, 30, 30 * UtcUnits.Minute),
        (UtcUnit.Hour, 1, UtcUnits.Hour),
        (UtcUnit.Hour, 3, 3 * UtcUnits.Hour),
        (UtcUnit.Hour, 6, 6 * UtcUnits.Hour),
        (UtcUnit.Hour, 12, 12 * UtcUnits.Hour),
        (UtcUnit.Day, 1, UtcUnits.Day),
        (UtcUnit.Day, 2, 2 * UtcUnits.Day),
        (UtcUnit.Week, 1, UtcUnits.Week),
        (UtcUnit.Month, 1, UtcUnits.Month),
        (UtcUnit.Month, 3, 3 * UtcUnits.Month),
        (UtcUnit.Year, 1, UtcUnits.Year),
    ];

    /// <summary>Returns the ticks between <paramref name="start"/> and <paramref name="stop"/>, both inclusive.</summary>
    /// <param name="start">The first time value; the ticks are in reverse order when it is after <paramref name="stop"/>.</param>
    /// <param name="stop">The second time value.</param>
    /// <param name="count">The approximate number of ticks wanted.</param>
    /// <param name="zone">The time zone; <see cref="TimeZoneInfo.Local"/> when null.</param>
    /// <returns>The ticks, empty when no interval fits.</returns>
    public static double[] Ticks(double start, double stop, double count, TimeZoneInfo? zone = null) =>
        TickInterval(Math.Min(start, stop), Math.Max(start, stop), count, zone) is { } interval ? Ticks(start, stop, interval) : [];

    /// <summary>Returns the boundaries of <paramref name="interval"/> between <paramref name="start"/> and <paramref name="stop"/>, both inclusive.</summary>
    /// <param name="start">The first time value.</param>
    /// <param name="stop">The second time value.</param>
    /// <param name="interval">The interval.</param>
    /// <returns>The ticks.</returns>
    public static double[] Ticks(double start, double stop, TimeInterval interval)
    {
        var reverse = stop < start;
        var values = new TimeList(stackalloc double[64]);
        try
        {
            if (reverse)
            {
                interval.Fill(stop, start + 1, 1, ref values);
            }
            else
            {
                interval.Fill(start, stop + 1, 1, ref values);
            }

            return values.ToArray(reverse);
        }
        finally
        {
            values.Dispose();
        }
    }

    /// <summary>Writes the ticks of <see cref="Ticks(double, double, double, TimeZoneInfo?)"/> without allocating, until <paramref name="destination"/> is full.</summary>
    /// <param name="start">The first time value.</param>
    /// <param name="stop">The second time value.</param>
    /// <param name="count">The approximate number of ticks wanted.</param>
    /// <param name="zone">The time zone; <see cref="TimeZoneInfo.Local"/> when null.</param>
    /// <param name="destination">Where the ticks go; <see cref="MaxCount"/> values are always enough.</param>
    /// <returns>The number of ticks written.</returns>
    public static int Ticks(double start, double stop, double count, Span<double> destination, TimeZoneInfo? zone = null)
    {
        if (TickInterval(Math.Min(start, stop), Math.Max(start, stop), count, zone) is not { } interval)
        {
            return 0;
        }

        var reverse = stop < start;
        var written = reverse ? interval.Range(stop, start + 1, 1, destination) : interval.Range(start, stop + 1, 1, destination);
        if (reverse)
        {
            destination[..written].Reverse();
        }

        return written;
    }

    /// <summary>Returns an upper bound of the number of ticks <see cref="Ticks(double, double, double, TimeZoneInfo?)"/> returns, to size a buffer.</summary>
    /// <param name="start">The first time value.</param>
    /// <param name="stop">The second time value.</param>
    /// <param name="count">The approximate number of ticks wanted.</param>
    /// <param name="zone">The time zone; <see cref="TimeZoneInfo.Local"/> when null.</param>
    /// <returns>The bound; 0 when no interval fits.</returns>
    public static int MaxCount(double start, double stop, double count, TimeZoneInfo? zone = null)
    {
        var (low, high) = (Math.Min(start, stop), Math.Max(start, stop));
        if (TickInterval(low, high, count, zone) is not { } interval)
        {
            return 0;
        }

        var bound = Math.Floor((high - low) / interval.MinimumSpacing) + 2;
        return bound < int.MaxValue ? (int)bound : int.MaxValue;
    }

    /// <summary>Returns the interval d3's <c>utcTickInterval</c> picks for about <paramref name="count"/> ticks.</summary>
    /// <param name="start">The first time value.</param>
    /// <param name="stop">The second time value.</param>
    /// <param name="count">The approximate number of ticks wanted.</param>
    /// <param name="zone">The time zone; <see cref="TimeZoneInfo.Local"/> when null.</param>
    /// <returns>The interval, or null when none fits.</returns>
    public static TimeInterval? TickInterval(double start, double stop, double count, TimeZoneInfo? zone = null)
    {
        var target = Math.Abs(stop - start) / count;
        var i = Right(target);
        if (i == Intervals.Length)
        {
            return Zoned(TimeInterval.Year, zone).Every(Arrays.Ticks.Step(start / UtcUnits.Year, stop / UtcUnits.Year, count));
        }

        if (i == 0)
        {
            return Zoned(TimeInterval.Millisecond, zone).Every(Math.Max(Arrays.Ticks.Step(start, stop, count), 1));
        }

        var (unit, step, _) = Intervals[target / Intervals[i - 1].Duration < Intervals[i].Duration / target ? i - 1 : i];
        return Zoned(Base(unit), zone).Every(step);
    }

    /// <summary>Extends a span to the boundaries of its tick interval, as the <c>nice</c> of d3's <c>scaleTime</c>.</summary>
    /// <param name="start">The first time value.</param>
    /// <param name="stop">The second time value.</param>
    /// <param name="count">The approximate number of ticks wanted.</param>
    /// <param name="zone">The time zone; <see cref="TimeZoneInfo.Local"/> when null.</param>
    /// <returns>The extended span in the same direction, or the span itself when no interval fits.</returns>
    public static (double Start, double Stop) Nice(double start, double stop, double count, TimeZoneInfo? zone = null)
    {
        if (TickInterval(start, stop, count, zone) is not { } interval)
        {
            return (start, stop);
        }

        return stop < start ? (interval.Ceil(start), interval.Floor(stop)) : (interval.Floor(start), interval.Ceil(stop));
    }

    private static int Right(double target)
    {
        if (double.IsNaN(target))
        {
            return Intervals.Length;
        }

        var lo = 0;
        var hi = Intervals.Length;
        while (lo < hi)
        {
            var mid = (lo + hi) >>> 1;
            if (Intervals[mid].Duration <= target)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    private static TimeInterval Zoned(TimeInterval interval, TimeZoneInfo? zone) => zone is null ? interval : interval.WithZone(zone);

    private static TimeInterval Base(UtcUnit unit) => unit switch
    {
        UtcUnit.Second => TimeInterval.Second,
        UtcUnit.Minute => TimeInterval.Minute,
        UtcUnit.Hour => TimeInterval.Hour,
        UtcUnit.Day => TimeInterval.Day,
        UtcUnit.Week => TimeInterval.Week,
        UtcUnit.Month => TimeInterval.Month,
        _ => TimeInterval.Year,
    };
}
