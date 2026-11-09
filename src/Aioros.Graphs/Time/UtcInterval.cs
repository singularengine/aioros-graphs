namespace Aioros.Graphs.Time;

/// <summary>
/// A UTC time interval of d3-time (<c>utcMillisecond</c>, <c>utcSecond</c>, <c>utcMinute</c>, <c>utcHour</c>,
/// <c>utcDay</c>, <c>unixDay</c>, <c>utcSunday</c>, <c>utcMonth</c>, <c>utcYear</c>), or one of their steps from
/// <see cref="Every"/>, over JavaScript time values in milliseconds since the Unix epoch.
/// </summary>
public readonly struct UtcInterval : IEquatable<UtcInterval>
{
    private readonly UtcUnit unit;
    private readonly IntervalKind kind;
    private readonly double step;

    private UtcInterval(UtcUnit unit, IntervalKind kind, double step)
    {
        this.unit = unit;
        this.kind = kind;
        this.step = step;
    }

    private enum IntervalKind : byte
    {
        Base,
        Filtered,
        MillisecondStep,
        YearStep,
    }

    /// <summary>Gets d3's <c>utcMillisecond</c>.</summary>
    public static UtcInterval Millisecond => new(UtcUnit.Millisecond, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcSecond</c>.</summary>
    public static UtcInterval Second => new(UtcUnit.Second, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcMinute</c>.</summary>
    public static UtcInterval Minute => new(UtcUnit.Minute, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcHour</c>.</summary>
    public static UtcInterval Hour => new(UtcUnit.Hour, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcDay</c>, whose steps count days of the month.</summary>
    public static UtcInterval Day => new(UtcUnit.Day, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>unixDay</c>, whose steps count days since the epoch; the time ticks use it.</summary>
    public static UtcInterval UnixDay => new(UtcUnit.UnixDay, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcSunday</c> (<c>utcWeek</c>): weeks starting on Sunday.</summary>
    public static UtcInterval Week => new(UtcUnit.Week, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcSunday</c>: weeks starting on Sunday, the same interval as <see cref="Week"/>.</summary>
    public static UtcInterval Sunday => Week;

    /// <summary>Gets d3's <c>utcMonday</c>: weeks starting on Monday.</summary>
    public static UtcInterval Monday => new(UtcUnit.Monday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcTuesday</c>: weeks starting on Tuesday.</summary>
    public static UtcInterval Tuesday => new(UtcUnit.Tuesday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcWednesday</c>: weeks starting on Wednesday.</summary>
    public static UtcInterval Wednesday => new(UtcUnit.Wednesday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcThursday</c>: weeks starting on Thursday.</summary>
    public static UtcInterval Thursday => new(UtcUnit.Thursday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcFriday</c>: weeks starting on Friday.</summary>
    public static UtcInterval Friday => new(UtcUnit.Friday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcSaturday</c>: weeks starting on Saturday.</summary>
    public static UtcInterval Saturday => new(UtcUnit.Saturday, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcMonth</c>.</summary>
    public static UtcInterval Month => new(UtcUnit.Month, IntervalKind.Base, 1);

    /// <summary>Gets d3's <c>utcYear</c>.</summary>
    public static UtcInterval Year => new(UtcUnit.Year, IntervalKind.Base, 1);

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
            return Math.Floor(UtcUnits.Count(unit, Floor(start), Floor(end)));
        }

        if (kind == IntervalKind.MillisecondStep)
        {
            return Math.Floor((Floor(end) - Floor(start)) / step);
        }

        throw new InvalidOperationException("This interval has no count, as in d3.");
    }

    /// <summary>
    /// Returns the interval of every <paramref name="step"/>th boundary, as <c>interval.every</c>: boundaries whose
    /// field (seconds, minutes, hours, day of the month, Unix day, month) or count since the epoch is a multiple of it.
    /// </summary>
    /// <param name="step">The step, floored.</param>
    /// <returns>The interval, or null when the floored step is not a positive finite number.</returns>
    /// <exception cref="InvalidOperationException">The interval is already a step.</exception>
    public UtcInterval? Every(double step)
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
            UtcUnit.Year => new UtcInterval(unit, IntervalKind.YearStep, k),
            _ when !(k > 1) => this,
            UtcUnit.Millisecond => new UtcInterval(unit, IntervalKind.MillisecondStep, k),
            _ => new UtcInterval(unit, IntervalKind.Filtered, k),
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
        (IntervalKind.YearStep, _) => step * UtcUnits.Year,
        (_, UtcUnit.Millisecond) => step,
        (_, UtcUnit.Second) => Cycled(60) * UtcUnits.Second,
        (_, UtcUnit.Minute) => Cycled(60) * UtcUnits.Minute,
        (_, UtcUnit.Hour) => Cycled(24) * UtcUnits.Hour,
        (_, UtcUnit.Day) => UtcUnits.Day,
        (_, UtcUnit.Month) => Cycled(12) * 28 * UtcUnits.Day,
        (_, UtcUnit.Year) => step * UtcUnits.Year,
        (_, UtcUnit.Day or UtcUnit.UnixDay) => step * UtcUnits.Day,
        _ => step * UtcUnits.Week,
    };

    /// <inheritdoc />
    public bool Equals(UtcInterval other) => unit == other.unit && kind == other.kind && step.Equals(other.step);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is UtcInterval other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(unit, kind, step);

    /// <summary>Compares two intervals.</summary>
    /// <param name="left">The first interval.</param>
    /// <param name="right">The second interval.</param>
    /// <returns>Whether they are the same interval.</returns>
    public static bool operator ==(UtcInterval left, UtcInterval right) => left.Equals(right);

    /// <summary>Compares two intervals.</summary>
    /// <param name="left">The first interval.</param>
    /// <param name="right">The second interval.</param>
    /// <returns>Whether they differ.</returns>
    public static bool operator !=(UtcInterval left, UtcInterval right) => !left.Equals(right);

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
                return UtcCalendar.Compose(Math.Floor(UtcCalendar.Year(t) / step) * step, 0, 1);
            case IntervalKind.Filtered:
                return FilteredFloor(t);
            default:
                return UtcUnits.Floor(unit, t);
        }
    }

    private double OffsetClipped(double t, double count) => kind switch
    {
        IntervalKind.MillisecondStep => UtcCalendar.Clip(t + count * step),
        IntervalKind.YearStep => UtcUnits.WithFields(t, count * step, 0),
        IntervalKind.Filtered => FilteredOffset(t, count),
        _ => UtcUnits.Offset(unit, t, count),
    };

    private double Cycled(double cycle) => cycle % step == 0 ? step : Math.Min(step, cycle % step);

    private bool Test(double t) => UtcUnits.Field(unit, t) % step == 0;

    private double FilteredFloor(double t)
    {
        while (true)
        {
            t = UtcUnits.Floor(unit, t);
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
                t = UtcUnits.Offset(unit, t, direction);
            }
            while (!double.IsNaN(t) && !Test(t));
        }

        return t;
    }
}
