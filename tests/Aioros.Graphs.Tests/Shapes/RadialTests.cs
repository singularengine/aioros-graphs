using System.Buffers;
using System.Text.Json;
using Aioros.Graphs.Shapes;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Tests.Shapes;

public sealed class RadialTests
{
    [Fact]
    [Trait("AC", "SHAPE-18")]
    public async Task RadialLinesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape-extra", "radialLines"))
        {
            var angle = item.Numbers("angle");
            var radius = item.Numbers("radius");
            var defined = radius.Select(v => !double.IsNaN(v)).ToArray();
            var curve = CurveNames.Parse(item.Text("curve"));

            var actual = GoldenFixture.Svg((ref SvgPathWriter writer) => LineRadial.Write(ref writer, curve, angle, radius, defined), Digits(item));

            Check(item, actual);
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-19")]
    public async Task RadialAreasMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape-extra", "radialAreas"))
        {
            var start = item.Numbers("startAngle");
            var end = item.TryGetProperty("endAngle", out _) ? item.Numbers("endAngle") : [];
            var inner = item.Numbers("innerRadius");
            var outer = item.Numbers("outerRadius");
            var defined = outer.Select(v => !double.IsNaN(v)).ToArray();
            var curve = CurveNames.Parse(item.Text("curve"));

            var actual = GoldenFixture.Svg((ref SvgPathWriter writer) => AreaRadial.Write(ref writer, curve, start, inner, outer, defined, end), Digits(item));

            Check(item, actual);
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-20")]
    public async Task RadialPointsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape-extra", "pointRadial"))
        {
            var (x, y) = Radial.Point(item.Number("angle"), item.Number("radius"));

            Assert.True(GoldenFixture.SamePath(string.Join(",", item.Numbers("point")), $"{x:R},{y:R}", 1e-12), $"{item}: got {x:R},{y:R}");
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-21")]
    public void MismatchedRadialSpansAndUnknownSymbolsAreRejected()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new SvgPathWriter(buffer);

        Assert.Throws<ArgumentException>(() => LineRadial.Write(ref writer, Curve.Linear, [1, 2], [1]));
        Assert.Throws<ArgumentException>(() => LineRadial.Write(ref writer, Curve.Linear, [1, 2], [1, 2], [true]));
        Assert.Throws<ArgumentException>(() => AreaRadial.Write(ref writer, Curve.Linear, [1, 2], [0, 0, 0], [1, 2]));
        Assert.Throws<ArgumentException>(() => AreaRadial.Write(ref writer, Curve.Linear, [1, 2], [0], [1]));
        Assert.Throws<ArgumentException>(() => AreaRadial.Write(ref writer, Curve.Linear, [1, 2], [0], [1, 2], endAngle: [1, 2, 3]));
        Assert.Throws<ArgumentOutOfRangeException>(() => Symbol.Write(ref writer, (SymbolType)99));
        Assert.Equal(0, buffer.WrittenCount);
    }

    internal static void Check(JsonElement item, string actual)
    {
        var expected = item.OptionalText("path") ?? "";
        if (Digits(item) is null)
        {
            Assert.True(GoldenFixture.SamePath(expected, actual, 1e-12), $"{item}: got {actual}");
        }
        else
        {
            Assert.True(expected == actual, $"{item}: got {actual}");
        }
    }

    private static int? Digits(JsonElement item) => item.OptionalNumber("digits") is { } d ? (int)d : null;
}
