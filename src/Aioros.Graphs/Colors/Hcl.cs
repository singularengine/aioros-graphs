namespace Aioros.Graphs.Colors;

/// <summary>A CIELCh color with opacity, as d3-color's <c>hcl</c> (and <c>lch</c>); an achromatic color has a NaN hue.</summary>
/// <param name="H">The hue in degrees.</param>
/// <param name="C">The chroma.</param>
/// <param name="L">The lightness, nominally from 0 to 100.</param>
/// <param name="Opacity">The opacity from 0 to 1.</param>
public readonly record struct Hcl(double H, double C, double L, double Opacity = 1)
{
    private const double K = 18;

    /// <summary>Gets a value indicating whether the color is displayable in RGB, as <c>displayable()</c>.</summary>
    public bool IsDisplayable => ToRgb().IsDisplayable;

    /// <summary>Converts to the tagged <see cref="Color"/>.</summary>
    /// <param name="color">The color.</param>
    public static implicit operator Color(Hcl color) => new(ColorSpace.Hcl, color.H, color.C, color.L, color.Opacity);

    /// <summary>Creates a color from its lightness, chroma and hue, as <c>d3.lch(l, c, h, opacity)</c>.</summary>
    /// <param name="l">The lightness.</param>
    /// <param name="c">The chroma.</param>
    /// <param name="h">The hue in degrees.</param>
    /// <param name="opacity">The opacity.</param>
    /// <returns>The color.</returns>
    public static Hcl Lch(double l, double c, double h, double opacity = 1) => new(h, c, l, opacity);

    /// <summary>Converts a Lab color, as <c>d3.hcl(lab)</c>: a gray has a NaN hue.</summary>
    /// <param name="color">The Lab color.</param>
    /// <returns>The HCL color.</returns>
    public static Hcl From(Lab color)
    {
        if (color.A == 0 && color.B == 0)
        {
            return new Hcl(double.NaN, 0 < color.L && color.L < 100 ? 0 : double.NaN, color.L, color.Opacity);
        }

        var h = Math.Atan2(color.B, color.A) * ColorMath.Degrees;
        return new Hcl(h < 0 ? h + 360 : h, Math.Sqrt(color.A * color.A + color.B * color.B), color.L, color.Opacity);
    }

    /// <summary>Converts to RGB through Lab, as <c>hcl.rgb()</c>.</summary>
    /// <returns>The RGB color.</returns>
    public Rgb ToRgb() => Lab.From(this).ToRgb();

    /// <summary>Returns a lighter copy, the lightness raised by 18 <paramref name="k"/>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The brighter color.</returns>
    public Hcl Brighter(double k = 1) => this with { L = L + K * k };

    /// <summary>Returns a darker copy, the lightness lowered by 18 <paramref name="k"/>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The darker color.</returns>
    public Hcl Darker(double k = 1) => this with { L = L - K * k };

    /// <summary>Formats the color in RGB, as d3's <c>toString()</c>.</summary>
    /// <returns>The text.</returns>
    public override string ToString() => ToRgb().FormatRgb();
}
