namespace Aioros.Graphs.Colors;

/// <summary>
/// A color in any of d3-color's spaces, as d3's <c>color</c> objects: parsed from CSS (RGB, or HSL for <c>hsl()</c>),
/// converted between spaces with d3's rules (a conversion to the same space copies) and formatted as d3 formats it.
/// </summary>
public readonly record struct Color
{
    internal Color(ColorSpace space, double channel0, double channel1, double channel2, double opacity)
    {
        Space = space;
        Channel0 = channel0;
        Channel1 = channel1;
        Channel2 = channel2;
        Opacity = opacity;
    }

    /// <summary>Gets the space the channels are in.</summary>
    public ColorSpace Space { get; }

    /// <summary>Gets the first channel: red, or the hue in HSL, HCL and cubehelix, or the lightness in Lab.</summary>
    public double Channel0 { get; }

    /// <summary>Gets the second channel: green, saturation, a or chroma.</summary>
    public double Channel1 { get; }

    /// <summary>Gets the third channel: blue, lightness or b.</summary>
    public double Channel2 { get; }

    /// <summary>Gets the opacity.</summary>
    public double Opacity { get; }

    /// <summary>Gets a value indicating whether the color is displayable, as <c>displayable()</c>.</summary>
    public bool IsDisplayable => Space == ColorSpace.Hsl ? ToHsl().IsDisplayable : ToRgb().IsDisplayable;

    /// <summary>
    /// Parses a CSS color as d3's <c>color(specifier)</c>: hex with 3, 4, 6 or 8 digits, <c>rgb()</c>, <c>rgba()</c>,
    /// <c>hsl()</c>, <c>hsla()</c>, a named color or <c>transparent</c>, trimmed and in any case.
    /// </summary>
    /// <param name="specifier">The CSS color.</param>
    /// <param name="color">The color, in RGB or HSL.</param>
    /// <returns>Whether the specifier is a color d3 accepts.</returns>
    public static bool TryParse(string? specifier, out Color color) => ColorParser.TryParse(specifier, out color);

    /// <summary>Parses a CSS color; one d3 rejects gives the RGB color whose channels and opacity are NaN, as <c>d3.rgb(specifier)</c>.</summary>
    /// <param name="specifier">The CSS color.</param>
    /// <returns>The color.</returns>
    public static Color Parse(string? specifier) =>
        TryParse(specifier, out var color) ? color : new Color(ColorSpace.Rgb, double.NaN, double.NaN, double.NaN, double.NaN);

    /// <summary>Converts to RGB, as <c>d3.rgb(color)</c>.</summary>
    /// <returns>The RGB color.</returns>
    public Rgb ToRgb() => Space switch
    {
        ColorSpace.Rgb => new Rgb(Channel0, Channel1, Channel2, Opacity),
        ColorSpace.Hsl => new Hsl(Channel0, Channel1, Channel2, Opacity).ToRgb(),
        ColorSpace.Lab => new Lab(Channel0, Channel1, Channel2, Opacity).ToRgb(),
        ColorSpace.Hcl => new Hcl(Channel0, Channel1, Channel2, Opacity).ToRgb(),
        _ => new Cubehelix(Channel0, Channel1, Channel2, Opacity).ToRgb(),
    };

    /// <summary>Converts to HSL, as <c>d3.hsl(color)</c>.</summary>
    /// <returns>The HSL color.</returns>
    public Hsl ToHsl() => Space == ColorSpace.Hsl ? new Hsl(Channel0, Channel1, Channel2, Opacity) : Hsl.From(ToRgb());

    /// <summary>Converts to Lab, as <c>d3.lab(color)</c>.</summary>
    /// <returns>The Lab color.</returns>
    public Lab ToLab() => Space switch
    {
        ColorSpace.Lab => new Lab(Channel0, Channel1, Channel2, Opacity),
        ColorSpace.Hcl => Lab.From(new Hcl(Channel0, Channel1, Channel2, Opacity)),
        _ => Lab.From(ToRgb()),
    };

    /// <summary>Converts to HCL, as <c>d3.hcl(color)</c>.</summary>
    /// <returns>The HCL color.</returns>
    public Hcl ToHcl() => Space == ColorSpace.Hcl ? new Hcl(Channel0, Channel1, Channel2, Opacity) : Hcl.From(ToLab());

    /// <summary>Converts to cubehelix, as <c>d3.cubehelix(color)</c>.</summary>
    /// <returns>The cubehelix color.</returns>
    public Cubehelix ToCubehelix() =>
        Space == ColorSpace.Cubehelix ? new Cubehelix(Channel0, Channel1, Channel2, Opacity) : Cubehelix.From(ToRgb());

    /// <summary>Returns a brighter copy in the same space, as <c>brighter(k)</c>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The brighter color.</returns>
    public Color Brighter(double k = 1) => Space switch
    {
        ColorSpace.Rgb => ToRgb().Brighter(k),
        ColorSpace.Hsl => ToHsl().Brighter(k),
        ColorSpace.Lab => ToLab().Brighter(k),
        ColorSpace.Hcl => ToHcl().Brighter(k),
        _ => ToCubehelix().Brighter(k),
    };

    /// <summary>Returns a darker copy in the same space, as <c>darker(k)</c>.</summary>
    /// <param name="k">The number of steps.</param>
    /// <returns>The darker color.</returns>
    public Color Darker(double k = 1) => Space switch
    {
        ColorSpace.Rgb => ToRgb().Darker(k),
        ColorSpace.Hsl => ToHsl().Darker(k),
        ColorSpace.Lab => ToLab().Darker(k),
        ColorSpace.Hcl => ToHcl().Darker(k),
        _ => ToCubehelix().Darker(k),
    };

    /// <summary>Formats as <c>#rrggbb</c>, as <c>formatHex()</c>.</summary>
    /// <returns>The text.</returns>
    public string FormatHex() => ToRgb().FormatHex();

    /// <summary>Formats as <c>#rrggbbaa</c>, as <c>formatHex8()</c>.</summary>
    /// <returns>The text.</returns>
    public string FormatHex8() => ToRgb().FormatHex8();

    /// <summary>Formats as <c>rgb()</c> or <c>rgba()</c>, as <c>formatRgb()</c>.</summary>
    /// <returns>The text.</returns>
    public string FormatRgb() => ToRgb().FormatRgb();

    /// <summary>Formats as <c>hsl()</c> or <c>hsla()</c>, as <c>formatHsl()</c>.</summary>
    /// <returns>The text.</returns>
    public string FormatHsl() => ToHsl().FormatHsl();

    /// <summary>Formats as <see cref="FormatRgb()"/>, as d3's <c>toString()</c>.</summary>
    /// <returns>The text.</returns>
    public override string ToString() => FormatRgb();
}
