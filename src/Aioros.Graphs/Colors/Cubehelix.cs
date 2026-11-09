namespace Aioros.Graphs.Colors;

/// <summary>A color in Dave Green's cubehelix space with opacity, as d3-color's <c>cubehelix</c>.</summary>
/// <param name="H">The hue in degrees.</param>
/// <param name="S">The saturation.</param>
/// <param name="L">The lightness from 0 to 1.</param>
/// <param name="Opacity">The opacity from 0 to 1.</param>
public readonly record struct Cubehelix(double H, double S, double L, double Opacity = 1)
{
    private const double A = -0.14861;
    private const double B = +1.78277;
    private const double C = -0.29227;
    private const double D = -0.90649;
    private const double E = +1.97294;
    private const double ED = E * D;
    private const double EB = E * B;
    private const double BCDA = B * C - D * A;

    /// <summary>Gets a value indicating whether the color is displayable in RGB, as <c>displayable()</c>.</summary>
    public bool IsDisplayable => ToRgb().IsDisplayable;

    /// <summary>Converts to the tagged <see cref="Color"/>.</summary>
    /// <param name="color">The color.</param>
    public static implicit operator Color(Cubehelix color) => new(ColorSpace.Cubehelix, color.H, color.S, color.L, color.Opacity);

    /// <summary>Converts an RGB color, as <c>d3.cubehelix(rgb)</c>.</summary>
    /// <param name="color">The RGB color.</param>
    /// <returns>The cubehelix color.</returns>
    public static Cubehelix From(Rgb color)
    {
        var r = color.R / 255;
        var g = color.G / 255;
        var b = color.B / 255;
        var l = (BCDA * b + ED * r - EB * g) / (BCDA + ED - EB);
        var bl = b - l;
        var k = (E * (g - l) - C * bl) / D;
        var s = Math.Sqrt(k * k + bl * bl) / (E * l * (1 - l));
        var h = s != 0 && !double.IsNaN(s) ? Math.Atan2(k, bl) * ColorMath.Degrees - 120 : double.NaN;
        return new Cubehelix(h < 0 ? h + 360 : h, s, l, color.Opacity);
    }

    /// <summary>Converts to RGB, as <c>cubehelix.rgb()</c>.</summary>
    /// <returns>The RGB color.</returns>
    public Rgb ToRgb()
    {
        var h = double.IsNaN(H) ? 0 : (H + 120) * ColorMath.Radians;
        var l = L;
        var a = double.IsNaN(S) ? 0 : S * l * (1 - l);
        var cosh = Math.Cos(h);
        var sinh = Math.Sin(h);
        return new Rgb(
            255 * (l + a * (A * cosh + B * sinh)),
            255 * (l + a * (C * cosh + D * sinh)),
            255 * (l + a * (E * cosh)),
            Opacity);
    }

    /// <summary>Returns a brighter copy, the lightness multiplied by (1 / 0.7) to the power <paramref name="k"/>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The brighter color.</returns>
    public Cubehelix Brighter(double k = 1) => this with { L = L * Math.Pow(ColorMath.Brighter, k) };

    /// <summary>Returns a darker copy, the lightness multiplied by 0.7 to the power <paramref name="k"/>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The darker color.</returns>
    public Cubehelix Darker(double k = 1) => this with { L = L * Math.Pow(ColorMath.Darker, k) };

    /// <summary>Formats the color in RGB, as d3's <c>toString()</c>.</summary>
    /// <returns>The text.</returns>
    public override string ToString() => ToRgb().FormatRgb();
}
