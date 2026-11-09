using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Scales;

/// <summary>
/// A bi-symmetric log scale, as d3-scale's <c>scaleSymlog</c>: maps through <c>sign(x) log1p(|x| / c)</c>, so it spans zero and negative values, with linear ticks. Immutable: the <c>With</c> methods and <see cref="Nice"/> return new scales.
/// </summary>
public sealed class SymlogScale
{
    private readonly Continuous continuous;

    private SymlogScale(double[] domain, double[] range, double constant, bool clamp, bool round)
    {
        Constant = constant;
        continuous = new Continuous(domain, range, ScaleTransform.Symlog(constant), clamp, round);
    }

    /// <summary>The domain's stops.</summary>
    public ReadOnlySpan<double> Domain => continuous.Domain;

    /// <summary>The range's stops.</summary>
    public ReadOnlySpan<double> Range => continuous.Range;

    /// <summary>The constant c where the scale turns from linear to logarithmic; 1 by default.</summary>
    public double Constant { get; }

    /// <summary>Whether values outside the domain are clamped to it.</summary>
    public bool IsClamped => continuous.IsClamped;

    /// <summary>Whether mapped values are rounded to integers.</summary>
    public bool IsRounded => continuous.IsRounded;

    /// <summary>Creates a scale from <paramref name="domain"/> to <paramref name="range"/>.</summary>
    /// <param name="domain">At least two domain stops.</param>
    /// <param name="range">At least two range stops.</param>
    /// <param name="constant">The constant c where the scale turns from linear to logarithmic; 1 by default.</param>
    /// <returns>The scale.</returns>
    public static SymlogScale Create(ReadOnlySpan<double> domain, ReadOnlySpan<double> range, double constant = 1) =>
        new(Continuous.Stops(domain, nameof(domain)), Continuous.Stops(range, nameof(range)), constant, false, false);

    /// <summary>Returns this scale with another constant.</summary>
    /// <param name="constant">The constant c where the scale turns from linear to logarithmic; 1 by default.</param>
    /// <returns>The new scale.</returns>
    public SymlogScale WithConstant(double constant) => new(continuous.Domain, continuous.Range, constant, IsClamped, IsRounded);

    /// <summary>Returns this scale with another domain.</summary>
    /// <param name="stops">At least two domain stops.</param>
    /// <returns>The new scale.</returns>
    public SymlogScale WithDomain(ReadOnlySpan<double> stops) => new(Continuous.Stops(stops, nameof(stops)), continuous.Range, Constant, IsClamped, IsRounded);

    /// <summary>Returns this scale with another range, not rounded.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public SymlogScale WithRange(ReadOnlySpan<double> stops) => new(continuous.Domain, Continuous.Stops(stops, nameof(stops)), Constant, IsClamped, false);

    /// <summary>Returns this scale with another range whose mapped values are rounded, as d3's <c>rangeRound</c>.</summary>
    /// <param name="stops">At least two range stops.</param>
    /// <returns>The new scale.</returns>
    public SymlogScale WithRangeRound(ReadOnlySpan<double> stops) => new(continuous.Domain, Continuous.Stops(stops, nameof(stops)), Constant, IsClamped, true);

    /// <summary>Returns this scale with clamping on or off.</summary>
    /// <param name="clamp">Whether values outside the domain are clamped.</param>
    /// <returns>The new scale.</returns>
    public SymlogScale WithClamp(bool clamp) => new(continuous.Domain, continuous.Range, Constant, clamp, IsRounded);

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
    public SymlogScale Nice(double count = 10) =>
        LinearNice.Stops(continuous.Domain, count) is { } stops ? new SymlogScale(stops, continuous.Range, Constant, IsClamped, IsRounded) : this;
}
