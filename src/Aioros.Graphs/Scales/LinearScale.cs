using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Scales;

/// <summary>
/// A continuous linear scale, as d3-scale's <c>scaleLinear</c>: maps a numeric domain to a numeric range, piecewise
/// when either has more than two stops. Immutable: the <c>With</c> methods and <see cref="Nice"/> return new scales.
/// </summary>
public sealed class LinearScale
{
    private readonly double[] domain;
    private readonly double[] range;
    private readonly Piecewise output;
    private readonly Piecewise input;
    private readonly double clampLow;
    private readonly double clampHigh;

    private LinearScale(double[] domain, double[] range, bool clamp, bool round)
    {
        this.domain = domain;
        this.range = range;
        IsClamped = clamp;
        IsRounded = round;
        output = new Piecewise(domain, range, round);
        input = new Piecewise(range, domain, false);
        var last = domain[Math.Min(domain.Length, range.Length) - 1];
        (clampLow, clampHigh) = domain[0] > last ? (last, domain[0]) : (domain[0], last);
    }

    /// <summary>The domain's stops.</summary>
    public ReadOnlySpan<double> Domain => domain;

    /// <summary>The range's stops.</summary>
    public ReadOnlySpan<double> Range => range;

    /// <summary>Whether values outside the domain are clamped to it.</summary>
    public bool IsClamped { get; }

    /// <summary>Whether mapped values are rounded to integers, as with d3's <c>rangeRound</c>.</summary>
    public bool IsRounded { get; }

    /// <summary>Creates a scale from <paramref name="domain"/> to <paramref name="range"/>.</summary>
    /// <param name="domain">At least two domain stops.</param>
    /// <param name="range">At least two range stops.</param>
    /// <returns>The scale.</returns>
    public static LinearScale Create(ReadOnlySpan<double> domain, ReadOnlySpan<double> range) =>
        new(Stops(domain, nameof(domain)), Stops(range, nameof(range)), false, false);

    /// <summary>Returns this scale with another domain.</summary>
    /// <param name="stops">At least two domain stops.</param>
    /// <returns>The new scale.</returns>
    public LinearScale WithDomain(ReadOnlySpan<double> stops) => new(Stops(stops, nameof(stops)), range, IsClamped, IsRounded);

    /// <summary>Returns this scale with another range, not rounded.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public LinearScale WithRange(ReadOnlySpan<double> stops) => new(domain, Stops(stops, nameof(stops)), IsClamped, false);

    /// <summary>Returns this scale with another range whose mapped values are rounded, as d3's <c>rangeRound</c>.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public LinearScale WithRangeRound(ReadOnlySpan<double> stops) => new(domain, Stops(stops, nameof(stops)), IsClamped, true);

    /// <summary>Returns this scale with clamping on or off.</summary>
    /// <param name="clamp">Whether values outside the domain are clamped.</param>
    /// <returns>The new scale.</returns>
    public LinearScale WithClamp(bool clamp) => new(domain, range, clamp, IsRounded);

    /// <summary>Maps a domain value to the range.</summary>
    /// <param name="x">The domain value.</param>
    /// <returns>The range value, or NaN for NaN.</returns>
    public double Map(double x)
    {
        if (double.IsNaN(x))
        {
            return double.NaN;
        }

        return output.Map(IsClamped ? Math.Max(clampLow, Math.Min(clampHigh, x)) : x);
    }

    /// <summary>Maps domain values to the range without allocating.</summary>
    /// <param name="values">The domain values.</param>
    /// <param name="destination">At least as many values as <paramref name="values"/>.</param>
    public void Map(ReadOnlySpan<double> values, Span<double> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            destination[i] = Map(values[i]);
        }
    }

    /// <summary>Maps a range value back to the domain.</summary>
    /// <param name="y">The range value.</param>
    /// <returns>The domain value.</returns>
    public double Invert(double y)
    {
        var x = input.Map(y);
        return IsClamped ? Math.Max(clampLow, Math.Min(clampHigh, x)) : x;
    }

    /// <summary>Returns about <paramref name="count"/> nicely-rounded values within the domain.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <returns>The ticks.</returns>
    public double[] Ticks(double count = 10) => Arrays.Ticks.Compute(domain[0], domain[^1], count);

    /// <summary>Writes about <paramref name="count"/> nicely-rounded values within the domain without allocating.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="destination">Where the ticks go; <see cref="Arrays.Ticks.Count"/> tells how many.</param>
    /// <returns>The number of ticks written.</returns>
    public int Ticks(double count, Span<double> destination) => Arrays.Ticks.Compute(domain[0], domain[^1], count, destination);

    /// <summary>Returns this scale with its first and last domain stops extended to nicely-rounded values, as d3's <c>nice</c>.</summary>
    /// <param name="count">The approximate number of ticks the nice domain is built for.</param>
    /// <returns>The new scale.</returns>
    public LinearScale Nice(double count = 10) =>
        LinearNice.Stops(domain, count) is { } stops ? new LinearScale(stops, range, IsClamped, IsRounded) : this;

    /// <summary>Returns the format for this scale's ticks, as d3's <c>scale.tickFormat(count, specifier)</c>.</summary>
    /// <param name="count">The tick count the ticks were made for.</param>
    /// <param name="specifier">A d3-format specifier, by default <c>,f</c> with the precision the tick step needs.</param>
    /// <returns>The format.</returns>
    public NumberFormat TickFormat(double count = 10, string? specifier = null) =>
        Formatting.TickFormat.Create(domain[0], domain[^1], count, specifier);

    private static double[] Stops(ReadOnlySpan<double> stops, string name)
    {
        if (stops.Length < 2)
        {
            throw new ArgumentException("A continuous scale needs at least two stops.", name);
        }

        return stops.ToArray();
    }
}
