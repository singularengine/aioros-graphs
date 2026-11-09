using System.Text.Json;
using Aioros.Graphs.Hierarchies;

namespace Aioros.Graphs.Tests.Hierarchies;

public sealed class LayoutTests
{
    [Fact]
    [Trait("AC", "HIERARCHY-10")]
    public async Task TreeMatchesD3Async() => await NodeLayoutAsync("tree", Tree.Compute);

    [Fact]
    [Trait("AC", "HIERARCHY-11")]
    public async Task ClusterMatchesD3Async() => await NodeLayoutAsync("cluster", Cluster.Compute);

    [Fact]
    [Trait("AC", "HIERARCHY-12")]
    public async Task TreemapMatchesD3Async()
    {
        var trees = await HierarchyFixture.TreesAsync();
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "treemap"))
        {
            var hierarchy = HierarchyFixture.Summed(trees[item.Text("tree")], item.OptionalSort());
            var size = item.Numbers("size");
            var tiling = item.Text("tiling");
            var options = Padded(item, new TreemapOptions { Width = size[0], Height = size[1], Round = item.TryGetProperty("round", out var round) && round.GetBoolean() });
            options = tiling switch
            {
                "squarify" => options,
                "squarify1" => options with { Ratio = 1 },
                "squarify3" => options with { Ratio = 3 },
                "squarify0.5" => options with { Ratio = 0.5 },
                _ => options with { Tiling = Enum.Parse<TreemapTiling>(tiling, ignoreCase: true) },
            };

            HierarchyFixture.SameRectangles(item, Treemap.Compute(hierarchy, options));
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-13")]
    public async Task ResquarifyKeepsThePreviousRowsAsync()
    {
        var trees = await HierarchyFixture.TreesAsync();
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "resquarify"))
        {
            var first = HierarchyFixture.Summed(trees[item.Text("tree")], "valueDescending");
            var options = new TreemapOptions { Width = 960, Height = 500, PaddingInner = 1, PaddingTop = 1, PaddingRight = 1, PaddingBottom = 1, PaddingLeft = 1, Tiling = TreemapTiling.Resquarify };
            var previous = Treemap.Compute(first, options);
            var second = first.Sum(item.Numbers("values"));
            var next = item.Text("second") switch
            {
                "resquarify" => options,
                "squarify" => options with { Tiling = TreemapTiling.Squarify },
                _ => options with { Ratio = 2 },
            };

            HierarchyFixture.SameRectangles(item, Treemap.Compute(second, next, previous));
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-14")]
    public async Task PackMatchesD3Async()
    {
        var trees = await HierarchyFixture.TreesAsync();
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "pack"))
        {
            var name = item.Text("tree");
            var hierarchy = HierarchyFixture.Summed(trees[name], item.OptionalSort());
            var size = item.Numbers("size");
            var options = new PackOptions { Width = size[0], Height = size[1], Padding = item.OptionalNumber("padding") ?? 0 };

            var circles = item.GetProperty("radii").ValueKind == JsonValueKind.Array
                ? Pack.Compute(hierarchy, options, item.Numbers("radii"))
                : Pack.Compute(hierarchy, options);

            GoldenFixture.Same(item.Numbers("x"), circles.X, name + " x");
            GoldenFixture.Same(item.Numbers("y"), circles.Y, name + " y");
            GoldenFixture.Same(item.Numbers("r"), circles.R, name + " r");
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-15")]
    public async Task PackSiblingsMatchesD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "packSiblings"))
        {
            var radii = item.Numbers("radii");
            var x = new double[radii.Length];
            var y = new double[radii.Length];

            var radius = Pack.Siblings(radii, x, y);

            GoldenFixture.Same(item.Numbers("x"), x, "x");
            GoldenFixture.Same(item.Numbers("y"), y, "y");
            GoldenFixture.Same(item.Number("radius"), radius, "radius");
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-16")]
    public async Task PackEncloseMatchesD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "packEnclose"))
        {
            Circle[] circles = [.. item.GetProperty("circles").EnumerateArray().Select(c => new Circle(c[0].Number(), c[1].Number(), c[2].Number()))];

            var found = Pack.Enclose(circles, out var enclosing);

            var expected = item.GetProperty("enclose");
            Assert.Equal(expected.ValueKind == JsonValueKind.Array, found);
            if (found)
            {
                GoldenFixture.Same(expected.Numbers(), [enclosing.X, enclosing.Y, enclosing.R], "enclose");
            }
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-17")]
    public async Task PartitionMatchesD3Async()
    {
        var trees = await HierarchyFixture.TreesAsync();
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "partition"))
        {
            var hierarchy = HierarchyFixture.Summed(trees[item.Text("tree")], item.OptionalSort());
            var size = item.TryGetProperty("size", out var s) ? s.Numbers() : [1, 1];
            var options = new PartitionOptions
            {
                Width = size[0],
                Height = size[1],
                Padding = item.OptionalNumber("padding") ?? 0,
                Round = item.TryGetProperty("round", out var round) && round.GetBoolean(),
            };

            HierarchyFixture.SameRectangles(item, Partition.Compute(hierarchy, options));
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-18")]
    public void LayoutsRejectMismatchedInputs()
    {
        var hierarchy = Hierarchy.FromParents([-1, 0, 0]).Sum([0, 1, 2]);
        var other = Hierarchy.FromParents([-1, 0]).Sum([0, 1]);
        var previous = Treemap.Compute(other, new TreemapOptions { Tiling = TreemapTiling.Resquarify });

        Assert.Throws<ArgumentException>(() => Pack.Compute(hierarchy, new PackOptions(), [1, 2]));
        Assert.Throws<ArgumentException>(() => Pack.Siblings([1, 2, 3], new double[2], new double[3]));
        Assert.Throws<ArgumentException>(() => Pack.Siblings([1, 2, 3], new double[3], new double[2]));
        Assert.Throws<ArgumentException>(() => Treemap.Compute(hierarchy, new TreemapOptions(), previous));
    }

    private static async Task NodeLayoutAsync(string group, Func<Hierarchy, NodeLayoutOptions, NodePoints> layout)
    {
        var trees = await HierarchyFixture.TreesAsync();
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", group))
        {
            var name = item.Text("tree");
            var hierarchy = HierarchyFixture.Summed(trees[name], item.OptionalSort());
            var nodeSize = item.TryGetProperty("nodeSize", out var n);
            var size = nodeSize ? n.Numbers() : item.TryGetProperty("size", out var s) ? s.Numbers() : [1, 1];
            var options = new NodeLayoutOptions
            {
                Width = size[0],
                Height = size[1],
                NodeSize = nodeSize,
                Separation = item.OptionalText("separation") == "radial" ? NodeSeparation.Radial : NodeSeparation.Default,
            };

            var points = layout(hierarchy, options);

            GoldenFixture.Same(item.Numbers("x"), points.X, $"{group} {name} {item} x");
            GoldenFixture.Same(item.Numbers("y"), points.Y, $"{group} {name} {item} y");
        }
    }

    private static TreemapOptions Padded(JsonElement item, TreemapOptions options)
    {
        var padding = item.OptionalNumber("padding");
        var outer = item.OptionalNumber("paddingOuter") ?? padding;
        return options with
        {
            PaddingInner = item.OptionalNumber("paddingInner") ?? padding ?? 0,
            PaddingTop = item.OptionalNumber("paddingTop") ?? outer ?? 0,
            PaddingRight = item.OptionalNumber("paddingRight") ?? outer ?? 0,
            PaddingBottom = item.OptionalNumber("paddingBottom") ?? outer ?? 0,
            PaddingLeft = item.OptionalNumber("paddingLeft") ?? outer ?? 0,
        };
    }
}
