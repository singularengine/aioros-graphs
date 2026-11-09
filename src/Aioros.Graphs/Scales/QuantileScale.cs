using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Scales;

/// <summary>
/// A quantile scale, as d3-scale's <c>scaleQuantile</c>: divides sample values into as many groups of equal size as
/// there are range values. Immutable: <see cref="WithUnknown"/> returns a new scale.
/// </summary>
/// <typeparam name="T">The range value, such as a color.</typeparam>
public sealed class QuantileScale<T>
{
    private readonly double[] domain;
    private readonly T[] range;
    private readonly double[] thresholds;

    private QuantileScale(double[] domain, T[] range, double[] thresholds, T unknown)
    {
        this.domain = domain;
        this.range = range;
        this.thresholds = thresholds;
        Unknown = unknown;
    }

    /// <summary>The sample values, sorted ascending, NaN removed.</summary>
    public ReadOnlySpan<double> Domain => domain;

    /// <summary>The range's values.</summary>
    public ReadOnlySpan<T> Range => range;

    /// <summary>The boundaries between the groups, as d3's <c>scale.quantiles()</c>.</summary>
    public ReadOnlySpan<double> Quantiles => thresholds;

    /// <summary>The value NaN, and inputs beyond an empty range, map to; <c>default</c> unless set with <see cref="WithUnknown"/>.</summary>
    public T Unknown { get; }

    /// <summary>Creates a scale from sample values to <paramref name="range"/>.</summary>
    /// <param name="samples">The sample values, in any order; NaN values are dropped.</param>
    /// <param name="range">The range's values.</param>
    /// <returns>The scale.</returns>
    public static QuantileScale<T> Create(ReadOnlySpan<double> samples, ReadOnlySpan<T> range)
    {
        var domain = SortedNumbers.From(samples);
        var n = Math.Max(1, range.Length);
        var thresholds = new double[n - 1];
        for (var i = 1; i < n; i++)
        {
            thresholds[i - 1] = Arrays.Quantiles.OfSorted(domain, (double)i / n);
        }

        return new QuantileScale<T>(domain, range.ToArray(), thresholds, default!);
    }

    /// <summary>Returns this scale with another value for NaN.</summary>
    /// <param name="value">The value NaN maps to.</param>
    /// <returns>The new scale.</returns>
    public QuantileScale<T> WithUnknown(T value) => new(domain, range, thresholds, value);

    /// <summary>Maps a value to the range value of its group.</summary>
    /// <param name="x">The value.</param>
    /// <returns>The range value, or <see cref="Unknown"/> for NaN.</returns>
    public T Map(double x)
    {
        if (double.IsNaN(x))
        {
            return Unknown;
        }

        var i = Bisect.Right(thresholds, x);
        return i < range.Length ? range[i] : Unknown;
    }

    /// <summary>Returns the extent of the values that map to <paramref name="value"/>, as d3's <c>invertExtent</c>.</summary>
    /// <param name="value">A range value; the first equal one counts.</param>
    /// <returns>The bounds, NaN for a value outside the range or an empty domain.</returns>
    public (double Low, double High) InvertExtent(T value)
    {
        var i = Array.IndexOf(range, value);
        if (i < 0)
        {
            return (double.NaN, double.NaN);
        }

        var first = domain.Length > 0 ? domain[0] : double.NaN;
        var last = domain.Length > 0 ? domain[^1] : double.NaN;
        return (i > 0 ? thresholds[i - 1] : first, i < thresholds.Length ? thresholds[i] : last);
    }
}
