using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Scales;

/// <summary>
/// A power scale, as d3-scale's <c>scalePow</c> (and <c>scaleSqrt</c> with the exponent 0.5): maps through <c>x^k</c>, keeping the sign of negative values, with linear ticks. Immutable: the <c>With</c> methods and <see cref="Nice"/> return new scales.
/// </summary>
public sealed class PowScale
{
    private readonly Continuous continuous;

    private PowScale(double[] domain, double[] range, double exponent, bool clamp, bool round)
    {
        Exponent = exponent;
        continuous = new Continuous(domain, range, ScaleTransform.Pow(exponent), clamp, round);
    }

    /// <summary>The domain's stops.</summary>
    public ReadOnlySpan<double> Domain => continuous.Domain;

    /// <summary>The range's stops.</summary>
    public ReadOnlySpan<double> Range => continuous.Range;

    /// <summary>The exponent; 1 is linear and 0.5 a square root.</summary>
    public double Exponent { get; }

    /// <summary>Whether values outside the domain are clamped to it.</summary>
    public bool IsClamped => continuous.IsClamped;

    /// <summary>Whether mapped values are rounded to integers.</summary>
    public bool IsRounded => continuous.IsRounded;

    /// <summary>Creates a scale from <paramref name="domain"/> to <paramref name="range"/>.</summary>
    /// <param name="domain">At least two domain stops.</param>
    /// <param name="range">At least two range stops.</param>
    /// <param name="exponent">The exponent; 1 is linear and 0.5 a square root.</param>
    /// <returns>The scale.</returns>
    public static PowScale Create(ReadOnlySpan<double> domain, ReadOnlySpan<double> range, double exponent = 1) =>
        new(Continuous.Stops(domain, nameof(domain)), Continuous.Stops(range, nameof(range)), exponent, false, false);

    /// <summary>Creates a square-root scale, as d3's <c>scaleSqrt</c>.</summary>
    /// <param name="domain">At least two domain stops.</param>
    /// <param name="range">At least two range stops.</param>
    /// <returns>The scale.</returns>
    public static PowScale Sqrt(ReadOnlySpan<double> domain, ReadOnlySpan<double> range) => Create(domain, range, 0.5);

    /// <summary>Returns this scale with another exponent.</summary>
    /// <param name="exponent">The exponent; 1 is linear and 0.5 a square root.</param>
    /// <returns>The new scale.</returns>
    public PowScale WithExponent(double exponent) => new(continuous.Domain, continuous.Range, exponent, IsClamped, IsRounded);

    /// <summary>Returns this scale with another domain.</summary>
    /// <param name="stops">At least two domain stops.</param>
    /// <returns>The new scale.</returns>
    public PowScale WithDomain(ReadOnlySpan<double> stops) => new(Continuous.Stops(stops, nameof(stops)), continuous.Range, Exponent, IsClamped, IsRounded);

    /// <summary>Returns this scale with another range, not rounded.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public PowScale WithRange(ReadOnlySpan<double> stops) => new(continuous.Domain, Continuous.Stops(stops, nameof(stops)), Exponent, IsClamped, false);

    /// <summary>Returns this scale with another range whose mapped values are rounded, as d3's <c>rangeRound</c>.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public PowScale WithRangeRound(ReadOnlySpan<double> stops) => new(continuous.Domain, Continuous.Stops(stops, nameof(stops)), Exponent, IsClamped, true);

    /// <summary>Returns this scale with clamping on or off.</summary>
    /// <param name="clamp">Whether values outside the domain are clamped.</param>
    /// <returns>The new scale.</returns>
    public PowScale WithClamp(bool clamp) => new(continuous.Domain, continuous.Range, Exponent, clamp, IsRounded);

    /// <summary>Maps a domain value to the range.</summary>
    /// <param name="x">The domain value.</param>
    /// <returns>The range value, or NaN for NaN.</returns>
    public double Map(double x) => continuous.Map(x);

    /// <summary>Maps domain values to the range without allocating.</summary>
    /// <param name="values">The domain values.</param>
    /// <param name="destination">At least as many values as <paramref name="values"/>.</param>
    public void Map(ReadOnlySpan<double> values, Span<double> destination) => continuous.Map(values, destination);

    /// <summary>Maps a range value back to the domain.</summary>
    /// <param name="y">The range value.</param>
    /// <returns>The domain value.</returns>
    public double Invert(double y) => continuous.Invert(y);

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
    public PowScale Nice(double count = 10) =>
        LinearNice.Stops(continuous.Domain, count) is { } stops ? new PowScale(stops, continuous.Range, Exponent, IsClamped, IsRounded) : this;
}
