using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using Aioros.Graphs.Colors;

namespace Aioros.Graphs.Tests.Colors;

public sealed partial class ColorTests
{
    private const double Tolerance = 1e-10;

    [Fact]
    [Trait("AC", "COLOR-01")]
    public async Task ParsedColorsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("color", "parse"))
        {
            var specifier = item.Text("specifier");
            Assert.True(Color.TryParse(specifier, out var color), specifier);
            Assert.Equal(item.Text("space"), Space(color.Space));
            GoldenFixture.Same(item.Numbers("values"), Channels(color), specifier);
        }
    }

    [Fact]
    [Trait("AC", "COLOR-02")]
    public async Task RejectedSpecifiersParseToNoColorAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("color", "rejected"))
        {
            var specifier = item.Text("specifier");
            Assert.False(item.GetProperty("parsed").GetBoolean());
            Assert.False(Color.TryParse(specifier, out _), specifier);
            var rgb = Rgb.Parse(specifier);
            GoldenFixture.Same(item.Numbers("rgb"), [rgb.R, rgb.G, rgb.B, rgb.Opacity], specifier);
        }

        Assert.False(Color.TryParse(null, out _));
    }

    [Fact]
    [Trait("AC", "COLOR-03")]
    public async Task ColorsFormatAsD3Async()
    {
        var chars = new char[160];
        var bytes = new byte[160];
        foreach (var item in await GoldenFixture.CasesAsync("color", "parse"))
        {
            var color = Color.Parse(item.Text("specifier"));
            AssertFormats(item, color, item.Text("specifier"));
            var rgb = color.ToRgb();
            var hsl = color.ToHsl();
            AssertSpans(item.Text("hex"), chars, bytes, rgb.TryFormatHex, rgb.TryFormatHex);
            AssertSpans(item.Text("hex8"), chars, bytes, rgb.TryFormatHex8, rgb.TryFormatHex8);
            AssertSpans(item.Text("rgb"), chars, bytes, rgb.TryFormatRgb, rgb.TryFormatRgb);
            AssertSpans(item.Text("hsl"), chars, bytes, hsl.TryFormatHsl, hsl.TryFormatHsl);
            Assert.False(rgb.TryFormatRgb(chars.AsSpan(0, 3), out _));
        }
    }

    [Fact]
    [Trait("AC", "COLOR-04")]
    public async Task ConversionsBetweenSpacesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("color", "parse"))
        {
            var color = Color.Parse(item.Text("specifier"));
            AssertConversions(item.GetProperty("conversions"), color, item.Text("specifier"), exact: true);
        }
    }

    [Fact]
    [Trait("AC", "COLOR-05")]
    public async Task BrighterAndDarkerMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("color", "parse"))
        {
            var color = Color.Parse(item.Text("specifier"));
            AssertShades(item, color, item.Text("specifier"));
            var brighter = item.GetProperty("brighterValues").EnumerateArray().ToList();
            var darker = item.GetProperty("darkerValues").EnumerateArray().ToList();
            var shades = Shades(color);
            for (var i = 0; i < 4; i++)
            {
                Near(brighter[i].Numbers(), Channels(shades.Brighter[i]), item.Text("specifier"));
                Near(darker[i].Numbers(), Channels(shades.Darker[i]), item.Text("specifier"));
            }

            foreach (var (space, converted) in Conversions(color))
            {
                AssertShades(item.GetProperty("conversions").GetProperty(space), converted, $"{item.Text("specifier")} {space}");
            }
        }
    }

    [Fact]
    [Trait("AC", "COLOR-06")]
    public async Task DisplayableAndClampMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("color", "parse"))
        {
            var color = Color.Parse(item.Text("specifier"));
            AssertDisplayAndClamp(item, color, item.Text("specifier"));
            foreach (var (space, converted) in Conversions(color))
            {
                AssertDisplayAndClamp(item.GetProperty("conversions").GetProperty(space), converted, $"{item.Text("specifier")} {space}");
            }
        }
    }

    [Fact]
    [Trait("AC", "COLOR-07")]
    public async Task ConstructedColorsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("color", "constructed"))
        {
            var kind = item.Text("kind");
            var p = item.Numbers("input");
            Color color = kind switch
            {
                "rgb" => new Rgb(p[0], p[1], p[2], p[3]),
                "hsl" => new Hsl(p[0], p[1], p[2], p[3]),
                "lab" => new Lab(p[0], p[1], p[2], p[3]),
                "hcl" => new Hcl(p[0], p[1], p[2], p[3]),
                "lch" => Hcl.Lch(p[0], p[1], p[2], p[3]),
                "cubehelix" => new Cubehelix(p[0], p[1], p[2], p[3]),
                _ => Lab.Gray(p[0], p[3]),
            };
            var because = $"{kind}({string.Join(", ", p)})";
            Near(item.Numbers("values"), Channels(color), because);
            AssertFormats(item, color, because);
            AssertShades(item, color, because);
            AssertDisplayAndClamp(item, color, because);
            AssertConversions(item.GetProperty("conversions"), color, because, exact: kind is "rgb" or "hsl");
        }
    }

    private static void AssertFormats(JsonElement item, Color color, string because)
    {
        Assert.True(item.Text("hex") == color.FormatHex(), $"{because} hex: {color.FormatHex()}");
        Assert.True(item.Text("hex8") == color.FormatHex8(), $"{because} hex8: {color.FormatHex8()}");
        Assert.True(item.Text("rgb") == color.FormatRgb(), $"{because} rgb: {color.FormatRgb()}");
        Assert.True(item.Text("hsl") == color.FormatHsl(), $"{because} hsl: {color.FormatHsl()}");
        Assert.True(item.Text("text") == color.ToString(), $"{because} text: {color}");
    }

    private static void AssertShades(JsonElement item, Color color, string because)
    {
        var brighter = item.GetProperty("brighter").EnumerateArray().Select(e => e.GetString()).ToList();
        var darker = item.GetProperty("darker").EnumerateArray().Select(e => e.GetString()).ToList();
        var shades = Shades(color);
        for (var i = 0; i < 4; i++)
        {
            Assert.True(brighter[i] == shades.Brighter[i].FormatRgb(), $"{because} brighter[{i}]: {shades.Brighter[i].FormatRgb()}, expected {brighter[i]}");
            Assert.True(darker[i] == shades.Darker[i].FormatRgb(), $"{because} darker[{i}]: {shades.Darker[i].FormatRgb()}, expected {darker[i]}");
        }
    }

    private static void AssertDisplayAndClamp(JsonElement item, Color color, string because)
    {
        Assert.True(item.GetProperty("displayable").GetBoolean() == color.IsDisplayable, $"{because} displayable");
        var clamp = item.GetProperty("clamp");
        switch (color.Space)
        {
            case ColorSpace.Rgb:
                var rgb = color.ToRgb().Clamp();
                GoldenFixture.Same(clamp.Numbers(), [rgb.R, rgb.G, rgb.B, rgb.Opacity], because + " clamp");
                break;
            case ColorSpace.Hsl:
                var hsl = color.ToHsl().Clamp();
                Near(clamp.Numbers(), [hsl.H, hsl.S, hsl.L, hsl.Opacity], because + " clamp");
                break;
            default:
                Assert.Equal(JsonValueKind.Null, clamp.ValueKind);
                break;
        }
    }

    private static void AssertConversions(JsonElement conversions, Color color, string because, bool exact)
    {
        foreach (var (space, converted) in Conversions(color))
        {
            var expected = conversions.GetProperty(space);
            var label = $"{because} as {space}";
            Assert.Equal(space, Space(converted.Space));
            if (exact && space is "rgb" or "hsl")
            {
                GoldenFixture.Same(expected.Numbers("values"), Channels(converted), label);
            }
            else
            {
                Near(expected.Numbers("values"), Channels(converted), label);
            }

            var tie = !exact || space is not ("rgb" or "hsl");
            AssertRounded(expected.Text("rgb"), converted.FormatRgb(), converted.ToRgb(), tie, label);
            AssertRounded(expected.Text("hex"), converted.FormatHex(), converted.ToRgb(), tie, label);
            if (exact && space is "rgb" or "hsl")
            {
                Assert.True(expected.Text("hsl") == converted.FormatHsl(), $"{label}: {converted.FormatHsl()}, expected {expected.Text("hsl")}");
            }
            else
            {
                NearText(expected.Text("hsl"), converted.FormatHsl(), label);
            }
        }
    }

    private static IEnumerable<(string Space, Color Color)> Conversions(Color color) =>
    [
        ("hsl", color.ToHsl()),
        ("lab", color.ToLab()),
        ("hcl", color.ToHcl()),
        ("cubehelix", color.ToCubehelix()),
        ("rgb", color.ToRgb()),
    ];

    private static (Color[] Brighter, Color[] Darker) Shades(Color color) =>
        ([color.Brighter(), color.Brighter(1), color.Brighter(2.5), color.Brighter(-1)],
         [color.Darker(), color.Darker(1), color.Darker(2.5), color.Darker(-1)]);

    private static string Space(ColorSpace space) => space switch
    {
        ColorSpace.Rgb => "rgb",
        ColorSpace.Hsl => "hsl",
        ColorSpace.Lab => "lab",
        ColorSpace.Hcl => "hcl",
        _ => "cubehelix",
    };

    private static double[] Channels(Color color) => [color.Channel0, color.Channel1, color.Channel2, color.Opacity];

    private static void Near(double[] expected, double[] actual, string because)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            var (e, a) = (expected[i], actual[i]);
            var near = e.Equals(a) || Math.Abs(e - a) <= Tolerance * Math.Max(1, Math.Abs(e));
            Assert.True(near, $"{because} [{i}]: expected {e:R}, got {a:R}");
        }
    }

    private static void AssertRounded(string expected, string actual, Rgb color, bool tie, string because)
    {
        if (expected == actual || (tie && SameUpToTies(expected, actual, color)))
        {
            return;
        }

        Assert.Fail($"{because}: expected {expected}, got {actual}");
    }

    private static bool SameUpToTies(string expected, string actual, Rgb color)
    {
        var e = Bytes(expected);
        var a = Bytes(actual);
        double[] channels = [color.R, color.G, color.B];
        for (var i = 0; i < 3; i++)
        {
            var atTie = Math.Abs(Math.Abs(channels[i] - Math.Floor(channels[i])) - 0.5) < 1e-9;
            if (e[i] != a[i] && !(atTie && Math.Abs(e[i] - a[i]) == 1))
            {
                return false;
            }
        }

        return e.Length == a.Length && e.Skip(3).SequenceEqual(a.Skip(3));
    }

    private static int[] Bytes(string text) => text[0] == '#'
        ? [.. Enumerable.Range(0, (text.Length - 1) / 2).Select(i => Convert.ToInt32(text.Substring(1 + 2 * i, 2), 16))]
        : [.. NumberPattern().Matches(text).Select((m, i) => (int)Math.Round(double.Parse(m.Value, CultureInfo.InvariantCulture) * (i < 3 ? 1 : 1e6)))];

    private static void NearText(string expected, string actual, string because)
    {
        Assert.True(NumberPattern().Replace(expected, "#") == NumberPattern().Replace(actual, "#"), $"{because}: expected {expected}, got {actual}");
        Near(
            [.. NumberPattern().Matches(expected).Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture))],
            [.. NumberPattern().Matches(actual).Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture))],
            $"{because}: expected {expected}, got {actual}");
    }

    [GeneratedRegex(@"-?[0-9.]+(?:e[+-]?[0-9]+)?")]
    private static partial Regex NumberPattern();

    private delegate bool CharFormatter(Span<char> destination, out int written);

    private delegate bool ByteFormatter(Span<byte> destination, out int written);

    private static void AssertSpans(string expected, Span<char> chars, Span<byte> bytes, CharFormatter toChars, ByteFormatter toBytes)
    {
        Assert.True(toChars(chars, out var written));
        Assert.Equal(expected, chars[..written].ToString());
        Assert.True(toBytes(bytes, out written));
        Assert.Equal(expected, Encoding.UTF8.GetString(bytes[..written]));
    }
}
