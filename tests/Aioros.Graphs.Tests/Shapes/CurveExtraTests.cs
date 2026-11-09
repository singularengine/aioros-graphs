using System.Buffers;
using System.Text.Json;
using Aioros.Graphs.Shapes;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Tests.Shapes;

public sealed class CurveExtraTests
{
    [Fact]
    [Trait("AC", "SHAPE-10")]
    public async Task ClosedOpenBundleAndBumpLinesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape-extra", "lines"))
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
    [Trait("AC", "SHAPE-11")]
    public async Task ClosedOpenAndBumpAreasMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape-extra", "areas"))
        {
            var x0 = item.Numbers("x0");
            var y0 = item.Numbers("y0");
            var y1 = item.Numbers("y1");
            var defined = y1.Select(v => !double.IsNaN(v)).ToArray();
            var curve = CurveNames.Parse(item.Text("curve"));

            var actual = GoldenFixture.Svg((ref SvgPathWriter writer) => Area.Write(ref writer, curve, x0, y0, y1, defined), Digits(item));

            Assert.True(Expected(item) == actual, $"{item.Text("curve")} {item.Text("series")} ({Digits(item)}): expected {Expected(item)}, got {actual}");
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-12")]
    public async Task AreasRejectTheBundleCurveAsD3Async()
    {
        var expected = (await GoldenFixture.CasesAsync("shape-extra", "bundleArea"))[0];
        Assert.True(expected.GetProperty("throws").GetBoolean());
        Assert.Equal(JsonValueKind.Null, expected.GetProperty("empty").ValueKind);

        var buffer = new ArrayBufferWriter<byte>();
        Assert.Throws<InvalidOperationException>(() =>
        {
            var writer = new SvgPathWriter(buffer);
            Area.Write(ref writer, Curve.Bundle(), [10, 30.5], [0], [20, 40.25]);
        });

        var empty = new ArrayBufferWriter<byte>();
        var emptyWriter = new SvgPathWriter(empty);
        Area.Write(ref emptyWriter, Curve.Bundle(), [], [0], []);
        Assert.Equal(0, empty.WrittenCount);
    }

    private static int? Digits(JsonElement item) => item.OptionalNumber("digits") is { } d ? (int)d : null;

    private static string Expected(JsonElement item) => item.OptionalText("path") ?? "";
}
