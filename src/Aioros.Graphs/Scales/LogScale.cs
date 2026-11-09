namespace Aioros.Graphs.Scales;

/// <summary>
/// A logarithmic scale, as d3-scale's <c>scaleLog</c>: maps through <c>log(x)</c>, or <c>-log(-x)</c> when the domain is
/// negative, with ticks on the powers of its base and their multiples. Immutable: the <c>With</c> methods and
/// <see cref="Nice"/> return new scales.
/// </summary>
public sealed class LogScale
{
    private readonly Continuous continuous;
    private readonly LogBase logBase;

    private LogScale(double[] domain, double[] range, double @base, bool clamp, bool round)
    {
        var negative = domain[0] < 0;
        continuous = new Continuous(domain, range, ScaleTransform.Log(negative), clamp, round);
        logBase = new LogBase(@base, negative);
    }

    /// <summary>The domain's stops.</summary>
    public ReadOnlySpan<double> Domain => continuous.Domain;

    /// <summary>The range's stops.</summary>
    public ReadOnlySpan<double> Range => continuous.Range;

    /// <summary>The base of the ticks and of <see cref="Nice"/>; 10 by default.</summary>
    public double Base => logBase.Base;

    /// <summary>Whether values outside the domain are clamped to it.</summary>
    public bool IsClamped => continuous.IsClamped;

    /// <summary>Whether mapped values are rounded to integers.</summary>
    public bool IsRounded => continuous.IsRounded;

    /// <summary>Creates a scale from <paramref name="domain"/> to <paramref name="range"/>, in base 10.</summary>
    /// <param name="domain">At least two stops, all positive or all negative.</param>
    /// <param name="range">At least two range stops.</param>
    /// <returns>The scale.</returns>
    public static LogScale Create(ReadOnlySpan<double> domain, ReadOnlySpan<double> range) =>
        new(Continuous.Stops(domain, nameof(domain)), Continuous.Stops(range, nameof(range)), 10, false, false);

    /// <summary>Returns this scale with another base.</summary>
    /// <param name="base">The base, such as 2 or <see cref="Math.E"/>.</param>
    /// <returns>The new scale.</returns>
    public LogScale WithBase(double @base) => new(continuous.Domain, continuous.Range, @base, IsClamped, IsRounded);

    /// <summary>Returns this scale with another domain.</summary>
    /// <param name="stops">At least two stops.</param>
    /// <returns>The new scale.</returns>
    public LogScale WithDomain(ReadOnlySpan<double> stops) => new(Continuous.Stops(stops, nameof(stops)), continuous.Range, Base, IsClamped, IsRounded);

    /// <summary>Returns this scale with another range, not rounded.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public LogScale WithRange(ReadOnlySpan<double> stops) => new(continuous.Domain, Continuous.Stops(stops, nameof(stops)), Base, IsClamped, false);

    /// <summary>Returns this scale with another range whose mapped values are rounded, as d3's <c>rangeRound</c>.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public LogScale WithRangeRound(ReadOnlySpan<double> stops) => new(continuous.Domain, Continuous.Stops(stops, nameof(stops)), Base, IsClamped, true);

    /// <summary>Returns this scale with clamping on or off.</summary>
    /// <param name="clamp">Whether values outside the domain are clamped.</param>
    /// <returns>The new scale.</returns>
    public LogScale WithClamp(bool clamp) => new(continuous.Domain, continuous.Range, Base, clamp, IsRounded);

    /// <summary>Maps a domain value to the range.</summary>
    /// <param name="x">The domain value.</param>
    /// <returns>The range value; NaN for NaN and for values of the other sign.</returns>
    public double Map(double x) => continuous.Map(x);

    /// <summary>Maps domain values to the range without allocating.</summary>
    /// <param name="values">The domain values.</param>
    /// <param name="destination">At least as many values as <paramref name="values"/>.</param>
    public void Map(ReadOnlySpan<double> values, Span<double> destination) => continuous.Map(values, destination);

    /// <summary>Maps a range value back to the domain.</summary>
    /// <param name="y">The range value.</param>
    /// <returns>The domain value.</returns>
    public double Invert(double y) => continuous.Invert(y);

    /// <summary>Returns about <paramref name="count"/> ticks, as d3's <c>scale.ticks(count)</c>.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <returns>The ticks.</returns>
    public double[] Ticks(double count = 10) => logBase.Ticks(Domain[0], Domain[^1], count);

    /// <summary>Writes the ticks of <see cref="Ticks(double)"/> without allocating.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="destination">Where the ticks go.</param>
    /// <returns>The number of ticks; when it exceeds the destination's length, the destination holds no meaningful values.</returns>
    public int Ticks(double count, Span<double> destination) => logBase.Ticks(Domain[0], Domain[^1], count, destination);

    /// <summary>Returns the tick format, as d3's <c>scale.tickFormat(count, specifier)</c>.</summary>
    /// <param name="count">The tick count the labels are for; <see cref="double.PositiveInfinity"/> labels every tick.</param>
    /// <param name="specifier">A d3-format specifier, by default <c>s</c> in base 10 and <c>,</c> otherwise; trimmed for integer bases unless it gives a precision.</param>
    /// <returns>The format.</returns>
    public TickLabelFormat TickFormat(double count = 10, string? specifier = null) => logBase.TickFormat(Domain[0], Domain[^1], count, specifier);

    /// <summary>Returns this scale with its first and last stops rounded out to powers of the base, as d3's <c>nice</c>.</summary>
    /// <returns>The new scale.</returns>
    public LogScale Nice() => new(logBase.Nice(continuous.Domain), continuous.Range, Base, IsClamped, IsRounded);
}
