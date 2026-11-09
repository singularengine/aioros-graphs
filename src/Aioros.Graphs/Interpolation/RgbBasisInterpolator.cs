using Aioros.Graphs.Colors;

namespace Aioros.Graphs.Interpolation;

/// <summary>
/// A B-spline through colors in RGB, as d3's <c>interpolateRgbBasis(colors)</c>, or a closed one, as
/// <c>interpolateRgbBasisClosed(colors)</c>, from <see cref="Closed"/>: NaN channels count as 0 and the opacity is 1.
/// </summary>
public readonly struct RgbBasisInterpolator : IInterpolator<Rgb>
{
    private readonly double[] r;
    private readonly double[] g;
    private readonly double[] b;
    private readonly bool closed;

    /// <summary>Initializes a new instance of the <see cref="RgbBasisInterpolator"/> struct: an open spline through the first and last colors.</summary>
    /// <param name="colors">The control colors, at least one.</param>
    public RgbBasisInterpolator(ReadOnlySpan<Color> colors)
        : this(colors, false)
    {
    }

    private RgbBasisInterpolator(ReadOnlySpan<Color> colors, bool closed)
    {
        if (colors.IsEmpty)
        {
            throw new ArgumentException("A spline needs at least one color.", nameof(colors));
        }

        this.closed = closed;
        r = new double[colors.Length];
        g = new double[colors.Length];
        b = new double[colors.Length];
        for (var i = 0; i < colors.Length; i++)
        {
            var color = colors[i].ToRgb();
            r[i] = OrZero(color.R);
            g[i] = OrZero(color.G);
            b[i] = OrZero(color.B);
        }
    }

    /// <summary>Returns a closed spline, as <c>interpolateRgbBasisClosed(colors)</c>.</summary>
    /// <param name="colors">The control colors, at least one.</param>
    /// <returns>The interpolator.</returns>
    public static RgbBasisInterpolator Closed(ReadOnlySpan<Color> colors) => new(colors, true);

    /// <inheritdoc />
    public Rgb Interpolate(double t) => closed
        ? new Rgb(BasisInterpolator.ClosedAt(r, t), BasisInterpolator.ClosedAt(g, t), BasisInterpolator.ClosedAt(b, t))
        : new Rgb(BasisInterpolator.OpenAt(r, t), BasisInterpolator.OpenAt(g, t), BasisInterpolator.OpenAt(b, t));

    private static double OrZero(double value) => double.IsNaN(value) || value == 0 ? 0 : value;
}
