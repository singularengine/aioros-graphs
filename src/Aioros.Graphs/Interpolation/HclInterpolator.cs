using Aioros.Graphs.Colors;

namespace Aioros.Graphs.Interpolation;

/// <summary>
/// Interpolates between two colors in CIELCh, as d3's <c>interpolateHcl(a, b)</c> (the shorter hue arc) or
/// <c>interpolateHclLong(a, b)</c> (<see cref="LongArc"/>), returning the color in RGB.
/// </summary>
public readonly struct HclInterpolator : IInterpolator<Rgb>
{
    private readonly Channel h;
    private readonly Channel c;
    private readonly Channel l;
    private readonly Channel opacity;

    /// <summary>Initializes a new instance of the <see cref="HclInterpolator"/> struct along the shorter hue arc.</summary>
    /// <param name="start">The color at 0.</param>
    /// <param name="end">The color at 1.</param>
    public HclInterpolator(Color start, Color end)
        : this(start.ToHcl(), end.ToHcl(), false)
    {
    }

    private HclInterpolator(Hcl a, Hcl b, bool longPath)
    {
        h = longPath ? Channel.NoGamma(a.H, b.H) : Channel.Hue(a.H, b.H);
        c = Channel.NoGamma(a.C, b.C);
        l = Channel.NoGamma(a.L, b.L);
        opacity = Channel.NoGamma(a.Opacity, b.Opacity);
    }

    /// <summary>Returns the interpolator that does not take the shorter hue arc, as <c>interpolateHclLong(a, b)</c>.</summary>
    /// <param name="start">The color at 0.</param>
    /// <param name="end">The color at 1.</param>
    /// <returns>The interpolator.</returns>
    public static HclInterpolator LongArc(Color start, Color end) => new(start.ToHcl(), end.ToHcl(), true);

    /// <inheritdoc />
    public Rgb Interpolate(double t) => new Hcl(h.At(t), c.At(t), l.At(t), opacity.At(t)).ToRgb();
}
