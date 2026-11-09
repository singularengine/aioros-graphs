using System.Collections.Immutable;
using System.Globalization;

namespace Aioros.Graphs.Colors;

/// <summary>
/// d3-scale-chromatic's color schemes: the categorical ones as hex colors, the ColorBrewer ones by size and the
/// continuous ones as <see cref="SchemeInterpolator"/>s (the ColorBrewer interpolators are RGB B-splines through each
/// scheme's largest size, as d3 builds them).
/// </summary>
public static class ColorSchemes
{
    private static readonly ImmutableArray<string>[][] Brewers = BuildBrewers();
    private static readonly SchemeInterpolator[] BrewerInterpolators = BuildBrewerInterpolators();

    /// <summary>Gets d3's <c>schemeCategory10</c>.</summary>
    public static ImmutableArray<string> Category10 { get; } = Colors(SchemeData.Category10);

    /// <summary>Gets d3's <c>schemeAccent</c>.</summary>
    public static ImmutableArray<string> Accent { get; } = Colors(SchemeData.Accent);

    /// <summary>Gets d3's <c>schemeDark2</c>.</summary>
    public static ImmutableArray<string> Dark2 { get; } = Colors(SchemeData.Dark2);

    /// <summary>Gets d3's <c>schemeObservable10</c>.</summary>
    public static ImmutableArray<string> Observable10 { get; } = Colors(SchemeData.Observable10);

    /// <summary>Gets d3's <c>schemePaired</c>.</summary>
    public static ImmutableArray<string> Paired { get; } = Colors(SchemeData.Paired);

    /// <summary>Gets d3's <c>schemePastel1</c>.</summary>
    public static ImmutableArray<string> Pastel1 { get; } = Colors(SchemeData.Pastel1);

    /// <summary>Gets d3's <c>schemePastel2</c>.</summary>
    public static ImmutableArray<string> Pastel2 { get; } = Colors(SchemeData.Pastel2);

    /// <summary>Gets d3's <c>schemeSet1</c>.</summary>
    public static ImmutableArray<string> Set1 { get; } = Colors(SchemeData.Set1);

    /// <summary>Gets d3's <c>schemeSet2</c>.</summary>
    public static ImmutableArray<string> Set2 { get; } = Colors(SchemeData.Set2);

    /// <summary>Gets d3's <c>schemeSet3</c>.</summary>
    public static ImmutableArray<string> Set3 { get; } = Colors(SchemeData.Set3);

    /// <summary>Gets d3's <c>schemeTableau10</c>.</summary>
    public static ImmutableArray<string> Tableau10 { get; } = Colors(SchemeData.Tableau10);

    /// <summary>Gets d3's <c>interpolateViridis</c>.</summary>
    public static SchemeInterpolator Viridis { get; } = SchemeInterpolator.FromRamp(Table(SchemeData.Viridis));

    /// <summary>Gets d3's <c>interpolateMagma</c>.</summary>
    public static SchemeInterpolator Magma { get; } = SchemeInterpolator.FromRamp(Table(SchemeData.Magma));

    /// <summary>Gets d3's <c>interpolateInferno</c>.</summary>
    public static SchemeInterpolator Inferno { get; } = SchemeInterpolator.FromRamp(Table(SchemeData.Inferno));

    /// <summary>Gets d3's <c>interpolatePlasma</c>.</summary>
    public static SchemeInterpolator Plasma { get; } = SchemeInterpolator.FromRamp(Table(SchemeData.Plasma));

    /// <summary>Gets d3's <c>interpolateCividis</c>.</summary>
    public static SchemeInterpolator Cividis { get; } = SchemeInterpolator.Cividis();

    /// <summary>Gets d3's <c>interpolateTurbo</c>.</summary>
    public static SchemeInterpolator Turbo { get; } = SchemeInterpolator.Turbo();

    /// <summary>Gets d3's <c>interpolateWarm</c>.</summary>
    public static SchemeInterpolator Warm { get; } = SchemeInterpolator.FromCubehelix(new Cubehelix(-100, 0.75, 0.35), new Cubehelix(80, 1.50, 0.8));

    /// <summary>Gets d3's <c>interpolateCool</c>.</summary>
    public static SchemeInterpolator Cool { get; } = SchemeInterpolator.FromCubehelix(new Cubehelix(260, 0.75, 0.35), new Cubehelix(80, 1.50, 0.8));

    /// <summary>Gets d3's <c>interpolateRainbow</c>, which repeats outside [0, 1].</summary>
    public static SchemeInterpolator Rainbow { get; } = SchemeInterpolator.Rainbow();

    /// <summary>Gets d3's <c>interpolateSinebow</c>, which repeats outside [0, 1].</summary>
    public static SchemeInterpolator Sinebow { get; } = SchemeInterpolator.Sinebow();

    /// <summary>Gets d3's <c>interpolateCubehelixDefault</c>.</summary>
    public static SchemeInterpolator CubehelixDefault { get; } = SchemeInterpolator.FromCubehelix(new Cubehelix(300, 0.5, 0.0), new Cubehelix(-240, 0.5, 1.0));

    /// <summary>Returns a ColorBrewer scheme of <paramref name="k"/> colors, as d3's <c>scheme*[k]</c>.</summary>
    /// <param name="scheme">The scheme.</param>
    /// <param name="k">The number of colors: 3 to 9, or to 11 for the diverging schemes.</param>
    /// <returns>The colors, empty for a size the scheme does not have.</returns>
    public static ImmutableArray<string> Scheme(BrewerScheme scheme, int k)
    {
        var sizes = Brewers[(int)scheme];
        return k >= 3 && k - 3 < sizes.Length ? sizes[k - 3] : [];
    }

    /// <summary>Returns a ColorBrewer scheme's interpolator, as d3's <c>interpolate*</c>.</summary>
    /// <param name="scheme">The scheme.</param>
    /// <returns>The interpolator.</returns>
    public static SchemeInterpolator Interpolator(BrewerScheme scheme) => BrewerInterpolators[(int)scheme];

    private static ImmutableArray<string>[][] BuildBrewers()
    {
        var schemes = new ImmutableArray<string>[Enum.GetValues<BrewerScheme>().Length][];
        for (var i = 0; i < schemes.Length; i++)
        {
            schemes[i] = [.. SchemeData.Brewer(i).Select(Colors)];
        }

        return schemes;
    }

    private static SchemeInterpolator[] BuildBrewerInterpolators()
    {
        var interpolators = new SchemeInterpolator[Brewers.Length];
        for (var i = 0; i < interpolators.Length; i++)
        {
            Color[] colors = [.. Table(SchemeData.Brewer(i)[^1]).Select(rgb => (Color)rgb)];
            interpolators[i] = SchemeInterpolator.FromBasis(new Interpolation.RgbBasisInterpolator(colors));
        }

        return interpolators;
    }

    private static ImmutableArray<string> Colors(string hex)
    {
        var colors = ImmutableArray.CreateBuilder<string>(hex.Length / 6);
        for (var i = 0; i + 6 <= hex.Length; i += 6)
        {
            colors.Add(string.Concat("#", hex.AsSpan(i, 6)));
        }

        return colors.MoveToImmutable();
    }

    private static Rgb[] Table(string hex)
    {
        var table = new Rgb[hex.Length / 6];
        for (var i = 0; i < table.Length; i++)
        {
            var n = int.Parse(hex.AsSpan(i * 6, 6), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            table[i] = new Rgb((n >> 16) & 0xff, (n >> 8) & 0xff, n & 0xff);
        }

        return table;
    }
}
