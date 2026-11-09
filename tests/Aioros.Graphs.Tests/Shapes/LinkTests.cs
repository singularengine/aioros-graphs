using System.Text.Json;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Shapes;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Tests.Shapes;

public sealed class LinkTests
{
    [Fact]
    [Trait("AC", "SHAPE-16")]
    public async Task LinksMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape-extra", "links"))
        {
            var name = item.Text("curve");
            var ends = item.TryGetProperty("many", out var many)
                ? many.EnumerateArray().Select(Ends).ToList()
                : [(item.Numbers("source"), item.Numbers("target"))];

            var actual = GoldenFixture.Svg((ref SvgPathWriter writer) =>
            {
                var context = new PathContext<SvgPathWriter>(ref writer);
                foreach (var (source, target) in ends)
                {
                    switch (name)
                    {
                        case "horizontal":
                            Link.Horizontal(ref context, source[0], source[1], target[0], target[1]);
                            break;
                        case "vertical":
                            Link.Vertical(ref context, source[0], source[1], target[0], target[1]);
                            break;
                        default:
                            Link.Write(ref context, CurveNames.Parse(name), source[0], source[1], target[0], target[1]);
                            break;
                    }
                }
            }, Digits(item));

            Assert.True(Expected(item) == actual, $"{item}: got {actual}");
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-17")]
    public async Task RadialLinksMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape-extra", "radialLinks"))
        {
            var source = item.Numbers("source");
            var target = item.Numbers("target");

            var actual = GoldenFixture.Svg((ref SvgPathWriter writer) => Link.Radial(ref writer, source[0], source[1], target[0], target[1]), Digits(item));

            RadialTests.Check(item, actual);
        }
    }

    private static (double[] Source, double[] Target) Ends(JsonElement pair) => (pair[0].Numbers(), pair[1].Numbers());

    private static int? Digits(JsonElement item) => item.OptionalNumber("digits") is { } d ? (int)d : null;

    private static string Expected(JsonElement item) => item.OptionalText("path") ?? "";
}
