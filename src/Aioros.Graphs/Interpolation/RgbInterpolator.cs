using Aioros.Graphs.Colors;

namespace Aioros.Graphs.Interpolation;

/// <summary>
/// Interpolates between two colors in RGB, as d3's <c>interpolateRgb.gamma(gamma)(a, b)</c>: each channel with the
/// gamma, the opacity linearly, a NaN channel taking the other end's.
/// </summary>
public readonly struct RgbInterpolator : IInterpolator<Rgb>
{
    private readonly Channel r;
    private readonly Channel g;
    private readonly Channel b;
    private readonly Channel opacity;

    /// <summary>Initializes a new instance of the <see cref="RgbInterpolator"/> struct.</summary>
    /// <param name="start">The color at 0.</param>
    /// <param name="end">The color at 1.</param>
    /// <param name="gamma">The gamma; 1 interpolates linearly.</param>
    public RgbInterpolator(Color start, Color end, double gamma = 1)
    {
        var (s, e) = (start.ToRgb(), end.ToRgb());
        r = Channel.Gamma(s.R, e.R, gamma);
        g = Channel.Gamma(s.G, e.G, gamma);
        b = Channel.Gamma(s.B, e.B, gamma);
        opacity = Channel.NoGamma(s.Opacity, e.Opacity);
    }

    /// <inheritdoc />
    public Rgb Interpolate(double t) => new(r.At(t), g.At(t), b.At(t), opacity.At(t));
}
