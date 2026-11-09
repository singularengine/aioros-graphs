namespace Aioros.Graphs.Colors;

/// <summary>A color in HSL with opacity, as d3-color's <c>hsl</c>; an achromatic color has a NaN hue.</summary>
/// <param name="H">The hue in degrees.</param>
/// <param name="S">The saturation from 0 to 1.</param>
/// <param name="L">The lightness from 0 to 1.</param>
/// <param name="Opacity">The opacity from 0 to 1.</param>
public readonly record struct Hsl(double H, double S, double L, double Opacity = 1)
{
    /// <summary>Gets a value indicating whether the saturation (or a NaN one), lightness and opacity are within [0, 1], as <c>displayable()</c>.</summary>
    public bool IsDisplayable => ((0 <= S && S <= 1) || double.IsNaN(S)) && 0 <= L && L <= 1 && 0 <= Opacity && Opacity <= 1;

    /// <summary>Converts to the tagged <see cref="Color"/>.</summary>
    /// <param name="color">The color.</param>
    public static implicit operator Color(Hsl color) => new(ColorSpace.Hsl, color.H, color.S, color.L, color.Opacity);

    /// <summary>Converts an RGB color, as <c>d3.hsl(rgb)</c>.</summary>
    /// <param name="color">The RGB color.</param>
    /// <returns>The HSL color.</returns>
    public static Hsl From(Rgb color)
    {
        var r = color.R / 255;
        var g = color.G / 255;
        var b = color.B / 255;
        var min = Math.Min(Math.Min(r, g), b);
        var max = Math.Max(Math.Max(r, g), b);
        var h = double.NaN;
        var s = max - min;
        var l = (max + min) / 2;
        if (s != 0 && !double.IsNaN(s))
        {
            if (r == max)
            {
                h = (g - b) / s + ColorMath.IsTrue(g < b) * 6;
            }
            else if (g == max)
            {
                h = (b - r) / s + 2;
            }
            else
            {
                h = (r - g) / s + 4;
            }

            s /= l < 0.5 ? max + min : 2 - max - min;
            h *= 60;
        }
        else
        {
            s = l > 0 && l < 1 ? 0 : h;
        }

        return new Hsl(h, s, l, color.Opacity);
    }

    /// <summary>Converts to RGB, as <c>hsl.rgb()</c>.</summary>
    /// <returns>The RGB color.</returns>
    public Rgb ToRgb()
    {
        var h = H % 360 + ColorMath.IsTrue(H < 0) * 360;
        var s = double.IsNaN(h) || double.IsNaN(S) ? 0 : S;
        var l = L;
        var m2 = l + (l < 0.5 ? l : 1 - l) * s;
        var m1 = 2 * l - m2;
        return new Rgb(
            Channel(h >= 240 ? h - 240 : h + 120, m1, m2),
            Channel(h, m1, m2),
            Channel(h < 120 ? h + 240 : h - 120, m1, m2),
            Opacity);
    }

    /// <summary>Returns a brighter copy, the lightness multiplied by (1 / 0.7) to the power <paramref name="k"/>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The brighter color.</returns>
    public Hsl Brighter(double k = 1) => this with { L = L * Math.Pow(ColorMath.Brighter, k) };

    /// <summary>Returns a darker copy, the lightness multiplied by 0.7 to the power <paramref name="k"/>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The darker color.</returns>
    public Hsl Darker(double k = 1) => this with { L = L * Math.Pow(ColorMath.Darker, k) };

    /// <summary>Returns the color with its hue within [0, 360) and its saturation, lightness and opacity within [0, 1], as <c>clamp()</c>.</summary>
    /// <returns>The clamped color.</returns>
    public Hsl Clamp() => new(ColorMath.ClampHue(H), ColorMath.ClampUnit(S), ColorMath.ClampUnit(L), ColorMath.ClampOpacity(Opacity));

    /// <summary>Formats as <c>hsl(h, s%, l%)</c>, or <c>hsla(h, s%, l%, a)</c> below full opacity, as <c>formatHsl()</c>.</summary>
    /// <returns>The text.</returns>
    public string FormatHsl() => ColorText.ToText(ColorFormat.Hsl, H, S, L, Opacity);

    /// <summary>Writes <see cref="FormatHsl()"/>'s text without allocating.</summary>
    /// <param name="destination">Where the characters go.</param>
    /// <param name="written">The number of characters written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormatHsl(Span<char> destination, out int written) => ColorText.TryWrite(ColorFormat.Hsl, H, S, L, Opacity, destination, out written);

    /// <summary>Writes <see cref="FormatHsl()"/>'s text as UTF-8 without allocating.</summary>
    /// <param name="destination">Where the bytes go.</param>
    /// <param name="written">The number of bytes written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormatHsl(Span<byte> destination, out int written) => ColorText.TryWrite(ColorFormat.Hsl, H, S, L, Opacity, destination, out written);

    /// <summary>Formats the color in RGB, as d3's <c>toString()</c>.</summary>
    /// <returns>The text.</returns>
    public override string ToString() => ToRgb().FormatRgb();

    private static double Channel(double h, double m1, double m2) =>
        (h < 60 ? m1 + (m2 - m1) * h / 60
        : h < 180 ? m2
        : h < 240 ? m1 + (m2 - m1) * (240 - h) / 60
        : m1) * 255;
}
