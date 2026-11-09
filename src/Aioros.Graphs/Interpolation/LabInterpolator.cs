using Aioros.Graphs.Colors;

namespace Aioros.Graphs.Interpolation;

/// <summary>Interpolates between two colors in CIELAB, as d3's <c>interpolateLab(a, b)</c>, returning the color in RGB.</summary>
public readonly struct LabInterpolator : IInterpolator<Rgb>
{
    private readonly Channel l;
    private readonly Channel a;
    private readonly Channel b;
    private readonly Channel opacity;

    /// <summary>Initializes a new instance of the <see cref="LabInterpolator"/> struct.</summary>
    /// <param name="start">The color at 0.</param>
    /// <param name="end">The color at 1.</param>
    public LabInterpolator(Color start, Color end)
    {
        var (s, e) = (start.ToLab(), end.ToLab());
        l = Channel.NoGamma(s.L, e.L);
        a = Channel.NoGamma(s.A, e.A);
        b = Channel.NoGamma(s.B, e.B);
        opacity = Channel.NoGamma(s.Opacity, e.Opacity);
    }

    /// <inheritdoc />
    public Rgb Interpolate(double t) => new Lab(l.At(t), a.At(t), b.At(t), opacity.At(t)).ToRgb();
}
