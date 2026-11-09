using Aioros.Graphs.Colors;

namespace Aioros.Graphs.Interpolation;

/// <summary>
/// Interpolates between two colors in HSL, as d3's <c>interpolateHsl(a, b)</c> (the shorter hue arc) or
/// <c>interpolateHslLong(a, b)</c> (<see cref="LongArc"/>), returning the color in RGB.
/// </summary>
public readonly struct HslInterpolator : IInterpolator<Rgb>
{
    private readonly Channel h;
    private readonly Channel s;
    private readonly Channel l;
    private readonly Channel opacity;

    /// <summary>Initializes a new instance of the <see cref="HslInterpolator"/> struct along the shorter hue arc.</summary>
    /// <param name="start">The color at 0.</param>
    /// <param name="end">The color at 1.</param>
    public HslInterpolator(Color start, Color end)
        : this(start.ToHsl(), end.ToHsl(), false)
    {
    }

    private HslInterpolator(Hsl a, Hsl b, bool longPath)
    {
        h = longPath ? Channel.NoGamma(a.H, b.H) : Channel.Hue(a.H, b.H);
        s = Channel.NoGamma(a.S, b.S);
        l = Channel.NoGamma(a.L, b.L);
        opacity = Channel.NoGamma(a.Opacity, b.Opacity);
    }

    /// <summary>Returns the interpolator that does not take the shorter hue arc, as <c>interpolateHslLong(a, b)</c>.</summary>
    /// <param name="start">The color at 0.</param>
    /// <param name="end">The color at 1.</param>
    /// <returns>The interpolator.</returns>
    public static HslInterpolator LongArc(Color start, Color end) => new(start.ToHsl(), end.ToHsl(), true);

    /// <inheritdoc />
    public Rgb Interpolate(double t) => new Hsl(h.At(t), s.At(t), l.At(t), opacity.At(t)).ToRgb();
}
