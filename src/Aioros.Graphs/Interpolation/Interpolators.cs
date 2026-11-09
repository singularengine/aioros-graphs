using Aioros.Graphs.Colors;

namespace Aioros.Graphs.Interpolation;

/// <summary>d3-interpolate's helpers: the generic <c>interpolate</c>, <c>piecewise</c> over numbers and colors and <c>quantize</c>.</summary>
public static class Interpolators
{
    /// <summary>Returns d3's generic <c>interpolate(a, b)</c>, chosen by the kind of <paramref name="b"/>.</summary>
    /// <param name="a">The value at 0.</param>
    /// <param name="b">The value at 1.</param>
    /// <returns>The interpolator.</returns>
    /// <exception cref="ArgumentException">d3 throws: <paramref name="b"/> is an array and <paramref name="a"/> has no length.</exception>
    /// <exception cref="NotSupportedException"><paramref name="b"/> is a string and <paramref name="a"/> holds a date, whose text depends on the host.</exception>
    public static ValueInterpolator Value(InterpolationValue a, InterpolationValue b) => new(a, b);

    /// <summary>Interpolates numbers piecewise, as <c>piecewise(interpolateNumber, values)</c>.</summary>
    /// <param name="values">At least two stops.</param>
    /// <returns>The interpolator.</returns>
    public static PiecewiseInterpolator<NumberInterpolator, double> Piecewise(ReadOnlySpan<double> values)
    {
        var pieces = new NumberInterpolator[Math.Max(0, values.Length - 1)];
        for (var i = 0; i < pieces.Length; i++)
        {
            pieces[i] = new NumberInterpolator(values[i], values[i + 1]);
        }

        return new PiecewiseInterpolator<NumberInterpolator, double>(pieces);
    }

    /// <summary>Interpolates colors piecewise in RGB, as <c>piecewise(interpolateRgb.gamma(gamma), colors)</c>.</summary>
    /// <param name="colors">At least two stops.</param>
    /// <param name="gamma">The gamma of each piece.</param>
    /// <returns>The interpolator.</returns>
    public static PiecewiseInterpolator<RgbInterpolator, Rgb> PiecewiseRgb(ReadOnlySpan<Color> colors, double gamma = 1)
    {
        var pieces = new RgbInterpolator[Math.Max(0, colors.Length - 1)];
        for (var i = 0; i < pieces.Length; i++)
        {
            pieces[i] = new RgbInterpolator(colors[i], colors[i + 1], gamma);
        }

        return new PiecewiseInterpolator<RgbInterpolator, Rgb>(pieces);
    }

    /// <summary>Samples an interpolator uniformly, as <c>quantize(interpolator, n)</c>, without allocating.</summary>
    /// <typeparam name="TInterpolator">The interpolator.</typeparam>
    /// <typeparam name="T">The interpolated value.</typeparam>
    /// <param name="interpolator">The interpolator.</param>
    /// <param name="destination">The samples, at <c>i / (n - 1)</c> for n the length.</param>
    public static void Quantize<TInterpolator, T>(TInterpolator interpolator, Span<T> destination)
        where TInterpolator : IInterpolator<T>
    {
        for (var i = 0; i < destination.Length; i++)
        {
            destination[i] = interpolator.Interpolate(i / (destination.Length - 1.0));
        }
    }

    /// <summary>Samples an interpolator uniformly, as <c>quantize(interpolator, n)</c>.</summary>
    /// <typeparam name="T">The interpolated value.</typeparam>
    /// <param name="interpolator">The interpolator.</param>
    /// <param name="n">The number of samples.</param>
    /// <returns>The samples.</returns>
    public static T[] Quantize<T>(IInterpolator<T> interpolator, int n)
    {
        ArgumentNullException.ThrowIfNull(interpolator);
        var samples = new T[n];
        Quantize(interpolator, samples.AsSpan());
        return samples;
    }
}
