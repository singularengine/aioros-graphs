using Aioros.Graphs.Formatting;
using Aioros.Graphs.Interpolation;

namespace Aioros.Graphs.Colors;

/// <summary>
/// One of d3-scale-chromatic's continuous color schemes (<see cref="ColorSchemes"/>), as its <c>interpolate*</c>
/// function: <see cref="Interpolate"/> returns the color and <see cref="Format"/> its text as d3 writes it, a hex
/// color for viridis, magma, inferno and plasma and <c>rgb()</c> for the others.
/// </summary>
public readonly struct SchemeInterpolator : IInterpolator<Rgb>
{
    private const string NaNText = "rgb(NaN, NaN, NaN)";

    private readonly SchemeKind kind;
    private readonly Rgb[]? ramp;
    private readonly string[]? rampText;
    private readonly RgbBasisInterpolator basis;
    private readonly CubehelixInterpolator cubehelix;

    private SchemeInterpolator(SchemeKind kind, Rgb[]? ramp, RgbBasisInterpolator basis, CubehelixInterpolator cubehelix)
    {
        this.kind = kind;
        this.ramp = ramp;
        rampText = ramp is null ? null : Array.ConvertAll(ramp, color => color.FormatHex());
        this.basis = basis;
        this.cubehelix = cubehelix;
    }

    private enum SchemeKind : byte
    {
        Ramp,
        Basis,
        Cubehelix,
        Cividis,
        Turbo,
        Rainbow,
        Sinebow,
    }

    /// <summary>Gets a value indicating whether d3 writes this scheme's colors in hex rather than <c>rgb()</c>.</summary>
    public bool IsHex => kind == SchemeKind.Ramp;

    /// <inheritdoc />
    public Rgb Interpolate(double t) => kind switch
    {
        SchemeKind.Ramp => Ramp(ramp!, t),
        SchemeKind.Basis => basis.Interpolate(t),
        SchemeKind.Cubehelix => cubehelix.Interpolate(t),
        SchemeKind.Cividis => Cividis(t),
        SchemeKind.Turbo => Turbo(t),
        SchemeKind.Rainbow => Rainbow(t),
        _ => Sinebow(t),
    };

    /// <summary>Returns the color at <paramref name="t"/> as d3's <c>interpolate*(t)</c> writes it.</summary>
    /// <param name="t">The position, clamped to [0, 1] except by rainbow and sinebow, which repeat; NaN writes <c>rgb(NaN, NaN, NaN)</c> for turbo and cividis, as d3 does.</param>
    /// <returns>The text.</returns>
    public string Format(double t)
    {
        if (WritesNaN(t))
        {
            return NaNText;
        }

        if (!IsHex)
        {
            return Interpolate(t).FormatRgb();
        }

        var i = RampIndex(ramp!.Length, t);
        return i < 0 ? Interpolate(t).FormatHex() : rampText![i];
    }

    /// <summary>Writes <see cref="Format(double)"/>'s text without allocating.</summary>
    /// <param name="t">The position.</param>
    /// <param name="destination">Where the characters go.</param>
    /// <param name="written">The number of characters written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormat(double t, Span<char> destination, out int written)
    {
        if (WritesNaN(t))
        {
            var copied = NaNText.TryCopyTo(destination);
            written = copied ? NaNText.Length : 0;
            return copied;
        }

        return IsHex ? Interpolate(t).TryFormatHex(destination, out written) : Interpolate(t).TryFormatRgb(destination, out written);
    }

    /// <summary>Writes <see cref="Format(double)"/>'s text as UTF-8 without allocating.</summary>
    /// <param name="t">The position.</param>
    /// <param name="destination">Where the bytes go.</param>
    /// <param name="written">The number of bytes written.</param>
    /// <returns>Whether the text fit.</returns>
    public bool TryFormat(double t, Span<byte> destination, out int written)
    {
        if (WritesNaN(t))
        {
            var copied = "rgb(NaN, NaN, NaN)"u8.TryCopyTo(destination);
            written = copied ? NaNText.Length : 0;
            return copied;
        }

        return IsHex ? Interpolate(t).TryFormatHex(destination, out written) : Interpolate(t).TryFormatRgb(destination, out written);
    }

    private bool WritesNaN(double t) => double.IsNaN(t) && kind is SchemeKind.Cividis or SchemeKind.Turbo;

    internal static SchemeInterpolator FromRamp(Rgb[] table) => new(SchemeKind.Ramp, table, default, default);

    internal static SchemeInterpolator FromBasis(RgbBasisInterpolator basis) => new(SchemeKind.Basis, null, basis, default);

    internal static SchemeInterpolator FromCubehelix(Cubehelix start, Cubehelix end) =>
        new(SchemeKind.Cubehelix, null, default, CubehelixInterpolator.LongArc(start, end));

    internal static SchemeInterpolator Cividis() => new(SchemeKind.Cividis, null, default, default);

    internal static SchemeInterpolator Turbo() => new(SchemeKind.Turbo, null, default, default);

    internal static SchemeInterpolator Rainbow() => new(SchemeKind.Rainbow, null, default, default);

    internal static SchemeInterpolator Sinebow() => new(SchemeKind.Sinebow, null, default, default);

    private static Rgb Ramp(Rgb[] table, double t)
    {
        var i = RampIndex(table.Length, t);
        return i < 0 ? new Rgb(double.NaN, double.NaN, double.NaN) : table[i];
    }

    private static int RampIndex(int n, double t)
    {
        var i = Math.Max(0, Math.Min(n - 1, Math.Floor(t * n)));
        return double.IsNaN(i) ? -1 : (int)i;
    }

    private static Rgb Cividis(double t)
    {
        t = Math.Max(0, Math.Min(1, t));
        return new Rgb(
            Byte(-4.54 - t * (35.34 - t * (2381.73 - t * (6402.7 - t * (7024.72 - t * 2710.57))))),
            Byte(32.49 + t * (170.73 + t * (52.82 - t * (131.46 - t * (176.58 - t * 67.37))))),
            Byte(81.24 + t * (442.36 - t * (2482.43 - t * (6167.24 - t * (6614.94 - t * 2475.67))))));
    }

    private static Rgb Turbo(double t)
    {
        t = Math.Max(0, Math.Min(1, t));
        return new Rgb(
            Byte(34.61 + t * (1172.33 - t * (10793.56 - t * (33300.12 - t * (38394.49 - t * 14825.05))))),
            Byte(23.31 + t * (557.33 + t * (1225.33 - t * (3574.96 - t * (1073.77 + t * 707.56))))),
            Byte(27.2 + t * (3211.1 - t * (15327.97 - t * (27814 - t * (22569.18 - t * 6838.66))))));
    }

    private static Rgb Rainbow(double t)
    {
        if (t < 0 || t > 1)
        {
            t -= Math.Floor(t);
        }

        var ts = Math.Abs(t - 0.5);
        return new Cubehelix(360 * t - 100, 1.5 - 1.5 * ts, 0.8 - 0.9 * ts).ToRgb();
    }

    private static Rgb Sinebow(double t)
    {
        t = (0.5 - t) * Math.PI;
        var r = Math.Sin(t);
        var g = Math.Sin(t + Math.PI / 3);
        var b = Math.Sin(t + Math.PI * 2 / 3);
        return new Rgb(255 * r * r, 255 * g * g, 255 * b * b);
    }

    private static double Byte(double value) => Math.Max(0, Math.Min(255, JsNumber.Round(value)));
}
