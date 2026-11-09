using Aioros.Graphs.Chords;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Tests.Chords;

public sealed class RibbonTests
{
    [Fact]
    [Trait("AC", "CHORD-05")]
    public async Task RibbonsMatchD3Async() => await CheckAsync("ribbons");

    [Fact]
    [Trait("AC", "CHORD-06")]
    public async Task ArrowRibbonsMatchD3Async() => await CheckAsync("arrows");

    private static async Task CheckAsync(string group)
    {
        foreach (var item in await GoldenFixture.CasesAsync("chord", group))
        {
            var ribbons = item.GetProperty("ribbons").EnumerateArray().Select(r => r.Numbers()).ToList();
            var pad = item.Number("padAngle");
            var head = item.OptionalNumber("headRadius");
            var digits = item.OptionalNumber("digits") is { } d ? (int?)d : null;
            var standalone = item.TryGetProperty("standalone", out _);

            var actual = GoldenFixture.Svg((ref SvgPathWriter writer) =>
            {
                if (standalone)
                {
                    Write(ref writer, Shape(ribbons[0], pad), head);
                    return;
                }

                var context = new PathContext<SvgPathWriter>(ref writer);
                foreach (var ribbon in ribbons)
                {
                    Write(ref context, Shape(ribbon, pad), head);
                }
            }, digits);

            var expected = item.Text("path");
            Assert.True(digits is null ? GoldenFixture.SamePath(expected, actual, 1e-12) : expected == actual, $"{item}: got {actual}");
        }
    }

    private static RibbonShape Shape(double[] r, double pad) => new(r[0], r[1], r[2], r[3], r[4], r[5]) { PadAngle = pad };

    private static void Write(ref SvgPathWriter writer, in RibbonShape shape, double? head)
    {
        if (head is { } h)
        {
            Ribbon.WriteArrow(ref writer, shape, h);
        }
        else
        {
            Ribbon.Write(ref writer, shape);
        }
    }

    private static void Write(ref PathContext<SvgPathWriter> context, in RibbonShape shape, double? head)
    {
        if (head is { } h)
        {
            Ribbon.WriteArrow(ref context, shape, h);
        }
        else
        {
            Ribbon.Write(ref context, shape);
        }
    }
}
