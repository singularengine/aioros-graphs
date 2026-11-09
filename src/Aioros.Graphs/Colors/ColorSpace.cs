namespace Aioros.Graphs.Colors;

/// <summary>The color spaces of d3-color.</summary>
public enum ColorSpace
{
    /// <summary>sRGB: red, green and blue from 0 to 255 (<c>d3.rgb</c>).</summary>
    Rgb,

    /// <summary>Hue in degrees, saturation and lightness from 0 to 1 (<c>d3.hsl</c>).</summary>
    Hsl,

    /// <summary>CIELAB with the D50 white point: lightness, a and b (<c>d3.lab</c>).</summary>
    Lab,

    /// <summary>CIELCh: hue in degrees, chroma and lightness (<c>d3.hcl</c>).</summary>
    Hcl,

    /// <summary>Dave Green's cubehelix: hue in degrees, saturation and lightness (<c>d3.cubehelix</c>).</summary>
    Cubehelix,
}
