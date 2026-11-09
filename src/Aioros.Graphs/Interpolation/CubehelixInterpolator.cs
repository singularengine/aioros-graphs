using Aioros.Graphs.Colors;

namespace Aioros.Graphs.Interpolation;

/// <summary>
/// Interpolates between two colors in cubehelix, as d3's <c>interpolateCubehelix.gamma(gamma)(a, b)</c> (the shorter
/// hue arc) or <c>interpolateCubehelixLong.gamma(gamma)(a, b)</c> (<see cref="LongArc"/>): the lightness at t to the power
/// gamma; the color is returned in RGB.
/// </summary>
public readonly struct CubehelixInterpolator : IInterpolator<Rgb>
{
    private readonly Channel h;
    private readonly Channel s;
    private readonly Channel l;
    private readonly Channel opacity;
    private readonly double gamma;

    /// <summary>Initializes a new instance of the <see cref="CubehelixInterpolator"/> struct along the shorter hue arc.</summary>
    /// <param name="start">The color at 0.</param>
    /// <param name="end">The color at 1.</param>
    /// <param name="gamma">The gamma applied to the lightness.</param>
    public CubehelixInterpolator(Color start, Color end, double gamma = 1)
        : this(start.ToCubehelix(), end.ToCubehelix(), gamma, false)
    {
    }

    private CubehelixInterpolator(Cubehelix a, Cubehelix b, double gamma, bool longPath)
    {
        h = longPath ? Channel.NoGamma(a.H, b.H) : Channel.Hue(a.H, b.H);
        s = Channel.NoGamma(a.S, b.S);
        l = Channel.NoGamma(a.L, b.L);
        opacity = Channel.NoGamma(a.Opacity, b.Opacity);
        this.gamma = gamma;
    }

    /// <summary>Returns the interpolator that does not take the shorter hue arc, as <c>interpolateCubehelixLong.gamma(gamma)(a, b)</c>.</summary>
    /// <param name="start">The color at 0.</param>
    /// <param name="end">The color at 1.</param>
    /// <param name="gamma">The gamma applied to the lightness.</param>
    /// <returns>The interpolator.</returns>
    public static CubehelixInterpolator LongArc(Color start, Color end, double gamma = 1) => new(start.ToCubehelix(), end.ToCubehelix(), gamma, true);

    /// <inheritdoc />
    public Rgb Interpolate(double t) => new Cubehelix(h.At(t), s.At(t), l.At(Math.Pow(t, gamma)), opacity.At(t)).ToRgb();
}
