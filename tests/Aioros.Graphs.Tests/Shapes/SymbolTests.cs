using System.Buffers;
using System.Text.Json;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Shapes;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Tests.Shapes;

public sealed class SymbolTests
{
    [Fact]
    [Trait("AC", "SHAPE-13")]
    public async Task SymbolsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape-extra", "symbols"))
        {
            var type = Type(item.Text("type"));
            var size = item.OptionalNumber("size");

            var actual = GoldenFixture.Svg((ref SvgPathWriter writer) =>
            {
                if (size is { } s)
                {
                    Symbol.Write(ref writer, type, s);
                }
                else
                {
                    Symbol.Write(ref writer, type);
                }
            }, Digits(item));

            Check(item, actual);
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-14")]
    public async Task TranslatedSymbolsShareOnePathAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("shape-extra", "translatedSymbols"))
        {
            var type = Type(item.Text("type"));
            var size = item.Number("size");
            var centers = item.GetProperty("centers").EnumerateArray().Select(c => c.Numbers()).ToList();

            var actual = GoldenFixture.Svg((ref SvgPathWriter writer) =>
            {
                var context = new PathContext<SvgPathWriter>(ref writer);
                foreach (var center in centers)
                {
                    Symbol.Write(ref context, type, size, center[0], center[1]);
                }
            }, Digits(item));

            Check(item, actual);
        }
    }

    [Fact]
    [Trait("AC", "SHAPE-15")]
    public async Task SymbolListsFollowD3Async()
    {
        var lists = (await GoldenFixture.CasesAsync("shape-extra", "lists"))[0];

        Assert.Equal(lists.GetProperty("fill").EnumerateArray().Select(e => Type(e.GetString()!)), Symbol.Fill.ToArray());
        Assert.Equal(lists.GetProperty("stroke").EnumerateArray().Select(e => Type(e.GetString()!)), Symbol.Stroke.ToArray());
    }

    internal static SymbolType Type(string name) => name switch
    {
        "circle" => SymbolType.Circle,
        "cross" => SymbolType.Cross,
        "diamond" => SymbolType.Diamond,
        "square" => SymbolType.Square,
        "star" => SymbolType.Star,
        "triangle" => SymbolType.Triangle,
        "wye" => SymbolType.Wye,
        "plus" => SymbolType.Plus,
        "times" => SymbolType.Times,
        "triangle2" => SymbolType.Triangle2,
        "asterisk" => SymbolType.Asterisk,
        "square2" => SymbolType.Square2,
        "diamond2" => SymbolType.Diamond2,
        _ => throw new InvalidOperationException(name),
    };

    private static void Check(JsonElement item, string actual)
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
