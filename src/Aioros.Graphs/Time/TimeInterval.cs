namespace Aioros.Graphs.Time;

/// <summary>
/// A local time interval of d3-time (<c>timeMillisecond</c>, <c>timeSecond</c>, <c>timeMinute</c>, <c>timeHour</c>,
/// <c>timeDay</c>, <c>timeSunday</c> to <c>timeSaturday</c>, <c>timeMonth</c>, <c>timeYear</c>), or one of their steps
/// from <see cref="Every"/>, over JavaScript time values in a time zone: <see cref="TimeZoneInfo.Local"/> unless
/// <see cref="WithZone"/> gives another.
/// </summary>
public readonly struct TimeInterval : IEquatable<TimeInterval>
{
    private readonly UtcUnit unit;
    private readonly IntervalKind kind;
    private readonly double step;
    private readonly TimeZoneInfo? zone;

    private TimeInterval(UtcUnit unit, IntervalKind kind, double step, TimeZoneInfo? zone = null)
    {
        this.unit = unit;
        this.kind = kind;
        this.step = step;
        this.zone = zone;
    }

    private enum IntervalKind : byte
    {
        Base,
        Filtered,
        MillisecondStep,
        YearStep,
    }

    /// <summary>Gets d3's <c>timeMillisecond</c> (the same as <c>utcMillisecond</c>).</summary>
    public static TimeInterval Millisecond => new(UtcUnit.Millisecond, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeSecond</c>.</summary>
    public static TimeInterval Second => new(UtcUnit.Second, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeMinute</c>.</summary>
    public static TimeInterval Minute => new(UtcUnit.Minute, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeHour</c>.</summary>
    public static TimeInterval Hour => new(UtcUnit.Hour, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeDay</c>: local midnights, whose steps count days of the month; the time ticks use it.</summary>
    public static TimeInterval Day => new(UtcUnit.Day, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeSunday</c> (<c>timeWeek</c>): weeks starting on Sunday.</summary>
    public static TimeInterval Week => new(UtcUnit.Week, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeSunday</c>: weeks starting on Sunday, the same interval as <see cref="Week"/>.</summary>
    public static TimeInterval Sunday => Week;

    /// <summary>Gets d3's <c>timeMonday</c>: weeks starting on Monday.</summary>
    public static TimeInterval Monday => new(UtcUnit.Monday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeTuesday</c>: weeks starting on Tuesday.</summary>
    public static TimeInterval Tuesday => new(UtcUnit.Tuesday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeWednesday</c>: weeks starting on Wednesday.</summary>
    public static TimeInterval Wednesday => new(UtcUnit.Wednesday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeThursday</c>: weeks starting on Thursday.</summary>
    public static TimeInterval Thursday => new(UtcUnit.Thursday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeFriday</c>: weeks starting on Friday.</summary>
    public static TimeInterval Friday => new(UtcUnit.Friday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeSaturday</c>: weeks starting on Saturday.</summary>
    public static TimeInterval Saturday => new(UtcUnit.Saturday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeMonth</c>.</summary>
    public static TimeInterval Month => new(UtcUnit.Month, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>timeYear</c>.</summary>
    public static TimeInterval Year => new(UtcUnit.Year, IntervalKind.Base, 1);

    /// <summary>Gets the time zone: <see cref="TimeZoneInfo.Local"/> unless <see cref="WithZone"/> gave another.</summary>
    public TimeZoneInfo Zone => zone ?? TimeZoneInfo.Local;

    /// <summary>Returns this interval in another time zone.</summary>
    /// <param name="timeZone">The time zone.</param>
    /// <returns>The interval.</returns>
    public TimeInterval WithZone(TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        return new TimeInterval(unit, kind, step, timeZone);
    }

    /// <summary>Returns the latest boundary at or before <paramref name="t"/>, as <c>interval.floor</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The boundary, or NaN for an invalid time.</returns>
    public double Floor(double t) => FloorClipped(UtcCalendar.Clip(t));

    /// <summary>Returns the earliest boundary at or after <paramref name="t"/>, as <c>interval.ceil</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The boundary, or NaN for an invalid time.</returns>
    public double Ceil(double t) => FloorClipped(OffsetClipped(FloorClipped(UtcCalendar.Clip(t - 1)), 1));

    /// <summary>Returns the nearest boundary, the later one on ties, as <c>interval.round</c>.</summary>
    /// <param name="t">The time value.</param>
    /// <returns>The boundary, or NaN for an invalid time.</returns>
    public double Round(double t)
    {
        var floor = Floor(t);
        var ceil = Ceil(t);
        return t - floor < ceil - t ? floor : ceil;
    }

    /// <summary>Moves <paramref name="t"/> by whole intervals, as <c>interval.offset</c>; the step is floored.</summary>
    /// <param name="t">The time value.</param>
    /// <param name="step">The number of intervals, floored.</param>
    /// <returns>The moved time value, not floored.</returns>
    public double Offset(double t, double step = 1) => OffsetClipped(UtcCalendar.Clip(t), Math.Floor(step));

    /// <summary>Returns the number of boundaries after <paramref name="start"/> up to <paramref name="end"/>, as <c>interval.count</c>.</summary>
    /// <param name="start">The first time value.</param>
    /// <param name="end">The second time value.</param>
    /// <returns>The count.</returns>
    /// <exception cref="InvalidOperationException">The interval is a filtered step without a count, as in d3.</exception>
    public double Count(double start, double end)
    {
        if (kind == IntervalKind.Base)
        {
            return Math.Floor(LocalUnits.Count(unit, Floor(start), Floor(end), Zone));
        }

        if (kind == IntervalKind.MillisecondStep)
        {
            return Math.Floor((Floor(end) - Floor(start)) / step);
        }

        throw new InvalidOperationException("This interval has no count, as in d3.");
    }

    /// <summary>
    /// Returns the interval of every <paramref name="step"/>th boundary, as <c>interval.every</c>: boundaries whose
    /// local field (seconds, minutes, hours, day of the month, month) or count since the epoch is a multiple of it.
    /// </summary>
    /// <param name="step">The step, floored.</param>
    /// <returns>The interval, or null when the floored step is not a positive finite number.</returns>
    /// <exception cref="InvalidOperationException">The interval is already a step.</exception>
    public TimeInterval? Every(double step)
    {
        if (kind != IntervalKind.Base)
        {
            throw new InvalidOperationException("Only the base intervals have steps.");
        }

        var k = Math.Floor(step);
        if (!double.IsFinite(k) || !(k > 0))
        {
            return null;
        }

        return unit switch
        {
            UtcUnit.Year => new TimeInterval(unit, IntervalKind.YearStep, k, zone),
            _ when !(k > 1) => this,
            UtcUnit.Millisecond => new TimeInterval(unit, IntervalKind.MillisecondStep, k, zone),
            _ => new TimeInterval(unit, IntervalKind.Filtered, k, zone),
        };
    }

    /// <summary>Returns the boundaries from <paramref name="start"/> inclusive to <paramref name="stop"/> exclusive, as <c>interval.range</c>.</summary>
    /// <param name="start">The first time value, rounded up to a boundary.</param>
    /// <param name="stop">The time value the boundaries stay below.</param>
    /// <param name="step">Every how many boundaries, floored; none when below one.</param>
    /// <returns>The boundaries.</returns>
    public double[] Range(double start, double stop, double step = 1)
    {
        var values = new TimeList(stackalloc double[64]);
        try
        {
            Fill(start, stop, step, ref values);
            return values.ToArray(false);
        }
        finally
        {
            values.Dispose();
        }
    }

    /// <summary>Writes the boundaries of <see cref="Range(double, double, double)"/> without allocating, until <paramref name="destination"/> is full.</summary>
    /// <param name="start">The first time value, rounded up to a boundary.</param>
    /// <param name="stop">The time value the boundaries stay below.</param>
    /// <param name="step">Every how many boundaries, floored.</param>
    /// <param name="destination">Where the boundaries go.</param>
    /// <returns>The number of boundaries written.</returns>
    public int Range(double start, double stop, double step, Span<double> destination)
    {
        var t = Ceil(start);
        step = Math.Floor(step);
        if (!(t < stop) || !(step > 0))
        {
            return 0;
        }

        var count = 0;
        double previous;
        do
        {
            if (count == destination.Length)
            {
                return count;
            }

            previous = t;
            destination[count++] = t;
            t = FloorClipped(OffsetClipped(t, step));
        }
        while (previous < t && t < stop);
        return count;
    }

    internal double MinimumSpacing => (kind, unit) switch
    {
        (IntervalKind.YearStep, _) => step * UtcUnits.Year - UtcUnits.Hour,
        (_, UtcUnit.Millisecond) => step,
        (_, UtcUnit.Second) => Cycled(60) * UtcUnits.Second,
        (_, UtcUnit.Minute) => Cycled(60) * UtcUnits.Minute,
        (_, UtcUnit.Hour) => Math.Max(Cycled(24) - 1, 0.5) * UtcUnits.Hour,
        (_, UtcUnit.Day) => UtcUnits.Day - UtcUnits.Hour,
        (_, UtcUnit.Month) => Cycled(12) * 28 * UtcUnits.Day - UtcUnits.Hour,
        (_, UtcUnit.Year) => step * UtcUnits.Year - UtcUnits.Hour,
        _ => step * UtcUnits.Week - UtcUnits.Hour,
    };

    /// <inheritdoc />
    public bool Equals(TimeInterval other) => unit == other.unit && kind == other.kind && step.Equals(other.step) && Zone.Equals(other.Zone);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TimeInterval other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(unit, kind, step, Zone);

    /// <summary>Compares two intervals.</summary>
    /// <param name="left">The first interval.</param>
    /// <param name="right">The second interval.</param>
    /// <returns>Whether they are the same interval.</returns>
    public static bool operator ==(TimeInterval left, TimeInterval right) => left.Equals(right);

    /// <summary>Compares two intervals.</summary>
    /// <param name="left">The first interval.</param>
    /// <param name="right">The second interval.</param>
    /// <returns>Whether they differ.</returns>
    public static bool operator !=(TimeInterval left, TimeInterval right) => !left.Equals(right);

    internal void Fill(double start, double stop, double step, ref TimeList values)
    {
        var t = Ceil(start);
        step = Math.Floor(step);
        if (!(t < stop) || !(step > 0))
        {
            return;
        }

        double previous;
        do
        {
            previous = t;
            values.Add(t);
            t = FloorClipped(OffsetClipped(t, step));
        }
        while (previous < t && t < stop);
    }

    private double FloorClipped(double t)
    {
        if (double.IsNaN(t))
        {
            return t;
        }

        switch (kind)
        {
            case IntervalKind.MillisecondStep:
                return UtcCalendar.Clip(Math.Floor(t / step) * step);
            case IntervalKind.YearStep:
                return LocalUnits.YearStepFloor(t, step, Zone);
            case IntervalKind.Filtered:
                return FilteredFloor(t);
            default:
                return LocalUnits.Floor(unit, t, Zone);
        }
    }

    private double OffsetClipped(double t, double count) => kind switch
    {
        IntervalKind.MillisecondStep => UtcCalendar.Clip(t + count * step),
        IntervalKind.YearStep => LocalUnits.Offset(UtcUnit.Year, t, count * step, Zone),
        IntervalKind.Filtered => FilteredOffset(t, count),
        _ => LocalUnits.Offset(unit, t, count, Zone),
    };

    private double Cycled(double cycle) => cycle % step == 0 ? step : Math.Min(step, cycle % step);

    private bool Test(double t) => LocalUnits.Field(unit, t, Zone) % step == 0;

    private double FilteredFloor(double t)
    {
        while (true)
        {
            t = LocalUnits.Floor(unit, t, Zone);
            if (double.IsNaN(t) || Test(t))
            {
                return t;
            }

            t = UtcCalendar.Clip(t - 1);
        }
    }

    private double FilteredOffset(double t, double count)
    {
        var direction = count < 0 ? -1 : 1;
        for (var remaining = Math.Abs(count); remaining > 0 && !double.IsNaN(t); remaining--)
        {
            do
            {
                t = LocalUnits.Offset(unit, t, direction, Zone);
            }
            while (!double.IsNaN(t) && !Test(t));
        }

        return t;
    }
}
