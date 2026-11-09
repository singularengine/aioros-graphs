using Aioros.Graphs.Arrays;
using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Scales;

/// <summary>
/// A quantize scale, as d3-scale's <c>scaleQuantize</c>: divides a continuous domain into uniform segments, one per
/// range value. Immutable: <see cref="WithUnknown"/> and <see cref="Nice"/> return new scales.
/// </summary>
/// <typeparam name="T">The range value, such as a color.</typeparam>
public sealed class QuantizeScale<T>
{
    private readonly double x0;
    private readonly double x1;
    private readonly T[] range;
    private readonly double[] thresholds;

    private QuantizeScale(double x0, double x1, T[] range, T unknown)
    {
        this.x0 = x0;
        this.x1 = x1;
        this.range = range;
        Unknown = unknown;
        var n = range.Length - 1;
        thresholds = new double[n];
        for (var i = 0; i < n; i++)
        {
            thresholds[i] = ((i + 1) * x1 - (i - n) * x0) / (n + 1);
        }
    }

    /// <summary>The domain's bounds.</summary>
    public (double Start, double End) Domain => (x0, x1);

    /// <summary>The range's values.</summary>
    public ReadOnlySpan<T> Range => range;

    /// <summary>The boundaries between the segments, as d3's <c>scale.thresholds()</c>.</summary>
    public ReadOnlySpan<double> Thresholds => thresholds;

    /// <summary>The value NaN maps to; <c>default</c> unless set with <see cref="WithUnknown"/>.</summary>
    public T Unknown { get; }

    /// <summary>Creates a scale from the domain [<paramref name="start"/>, <paramref name="end"/>] to <paramref name="range"/>.</summary>
    /// <param name="start">The domain's start.</param>
    /// <param name="end">The domain's end.</param>
    /// <param name="range">At least one value.</param>
    /// <returns>The scale.</returns>
    /// <exception cref="ArgumentException">The range is empty.</exception>
    public static QuantizeScale<T> Create(double start, double end, ReadOnlySpan<T> range)
    {
        if (range.IsEmpty)
        {
            throw new ArgumentException("A quantize scale needs at least one range value.", nameof(range));
        }

        return new QuantizeScale<T>(start, end, range.ToArray(), default!);
    }

    /// <summary>Returns this scale with another value for NaN.</summary>
    /// <param name="value">The value NaN maps to.</param>
    /// <returns>The new scale.</returns>
    public QuantizeScale<T> WithUnknown(T value) => new(x0, x1, range, value);

    /// <summary>Maps a value to the range value of its segment.</summary>
    /// <param name="x">The value.</param>
    /// <returns>The range value, or <see cref="Unknown"/> for NaN.</returns>
    public T Map(double x) => double.IsNaN(x) ? Unknown : range[Bisect.Right(thresholds, x, 0, thresholds.Length)];

    /// <summary>Returns the segment of the domain that maps to <paramref name="value"/>, as d3's <c>invertExtent</c>.</summary>
    /// <param name="value">A range value; the first equal one counts.</param>
    /// <returns>The segment's bounds, NaN for a value outside the range.</returns>
    public (double Low, double High) InvertExtent(T value)
    {
        var i = Array.IndexOf(range, value);
        var n = thresholds.Length;
        if (i < 0)
        {
            return (double.NaN, double.NaN);
        }

        if (i < 1)
        {
            return (x0, n > 0 ? thresholds[0] : double.NaN);
        }

        return i >= n ? (thresholds[n - 1], x1) : (thresholds[i - 1], thresholds[i]);
    }

    /// <summary>Returns about <paramref name="count"/> nicely-rounded values within the domain, as d3's <c>scale.ticks(count)</c>.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <returns>The ticks.</returns>
    public double[] Ticks(double count = 10) => Arrays.Ticks.Compute(x0, x1, count);

    /// <summary>Returns the format for this scale's ticks, as d3's <c>scale.tickFormat(count, specifier)</c>.</summary>
    /// <param name="count">The tick count the ticks were made for.</param>
    /// <param name="specifier">A d3-format specifier, by default <c>,f</c> with the precision the tick step needs.</param>
    /// <returns>The format.</returns>
    public NumberFormat TickFormat(double count = 10, string? specifier = null) => Formatting.TickFormat.Create(x0, x1, count, specifier);

    /// <summary>Returns this scale with its domain extended to nicely-rounded values, as d3's <c>nice</c>.</summary>
    /// <param name="count">The approximate number of ticks the nice domain is built for.</param>
    /// <returns>The new scale.</returns>
    public QuantizeScale<T> Nice(double count = 10) =>
        LinearNice.Stops([x0, x1], count) is { } stops ? new QuantizeScale<T>(stops[0], stops[1], range, Unknown) : this;
}
