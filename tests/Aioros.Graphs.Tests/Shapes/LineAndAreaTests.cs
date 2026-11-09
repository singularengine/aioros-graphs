using System.Buffers;
using System.Text.Json;
using Aioros.Graphs.Shapes;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Tests.Shapes;

public sealed class LineAndAreaTests
{
    [Fact]
    [Trait("AC", "SHAPE-01")]
    public async Task LinesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape", "lines"))
        {
            var x = item.Numbers("x");
            var y = item.Numbers("y");
            var defined = y.Select(v => !double.IsNaN(v)).ToArray();
            var curve = CurveNames.Parse(item.Text("curve"));

            var actual = GoldenFixture.Svg((ref SvgPathWriter writer) => Line.Write(ref writer, curve, x, y, defined), Digits(item));

            Assert.True(Expected(item) == actual, $"{item.Text("curve")} {item.Text("series")} ({Digits(item)}): expected {Expected(item)}, got {actual}");
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-02")]
    public async Task AreasMatchD3Async()
    {
        foreach (var item in (await GoldenFixture.CasesAsync("shape", "areas")).Where(c => !c.TryGetProperty("x1", out _)))
        {
            var actual = WriteArea(item);

            Assert.True(Expected(item) == actual, $"{item.Text("curve")} {item.Text("series")} ({Digits(item)}): expected {Expected(item)}, got {actual}");
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-03")]
    public async Task AreasWithSeparateToplineAndConstantBaselineMatchD3Async()
    {
        var shifted = (await GoldenFixture.CasesAsync("shape", "areas")).Where(c => c.TryGetProperty("x1", out _)).ToList();
        Assert.Equal(13, shifted.Count);
        foreach (var item in shifted)
        {
            var actual = WriteArea(item);

            Assert.True(Expected(item) == actual, $"{item.Text("curve")}: expected {Expected(item)}, got {actual}");
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-04")]
    public void MismatchedSpansAreRejected()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);

        Assert.Throws<ArgumentException>(() => Line.Write(ref writer, Curve.Linear, [1, 2], [1]));
        Assert.Throws<ArgumentException>(() => Line.Write(ref writer, Curve.Linear, [1, 2], [1, 2], [true]));
        Assert.Throws<ArgumentException>(() => Area.Write(ref writer, Curve.Linear, [1, 2], [0, 0, 0], [1, 2]));
        Assert.Throws<ArgumentException>(() => Area.Write(ref writer, Curve.Linear, [1, 2], [0], [1]));
        Assert.Throws<ArgumentException>(() => Area.Write(ref writer, Curve.Linear, [1, 2], [0], [1, 2], x1: [1, 2, 3]));
        Assert.Equal(0, buffer.WrittenCount);
    }

    private static string WriteArea(JsonElement item)
    {
        var x0 = item.Numbers("x0");
        var y0 = item.Numbers("y0");
        var y1 = item.Numbers("y1");
        var x1 = item.TryGetProperty("x1", out _) ? item.Numbers("x1") : [];
        var defined = y1.Select(v => !double.IsNaN(v)).ToArray();
        var curve = CurveNames.Parse(item.Text("curve"));
        return GoldenFixture.Svg((ref SvgPathWriter writer) => Area.Write(ref writer, curve, x0, y0, y1, defined, x1), Digits(item));
    }

    private static int? Digits(JsonElement item) => item.OptionalNumber("digits") is { } d ? (int)d : null;

    private static string Expected(JsonElement item) => item.OptionalText("path") ?? "";
}
