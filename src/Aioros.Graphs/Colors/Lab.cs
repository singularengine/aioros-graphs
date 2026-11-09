namespace Aioros.Graphs.Colors;

/// <summary>A CIELAB color with opacity, as d3-color's <c>lab</c>: the D50 white point and sRGB's companding.</summary>
/// <param name="L">The lightness, nominally from 0 to 100.</param>
/// <param name="A">The green–red axis.</param>
/// <param name="B">The blue–yellow axis.</param>
/// <param name="Opacity">The opacity from 0 to 1.</param>
public readonly record struct Lab(double L, double A, double B, double Opacity = 1)
{
    private const double K = 18;
    private const double Xn = 0.96422;
    private const double Yn = 1;
    private const double Zn = 0.82521;
    private const double T0 = 4.0 / 29;
    private const double T1 = 6.0 / 29;
    private const double T2 = 3 * T1 * T1;
    private const double T3 = T1 * T1 * T1;

    /// <summary>Gets a value indicating whether the color is displayable in RGB, as <c>displayable()</c>.</summary>
    public bool IsDisplayable => ToRgb().IsDisplayable;

    /// <summary>Converts to the tagged <see cref="Color"/>.</summary>
    /// <param name="color">The color.</param>
    public static implicit operator Color(Lab color) => new(ColorSpace.Lab, color.L, color.A, color.B, color.Opacity);

    /// <summary>Returns the gray of lightness <paramref name="l"/>, as <c>d3.gray(l, opacity)</c>.</summary>
    /// <param name="l">The lightness.</param>
    /// <param name="opacity">The opacity.</param>
    /// <returns>The color.</returns>
    public static Lab Gray(double l, double opacity = 1) => new(l, 0, 0, opacity);

    /// <summary>Converts an RGB color, as <c>d3.lab(rgb)</c>.</summary>
    /// <param name="color">The RGB color.</param>
    /// <returns>The Lab color.</returns>
    public static Lab From(Rgb color)
    {
        var r = Linear(color.R);
        var g = Linear(color.G);
        var b = Linear(color.B);
        var y = Compress((0.2225045 * r + 0.7168786 * g + 0.0606169 * b) / Yn);
        double x;
        double z;
        if (r == g && g == b)
        {
            x = z = y;
        }
        else
        {
            x = Compress((0.4360747 * r + 0.3850649 * g + 0.1430804 * b) / Xn);
            z = Compress((0.0139322 * r + 0.0971045 * g + 0.7141733 * b) / Zn);
        }

        return new Lab(116 * y - 16, 500 * (x - y), 200 * (y - z), color.Opacity);
    }

    /// <summary>Converts an HCL color, as <c>d3.lab(hcl)</c>: a NaN hue gives a gray.</summary>
    /// <param name="color">The HCL color.</param>
    /// <returns>The Lab color.</returns>
    public static Lab From(Hcl color)
    {
        if (double.IsNaN(color.H))
        {
            return new Lab(color.L, 0, 0, color.Opacity);
        }

        var h = color.H * ColorMath.Radians;
        return new Lab(color.L, Math.Cos(h) * color.C, Math.Sin(h) * color.C, color.Opacity);
    }

    /// <summary>Converts to RGB, as <c>lab.rgb()</c>.</summary>
    /// <returns>The RGB color.</returns>
    public Rgb ToRgb()
    {
        var y = (L + 16) / 116;
        var x = double.IsNaN(A) ? y : y + A / 500;
        var z = double.IsNaN(B) ? y : y - B / 200;
        x = Xn * Expand(x);
        y = Yn * Expand(y);
        z = Zn * Expand(z);
        return new Rgb(
            Gamma(3.1338561 * x - 1.6168667 * y - 0.4906146 * z),
            Gamma(-0.9787684 * x + 1.9161415 * y + 0.0334540 * z),
            Gamma(0.0719453 * x - 0.2289914 * y + 1.4052427 * z),
            Opacity);
    }

    /// <summary>Returns a lighter copy, the lightness raised by 18 <paramref name="k"/>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The brighter color.</returns>
    public Lab Brighter(double k = 1) => this with { L = L + K * k };

    /// <summary>Returns a darker copy, the lightness lowered by 18 <paramref name="k"/>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The darker color.</returns>
    public Lab Darker(double k = 1) => this with { L = L - K * k };

    /// <summary>Formats the color in RGB, as d3's <c>toString()</c>.</summary>
    /// <returns>The text.</returns>
    public override string ToString() => ToRgb().FormatRgb();

    private static double Compress(double t) => t > T3 ? Math.Pow(t, 1.0 / 3) : t / T2 + T0;

    private static double Expand(double t) => t > T1 ? t * t * t : T2 * (t - T0);

    private static double Gamma(double x) => 255 * (x <= 0.0031308 ? 12.92 * x : 1.055 * Math.Pow(x, 1 / 2.4) - 0.055);

    private static double Linear(double channel)
    {
        var x = channel / 255;
        return x <= 0.04045 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
    }
}
