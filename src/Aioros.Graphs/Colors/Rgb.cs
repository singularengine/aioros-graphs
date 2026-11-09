namespace Aioros.Graphs.Colors;

/// <summary>An sRGB color with opacity, as d3-color's <c>rgb</c>: channels are not clamped until formatted.</summary>
/// <param name="R">The red channel, nominally from 0 to 255.</param>
/// <param name="G">The green channel, nominally from 0 to 255.</param>
/// <param name="B">The blue channel, nominally from 0 to 255.</param>
/// <param name="Opacity">The opacity from 0 to 1.</param>
public readonly record struct Rgb(double R, double G, double B, double Opacity = 1)
{
    /// <summary>Gets a value indicating whether the channels are within [0, 255] once rounded and the opacity within [0, 1], as <c>displayable()</c>.</summary>
    public bool IsDisplayable =>
        -0.5 <= R && R < 255.5 && -0.5 <= G && G < 255.5 && -0.5 <= B && B < 255.5 && 0 <= Opacity && Opacity <= 1;

    /// <summary>Parses a CSS color as <c>d3.rgb(specifier)</c>: a color that is not valid gives NaN channels and opacity.</summary>
    /// <param name="specifier">The CSS color.</param>
    /// <returns>The color in RGB.</returns>
    public static Rgb Parse(string? specifier) => Color.Parse(specifier).ToRgb();

    /// <summary>Converts to the tagged <see cref="Color"/>.</summary>
    /// <param name="color">The color.</param>
    public static implicit operator Color(Rgb color) => new(ColorSpace.Rgb, color.R, color.G, color.B, color.Opacity);

    /// <summary>Returns a brighter copy, each channel multiplied by (1 / 0.7) to the power <paramref name="k"/>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The brighter color.</returns>
    public Rgb Brighter(double k = 1)
    {
        var f = Math.Pow(ColorMath.Brighter, k);
        return new Rgb(R * f, G * f, B * f, Opacity);
    }

    /// <summary>Returns a darker copy, each channel multiplied by 0.7 to the power <paramref name="k"/>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The darker color.</returns>
    public Rgb Darker(double k = 1)
    {
        var f = Math.Pow(ColorMath.Darker, k);
        return new Rgb(R * f, G * f, B * f, Opacity);
    }

    /// <summary>Returns the color with rounded channels within [0, 255] and an opacity within [0, 1], as <c>clamp()</c>.</summary>
    /// <returns>The clamped color.</returns>
    public Rgb Clamp() =>
        new(ColorMath.ClampChannel(R), ColorMath.ClampChannel(G), ColorMath.ClampChannel(B), ColorMath.ClampOpacity(Opacity));

    /// <summary>Formats as <c>#rrggbb</c>, as <c>formatHex()</c>.</summary>
    /// <returns>The text.</returns>
    public string FormatHex() => ColorText.ToText(ColorFormat.Hex, R, G, B, Opacity);

    /// <summary>Formats as <c>#rrggbbaa</c>, as <c>formatHex8()</c>.</summary>
    /// <returns>The text.</returns>
    public string FormatHex8() => ColorText.ToText(ColorFormat.Hex8, R, G, B, Opacity);

    /// <summary>Formats as <c>rgb(r, g, b)</c>, or <c>rgba(r, g, b, a)</c> below full opacity, as <c>formatRgb()</c>.</summary>
    /// <returns>The text.</returns>
    public string FormatRgb() => ColorText.ToText(ColorFormat.Rgb, R, G, B, Opacity);

    /// <summary>Formats in HSL, as <c>formatHsl()</c>.</summary>
    /// <returns>The text.</returns>
    public string FormatHsl() => Hsl.From(this).FormatHsl();

    /// <summary>Formats as <see cref="FormatRgb()"/>, as d3's <c>toString()</c>.</summary>
    /// <returns>The text.</returns>
    public override string ToString() => FormatRgb();

    /// <summary>Writes <see cref="FormatHex()"/>'s text without allocating.</summary>
    /// <param name="destination">Where the characters go.</param>
    /// <param name="written">The number of characters written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormatHex(Span<char> destination, out int written) => ColorText.TryWriteHex(R, G, B, destination, out written);

    /// <summary>Writes <see cref="FormatHex()"/>'s text as UTF-8 without allocating.</summary>
    /// <param name="destination">Where the bytes go.</param>
    /// <param name="written">The number of bytes written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormatHex(Span<byte> destination, out int written) => ColorText.TryWriteHex(R, G, B, destination, out written);

    /// <summary>Writes <see cref="FormatHex8()"/>'s text without allocating.</summary>
    /// <param name="destination">Where the characters go.</param>
    /// <param name="written">The number of characters written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormatHex8(Span<char> destination, out int written) => ColorText.TryWrite(ColorFormat.Hex8, R, G, B, Opacity, destination, out written);

    /// <summary>Writes <see cref="FormatHex8()"/>'s text as UTF-8 without allocating.</summary>
    /// <param name="destination">Where the bytes go.</param>
    /// <param name="written">The number of bytes written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormatHex8(Span<byte> destination, out int written) => ColorText.TryWrite(ColorFormat.Hex8, R, G, B, Opacity, destination, out written);

    /// <summary>Writes <see cref="FormatRgb()"/>'s text without allocating.</summary>
    /// <param name="destination">Where the characters go.</param>
    /// <param name="written">The number of characters written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormatRgb(Span<char> destination, out int written) => ColorText.TryWrite(ColorFormat.Rgb, R, G, B, Opacity, destination, out written);

    /// <summary>Writes <see cref="FormatRgb()"/>'s text as UTF-8 without allocating.</summary>
    /// <param name="destination">Where the bytes go.</param>
    /// <param name="written">The number of bytes written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormatRgb(Span<byte> destination, out int written) => ColorText.TryWrite(ColorFormat.Rgb, R, G, B, Opacity, destination, out written);
}
