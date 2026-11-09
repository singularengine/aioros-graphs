using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Scales;

/// <summary>
/// A radial scale, as d3-scale's <c>scaleRadial</c>: linear in the square of the range, so that the area of a circle or
/// a ring grows linearly with the value; ticks are linear. Immutable: the <c>With</c> methods and <see cref="Nice"/>
/// return new scales.
/// </summary>
public sealed class RadialScale
{
    private readonly Continuous squared;
    private readonly double[] range;

    private RadialScale(double[] domain, double[] range, bool clamp, bool round)
    {
        this.range = range;
        IsRounded = round;
        var squares = new double[range.Length];
        for (var i = 0; i < range.Length; i++)
        {
            squares[i] = Square(range[i]);
        }

        squared = new Continuous(domain, squares, ScaleTransform.Identity, clamp, false);
    }

    /// <summary>The domain's stops.</summary>
    public ReadOnlySpan<double> Domain => squared.Domain;

    /// <summary>The range's stops.</summary>
    public ReadOnlySpan<double> Range => range;

    /// <summary>Whether values outside the domain are clamped to it.</summary>
    public bool IsClamped => squared.IsClamped;

    /// <summary>Whether mapped values are rounded to integers.</summary>
    public bool IsRounded { get; }

    /// <summary>Creates a scale from <paramref name="domain"/> to the radii <paramref name="range"/>.</summary>
    /// <param name="domain">At least two domain stops.</param>
    /// <param name="range">At least two radii.</param>
    /// <returns>The scale.</returns>
    public static RadialScale Create(ReadOnlySpan<double> domain, ReadOnlySpan<double> range) =>
        new(Continuous.Stops(domain, nameof(domain)), Continuous.Stops(range, nameof(range)), false, false);

    /// <summary>Returns this scale with another domain.</summary>
    /// <param name="stops">At least two domain stops.</param>
    /// <returns>The new scale.</returns>
    public RadialScale WithDomain(ReadOnlySpan<double> stops) => new(Continuous.Stops(stops, nameof(stops)), range, IsClamped, IsRounded);

    /// <summary>Returns this scale with other radii, not rounded.</summary>
    /// <param name="stops">At least two radii.</param>
    /// <returns>The new scale.</returns>
    public RadialScale WithRange(ReadOnlySpan<double> stops) => new(squared.Domain, Continuous.Stops(stops, nameof(stops)), IsClamped, false);

    /// <summary>Returns this scale with other radii and rounded outputs, as d3's <c>rangeRound</c>.</summary>
    /// <param name="stops">At least two radii.</param>
    /// <returns>The new scale.</returns>
    public RadialScale WithRangeRound(ReadOnlySpan<double> stops) => new(squared.Domain, Continuous.Stops(stops, nameof(stops)), IsClamped, true);

    /// <summary>Returns this scale with clamping on or off.</summary>
    /// <param name="clamp">Whether values outside the domain are clamped.</param>
    /// <returns>The new scale.</returns>
    public RadialScale WithClamp(bool clamp) => new(squared.Domain, range, clamp, IsRounded);

    /// <summary>Maps a domain value to a radius.</summary>
    /// <param name="x">The domain value.</param>
    /// <returns>The radius, or NaN for NaN.</returns>
    public double Map(double x)
    {
        var y = squared.Map(x);
        y = JsMath.Sign(y) * Math.Sqrt(Math.Abs(y));
        return IsRounded && !double.IsNaN(y) ? JsNumber.Round(y) : y;
    }

    /// <summary>Maps domain values to radii without allocating.</summary>
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

    /// <summary>Maps a radius back to the domain.</summary>
    /// <param name="y">The radius.</param>
    /// <returns>The domain value.</returns>
    public double Invert(double y) => squared.Invert(Square(y));

    /// <summary>Returns about <paramref name="count"/> nicely-rounded values within the domain, as d3's <c>scale.ticks(count)</c>.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <returns>The ticks.</returns>
    public double[] Ticks(double count = 10) => Arrays.Ticks.Compute(Domain[0], Domain[^1], count);

    /// <summary>Writes the ticks of <see cref="Ticks(double)"/> without allocating.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="destination">Where the ticks go; <see cref="Arrays.Ticks.Count"/> tells how many.</param>
    /// <returns>The number of ticks written.</returns>
    public int Ticks(double count, Span<double> destination) => Arrays.Ticks.Compute(Domain[0], Domain[^1], count, destination);

    /// <summary>Returns the format for this scale's ticks, as d3's <c>scale.tickFormat(count, specifier)</c>.</summary>
    /// <param name="count">The tick count the ticks were made for.</param>
    /// <param name="specifier">A d3-format specifier, by default <c>,f</c> with the precision the tick step needs.</param>
    /// <returns>The format.</returns>
    public NumberFormat TickFormat(double count = 10, string? specifier = null) =>
        Formatting.TickFormat.Create(Domain[0], Domain[^1], count, specifier);

    /// <summary>Returns this scale with its first and last domain stops extended to nicely-rounded values, as d3's <c>nice</c>.</summary>
    /// <param name="count">The approximate number of ticks the nice domain is built for.</param>
    /// <returns>The new scale.</returns>
    public RadialScale Nice(double count = 10) =>
        LinearNice.Stops(squared.Domain, count) is { } stops ? new RadialScale(stops, range, IsClamped, IsRounded) : this;

    private static double Square(double x) => JsMath.Sign(x) * x * x;
}
