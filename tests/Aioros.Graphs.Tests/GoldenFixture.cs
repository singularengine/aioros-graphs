using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Tests;

internal static partial class GoldenFixture
{
    public static async Task<IReadOnlyList<JsonElement>> CasesAsync(string fixture, string group)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Golden", fixture + ".json");
        using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        var cases = document.RootElement.GetProperty("cases").GetProperty(group).EnumerateArray().Select(e => e.Clone()).ToList();
        Assert.NotEmpty(cases);
        return cases;
    }

    public static double Number(this JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.String => element.GetString() switch
        {
            "NaN" => double.NaN,
            "Infinity" => double.PositiveInfinity,
            "-Infinity" => double.NegativeInfinity,
            "-0" => -0.0,
            var text => double.Parse(text!, CultureInfo.InvariantCulture),
        },
        JsonValueKind.Null => double.NaN,
        _ => throw new InvalidOperationException($"Not a number: {element}"),
    };

    public static double Number(this JsonElement element, string property) => element.GetProperty(property).Number();

    public static double[] Numbers(this JsonElement element) => [.. element.EnumerateArray().Select(Number)];

    public static double[] Numbers(this JsonElement element, string property) => element.GetProperty(property).Numbers();

    public static string Text(this JsonElement element, string property) => element.GetProperty(property).GetString()!;

    public static string? OptionalText(this JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    public static double? OptionalNumber(this JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null ? value.Number() : null;

    public static string Svg(SvgDrawing draw, int? digits = 3)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer, digits);
        draw(ref writer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static bool SamePath(string expected, string actual, double tolerance)
    {
        if (expected == actual)
        {
            return true;
        }

        var e = Tokens().Split(expected);
        var a = Tokens().Split(actual);
        if (e.Length != a.Length)
        {
            return false;
        }

        for (var i = 0; i < e.Length; i++)
        {
            if (e[i] == a[i])
            {
                continue;
            }

            if (!double.TryParse(e[i], CultureInfo.InvariantCulture, out var x) || !double.TryParse(a[i], CultureInfo.InvariantCulture, out var y)
                || !(Math.Abs(x - y) <= tolerance * Math.Max(1, Math.Abs(x))))
            {
                return false;
            }
        }

        return true;
    }

    public static void Same(double expected, double actual, string because) =>
        Assert.True(expected.Equals(actual), $"{because}: expected {expected:R}, got {actual:R}");

    public static void Same(IReadOnlyList<double> expected, IReadOnlyList<double> actual, string because)
    {
        Assert.True(expected.Count == actual.Count, $"{because}: expected {expected.Count} values [{string.Join(", ", expected)}], got {actual.Count} [{string.Join(", ", actual)}]");
        for (var i = 0; i < expected.Count; i++)
        {
            Same(expected[i], actual[i], $"{because} [{i}]");
        }
    }

    [GeneratedRegex("(?<=[A-Za-z,])|(?=[A-Za-z,])")]
    private static partial Regex Tokens();
}

internal delegate void SvgDrawing(ref SvgPathWriter writer);
