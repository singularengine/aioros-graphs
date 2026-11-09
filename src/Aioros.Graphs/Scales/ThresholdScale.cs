using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Scales;

/// <summary>
/// A threshold scale, as d3-scale's <c>scaleThreshold</c>: maps values below the first threshold to the first range
/// value, values from each threshold to the next to the next range value. Immutable: <see cref="WithUnknown"/> returns a
/// new scale.
/// </summary>
/// <typeparam name="T">The range value, such as a color.</typeparam>
public sealed class ThresholdScale<T>
{
    private readonly double[] thresholds;
    private readonly T[] range;
    private readonly int n;

    private ThresholdScale(double[] thresholds, T[] range, T unknown)
    {
        this.thresholds = thresholds;
        this.range = range;
        Unknown = unknown;
        n = Math.Min(thresholds.Length, range.Length - 1);
    }

    /// <summary>The thresholds, ascending.</summary>
    public ReadOnlySpan<double> Domain => thresholds;

    /// <summary>The range's values, one more than the thresholds used.</summary>
    public ReadOnlySpan<T> Range => range;

    /// <summary>The value NaN maps to; <c>default</c> unless set with <see cref="WithUnknown"/>.</summary>
    public T Unknown { get; }

    /// <summary>Creates a scale from ascending thresholds to <paramref name="range"/>.</summary>
    /// <param name="thresholds">The thresholds, ascending.</param>
    /// <param name="range">The range's values; d3 uses as many thresholds as there are range values after the first.</param>
    /// <returns>The scale.</returns>
    public static ThresholdScale<T> Create(ReadOnlySpan<double> thresholds, ReadOnlySpan<T> range) =>
        new(thresholds.ToArray(), range.ToArray(), default!);

    /// <summary>Returns this scale with another value for NaN.</summary>
    /// <param name="value">The value NaN maps to.</param>
    /// <returns>The new scale.</returns>
    public ThresholdScale<T> WithUnknown(T value) => new(thresholds, range, value);

    /// <summary>Maps a value to the range value of its interval.</summary>
    /// <param name="x">The value.</param>
    /// <returns>The range value, or <see cref="Unknown"/> for NaN or an empty range.</returns>
    public T Map(double x)
    {
        if (double.IsNaN(x))
        {
            return Unknown;
        }

        var i = Bisect.Right(thresholds, x, 0, Math.Max(n, 0));
        return i < range.Length ? range[i] : Unknown;
    }

    /// <summary>Returns the thresholds around the values that map to <paramref name="value"/>, as d3's <c>invertExtent</c>.</summary>
    /// <param name="value">A range value; the first equal one counts.</param>
    /// <returns>The bounds, NaN where d3's are undefined (below the first and above the last threshold).</returns>
    public (double Low, double High) InvertExtent(T value)
    {
        var i = Array.IndexOf(range, value);
        return (At(i - 1), At(i));
    }

    private double At(int i) => i >= 0 && i < thresholds.Length ? thresholds[i] : double.NaN;
}
