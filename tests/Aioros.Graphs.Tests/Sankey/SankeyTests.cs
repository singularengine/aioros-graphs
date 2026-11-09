using System.Text.Json;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Sankey;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Tests.Sankey;

public sealed class SankeyTests
{
    [Fact]
    [Trait("AC", "SANKEY-01")]
    public async Task LayoutsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("sankey", "layouts"))
        {
            Check(item, Layout(item));
        }
    }

    [Fact]
    [Trait("AC", "SANKEY-02")]
    public async Task NodesByStringIdMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("sankey", "ids"))
        {
            Check(item, Layout(item));
        }
    }

    [Fact]
    [Trait("AC", "SANKEY-03")]
    public async Task NodeSortsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("sankey", "nodeSorts"))
        {
            Check(item, Layout(item));
        }
    }

    [Fact]
    [Trait("AC", "SANKEY-04")]
    public async Task LinkSortsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("sankey", "linkSorts"))
        {
            Check(item, Layout(item));
        }
    }

    [Fact]
    [Trait("AC", "SANKEY-05")]
    public async Task LinkPathsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("sankey", "layouts"))
        {
            var layout = Layout(item);
            var links = item.GetProperty("result").GetProperty("links");
            for (var i = 0; i < layout.LinkCount; i++)
            {
                var link = i;
                var actual = GoldenFixture.Svg((ref SvgPathWriter writer) => layout.WriteLink(ref writer, link), null);
                Assert.True(links[i].Text("path") == actual, $"{Name(item)} link {i}: expected {links[i].Text("path")}, got {actual}");
            }

            var joined = GoldenFixture.Svg((ref SvgPathWriter writer) =>
            {
                var context = new PathContext<SvgPathWriter>(ref writer);
                for (var i = 0; i < layout.LinkCount; i++)
                {
                    layout.WriteLink(ref context, i);
                }
            }, null);
            Assert.Equal(item.GetProperty("result").Text("joined"), joined);
        }
    }

    [Fact]
    [Trait("AC", "SANKEY-06")]
    public void LinksToMissingNodesAreRejected()
    {
        var options = new SankeyOptions();
        Assert.Equal("missing: 3", Assert.Throws<ArgumentException>(() => SankeyLayout.Compute(3, [0, 1], [1, 3], [1, 1], options)).Message);
        Assert.Equal("missing: -1", Assert.Throws<ArgumentException>(() => SankeyLayout.Compute(3, [-1], [1], [1], options)).Message);
        Assert.Equal("missing: z", Assert.Throws<ArgumentException>(() => SankeyLayout.Compute(["a", "b"], ["a", "z"], ["b", "a"], [1, 1], options)).Message);
        Assert.Equal("missing: y", Assert.Throws<ArgumentException>(() => SankeyLayout.Compute(["a", "b"], ["y"], ["z"], [1], options)).Message);
    }

    [Fact]
    [Trait("AC", "SANKEY-07")]
    public async Task CyclesAreRejectedAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("sankey", "cycles"))
        {
            var (sources, targets, values) = Links(item);
            var error = Assert.Throws<ArgumentException>(() => SankeyLayout.Compute(item.GetProperty("nodes").GetInt32(), sources, targets, values, new SankeyOptions()));
            Assert.Equal(item.Text("error"), error.Message);
        }
    }

    [Fact]
    [Trait("AC", "SANKEY-08")]
    public void MismatchedInputsAreRejected()
    {
        var options = new SankeyOptions();
        Assert.Throws<ArgumentException>(() => SankeyLayout.Compute(2, [0, 1], [1], [1, 1], options));
        Assert.Throws<ArgumentException>(() => SankeyLayout.Compute(2, [0], [1], [1, 1], options));
        Assert.Throws<ArgumentException>(() => SankeyLayout.Compute(2, [0], [1], [1], options, [1, 2, 3]));
        Assert.Throws<ArgumentException>(() => SankeyLayout.Compute(0, [], [], [], options));
        Assert.Throws<ArgumentException>(() => SankeyLayout.Compute([], [], [], [], options));
        Assert.Throws<ArgumentException>(() => SankeyLayout.Compute(["a", "b"], ["a"], ["b", "a"], [1], options));
    }

    private static SankeyLayout Layout(JsonElement item)
    {
        var options = Options(item.GetProperty("options"));
        var (sources, targets, values) = Links(item);
        var fixedValues = item.GetProperty("fixed").ValueKind == JsonValueKind.Array ? item.Numbers("fixed") : [];
        if (item.GetProperty("ids").ValueKind != JsonValueKind.Array)
        {
            return SankeyLayout.Compute(item.GetProperty("nodes").GetInt32(), sources, targets, values, options, fixedValues);
        }

        var ids = item.GetProperty("ids").EnumerateArray().Select(e => e.GetString()!).ToArray();
        return SankeyLayout.Compute(ids, [.. sources.Select(s => ids[s])], [.. targets.Select(t => ids[t])], values, options, fixedValues);
    }

    private static (int[] Sources, int[] Targets, double[] Values) Links(JsonElement item)
    {
        var links = item.GetProperty("links").EnumerateArray().ToList();
        return ([.. links.Select(l => l[0].GetInt32())], [.. links.Select(l => l[1].GetInt32())], [.. links.Select(l => l[2].Number())]);
    }

    private static SankeyOptions Options(JsonElement o)
    {
        var options = new SankeyOptions();
        if (o.TryGetProperty("extent", out var extent))
        {
            options = options with { X0 = extent[0][0].Number(), Y0 = extent[0][1].Number(), X1 = extent[1][0].Number(), Y1 = extent[1][1].Number() };
        }

        options = options with
        {
            NodeWidth = o.OptionalNumber("nodeWidth") ?? options.NodeWidth,
            NodePadding = o.OptionalNumber("nodePadding") ?? options.NodePadding,
            Iterations = (int)(o.OptionalNumber("iterations") ?? options.Iterations),
            Align = o.OptionalText("align") switch
            {
                "left" => SankeyAlign.Left,
                "right" => SankeyAlign.Right,
                "center" => SankeyAlign.Center,
                _ => SankeyAlign.Justify,
            },
        };

        options = o.OptionalText("nodeSort") switch
        {
            "input" => options with { NodeOrder = SankeyOrder.Input },
            "valueDescending" => options with { NodeComparison = (layout, a, b) => layout.Value[b] - layout.Value[a] },
            "indexDescending" => options with { NodeComparison = (_, a, b) => b - a },
            _ => options,
        };

        return o.OptionalText("linkSort") switch
        {
            "input" => options with { LinkOrder = SankeyOrder.Input },
            "valueDescending" => options with { LinkComparison = (layout, a, b) => layout.LinkValue[b] - layout.LinkValue[a] },
            _ => options,
        };
    }

    private static string Name(JsonElement item) => $"{item.Text("graph")} {item.GetProperty("options")}";

    private static void Check(JsonElement item, SankeyLayout layout)
    {
        var name = Name(item);
        var result = item.GetProperty("result");
        var nodes = result.GetProperty("nodes");
        Assert.Equal(nodes.GetArrayLength(), layout.NodeCount);
        for (var i = 0; i < layout.NodeCount; i++)
        {
            var node = nodes[i];
            var at = $"{name} node {i}";
            GoldenFixture.Same(node.Number("x0"), layout.X0[i], at + " x0");
            GoldenFixture.Same(node.Number("x1"), layout.X1[i], at + " x1");
            GoldenFixture.Same(node.Number("y0"), layout.Y0[i], at + " y0");
            GoldenFixture.Same(node.Number("y1"), layout.Y1[i], at + " y1");
            GoldenFixture.Same(node.Number("value"), layout.Value[i], at + " value");
            Assert.True(node.GetProperty("depth").GetInt32() == layout.Depth[i], at + " depth");
            Assert.True(node.GetProperty("height").GetInt32() == layout.Height[i], at + " height");
            Assert.True(node.GetProperty("layer").GetInt32() == layout.Layer[i], at + " layer");
            Assert.True(Ints(node, "sourceLinks").SequenceEqual(layout.SourceLinks(i).ToArray()), at + " sourceLinks");
            Assert.True(Ints(node, "targetLinks").SequenceEqual(layout.TargetLinks(i).ToArray()), at + " targetLinks");
        }

        var links = result.GetProperty("links");
        Assert.Equal(links.GetArrayLength(), layout.LinkCount);
        for (var i = 0; i < layout.LinkCount; i++)
        {
            var at = $"{name} link {i}";
            GoldenFixture.Same(links[i].Number("y0"), layout.LinkY0[i], at + " y0");
            GoldenFixture.Same(links[i].Number("y1"), layout.LinkY1[i], at + " y1");
            GoldenFixture.Same(links[i].Number("width"), layout.LinkWidth[i], at + " width");
        }
    }

    private static int[] Ints(JsonElement element, string property) => [.. element.GetProperty(property).EnumerateArray().Select(e => e.GetInt32())];
}
