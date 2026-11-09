using Aioros.Graphs.Arrays;
using Aioros.Graphs.Interpolation;

namespace Aioros.Graphs.Scales;

/// <summary>
/// A sequential quantile scale, as d3-scale's <c>scaleSequentialQuantile</c>: maps a value to its rank among the
/// domain's sorted values, scaled to [0, 1], then through an interpolator. Immutable.
/// </summary>
/// <typeparam name="T">The interpolated value, such as a color.</typeparam>
/// <typeparam name="TInterpolator">The interpolator.</typeparam>
public sealed class SequentialQuantileScale<T, TInterpolator>
    where TInterpolator : struct, IInterpolator<T>
{
    private readonly double[] domain;
    private readonly TInterpolator interpolator;

    private SequentialQuantileScale(double[] domain, TInterpolator interpolator, T unknown)
    {
        this.domain = domain;
        this.interpolator = interpolator;
        Unknown = unknown;
    }

    /// <summary>The domain's values, sorted ascending, NaN removed.</summary>
    public ReadOnlySpan<double> Domain => domain;

    /// <summary>The value NaN maps to; <c>default</c> unless set with <see cref="WithUnknown"/>.</summary>
    public T Unknown { get; }

    /// <summary>Creates a scale from sample values.</summary>
    /// <param name="values">The values, in any order; NaN values are dropped.</param>
    /// <param name="interpolator">The interpolator.</param>
    /// <returns>The scale.</returns>
    public static SequentialQuantileScale<T, TInterpolator> Create(ReadOnlySpan<double> values, TInterpolator interpolator) =>
        new(SortedNumbers.From(values), interpolator, default!);

    /// <summary>Returns this scale with another value for NaN.</summary>
    /// <param name="value">The value NaN maps to.</param>
    /// <returns>The new scale.</returns>
    public SequentialQuantileScale<T, TInterpolator> WithUnknown(T value) => new(domain, interpolator, value);

    /// <summary>Maps a value through the interpolator at its rank.</summary>
    /// <param name="x">The value.</param>
    /// <returns>The interpolated value, or <see cref="Unknown"/> for NaN.</returns>
    public T Map(double x) =>
        double.IsNaN(x) ? Unknown : interpolator.Interpolate((double)(Bisect.Right(domain, x, 1, domain.Length) - 1) / (domain.Length - 1));

    /// <summary>Returns the interpolated value of each domain value's rank, as d3's <c>scale.range()</c>.</summary>
    /// <returns>The values.</returns>
    public T[] Range()
    {
        var range = new T[domain.Length];
        for (var i = 0; i < range.Length; i++)
        {
            range[i] = interpolator.Interpolate((double)i / (domain.Length - 1));
        }

        return range;
    }

    /// <summary>Returns the <paramref name="n"/> + 1 quantiles that split the domain into <paramref name="n"/> groups, as d3's <c>scale.quantiles(n)</c>.</summary>
    /// <param name="n">The number of groups.</param>
    /// <returns>The quantiles, from the minimum to the maximum.</returns>
    public double[] Quantiles(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        var quantiles = new double[n + 1];
        for (var i = 0; i <= n; i++)
        {
            quantiles[i] = Arrays.Quantiles.Of(domain, (double)i / n);
        }

        return quantiles;
    }
}
