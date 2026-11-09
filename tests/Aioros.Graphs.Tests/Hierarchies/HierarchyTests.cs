using System.Globalization;
using System.Text.Json;
using Aioros.Graphs.Hierarchies;

namespace Aioros.Graphs.Tests.Hierarchies;

public sealed class HierarchyTests
{
    [Fact]
    [Trait("AC", "HIERARCHY-01")]
    public async Task NestedDataBuildsD3sHierarchyAsync()
    {
        var trees = await HierarchyFixture.TreesAsync();
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "structure"))
        {
            var name = item.Text("tree");
            var hierarchy = HierarchyFixture.Build(trees[name], out _);

            Assert.Equal(item.Ints("parent"), hierarchy.Parents.ToArray());
            Assert.Equal(item.Ints("depth"), hierarchy.Depths.ToArray());
            Assert.Equal(item.Ints("height"), hierarchy.Heights.ToArray());
            var children = item.GetProperty("children").EnumerateArray().ToList();
            for (var i = 0; i < hierarchy.Count; i++)
            {
                Assert.Equal(children[i].EnumerateArray().Select(c => c.GetInt32()), hierarchy.Children(i).ToArray());
                Assert.Equal(item.GetProperty("ancestors")[i].EnumerateArray().Select(c => c.GetInt32()), hierarchy.Ancestors(i));
            }

            Assert.Equal(item.Ints("descendants"), hierarchy.Descendants());
            Assert.Equal(item.Ints("leaves"), hierarchy.Leaves());
            Assert.Equal(item.Ints("preOrder"), hierarchy.PreOrder());
            Assert.Equal(item.Ints("postOrder"), hierarchy.PostOrder());
            Assert.Equal(item.GetProperty("links").EnumerateArray().Select(l => new HierarchyLink(l[0].GetInt32(), l[1].GetInt32())), hierarchy.Links());
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-02")]
    public async Task PathsGoThroughTheLeastCommonAncestorAsync()
    {
        var trees = await HierarchyFixture.TreesAsync();
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "nodePaths"))
        {
            var hierarchy = HierarchyFixture.Build(trees[item.Text("tree")], out _);
            foreach (var pair in item.GetProperty("pairs").EnumerateArray())
            {
                Assert.Equal(pair.Ints("path"), hierarchy.Path(pair.GetProperty("from").GetInt32(), pair.GetProperty("to").GetInt32()));
            }
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-03")]
    public async Task SumAndCountMatchD3Async()
    {
        var trees = await HierarchyFixture.TreesAsync();
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "values"))
        {
            var name = item.Text("tree");
            var hierarchy = HierarchyFixture.Build(trees[name], out var values);

            GoldenFixture.Same(item.Numbers("values"), values, name + " values");
            GoldenFixture.Same(item.Numbers("sum"), hierarchy.Sum(values).Values.ToArray(), name + " sum");
            GoldenFixture.Same(item.Numbers("count"), hierarchy.CountLeaves().Values.ToArray(), name + " count");
            Assert.All(hierarchy.Values.ToArray(), v => Assert.True(double.IsNaN(v)));
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-04")]
    public void SumNeedsOneValuePerNode()
    {
        var hierarchy = Hierarchy.FromParents([-1, 0, 0]);

        Assert.Throws<ArgumentException>(() => hierarchy.Sum([1, 2]));
        Assert.Throws<ArgumentException>(() => hierarchy.Sum([1, 2, 3, 4]));
    }

    [Fact]
    [Trait("AC", "HIERARCHY-05")]
    public async Task SortOrdersChildrenAsD3Async()
    {
        var trees = await HierarchyFixture.TreesAsync();
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "sorts"))
        {
            var name = item.Text("tree");
            var summed = HierarchyFixture.Summed(trees[name], null);
            var sorted = summed.Sort(HierarchyFixture.Comparator(summed, item.Text("comparator")));

            var children = item.GetProperty("children").EnumerateArray().ToList();
            for (var i = 0; i < sorted.Count; i++)
            {
                Assert.True(children[i].EnumerateArray().Select(c => c.GetInt32()).SequenceEqual(sorted.Children(i).ToArray()), $"{name} {item.Text("comparator")} node {i}");
            }

            Assert.Equal(summed.Parents.ToArray(), sorted.Parents.ToArray());
            Assert.Equal(summed.Depths.ToArray(), sorted.Depths.ToArray());
            Assert.Equal(summed.Heights.ToArray(), sorted.Heights.ToArray());
            Assert.Equal(summed.Values.ToArray(), sorted.Values.ToArray());
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-06")]
    public async Task StratifyMatchesD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "stratify"))
        {
            var hierarchy = Hierarchy.Stratify(Strings(item, "ids"), Strings(item, "parentIds"));

            Check(item, hierarchy);
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-07")]
    public async Task StratifyRejectsWhatD3RejectsAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "stratifyErrors"))
        {
            var error = Assert.Throws<ArgumentException>(() => Hierarchy.Stratify(Strings(item, "ids"), Strings(item, "parentIds")));

            Assert.StartsWith(item.Text("error") + " (", error.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-08")]
    public async Task StratifyPathsMatchesD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "paths"))
        {
            var hierarchy = Hierarchy.StratifyPaths([.. Strings(item, "paths").Select(p => p!)]);

            Check(item, hierarchy);
        }
    }

    [Fact]
    [Trait("AC", "HIERARCHY-09")]
    public async Task ParentIndicesBuildD3sStratifiedHierarchyAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("hierarchy", "parents"))
        {
            var parents = item.Ints("parents");
            if (item.OptionalText("error") is { } message)
            {
                var error = Assert.Throws<ArgumentException>(() => Hierarchy.FromParents(parents));
                Assert.StartsWith(message + " (", error.Message, StringComparison.Ordinal);
                continue;
            }

            Check(item, Hierarchy.FromParents(parents), ids: false);
        }
    }

    private static string?[] Strings(JsonElement item, string property) =>
        [.. item.GetProperty(property).EnumerateArray().Select(e => e.ValueKind == JsonValueKind.Null ? null : e.GetString())];

    private static void Check(JsonElement item, Hierarchy hierarchy, bool ids = true)
    {
        var expected = item.GetProperty("nodes").EnumerateArray().ToList();
        var descendants = hierarchy.Descendants();
        Assert.Equal(expected.Count, descendants.Length);
        for (var k = 0; k < expected.Count; k++)
        {
            CheckNode(expected[k], hierarchy, descendants[k], ids);
        }
    }

    private static void CheckNode(JsonElement e, Hierarchy hierarchy, int node, bool ids)
    {
        var row = e.GetProperty("row").GetInt32();
        Assert.Equal(row, hierarchy.Row(node));
        if (row >= 0)
        {
            Assert.Equal(row, node);
        }

        if (ids)
        {
            Assert.Equal(e.OptionalText("id"), hierarchy.Id(node));
        }

        Assert.Equal(e.GetProperty("depth").GetInt32(), hierarchy.Depths[node]);
        Assert.Equal(e.GetProperty("height").GetInt32(), hierarchy.Heights[node]);
        var parent = hierarchy.Parents[node];
        var parentRow = e.GetProperty("parentRow").GetInt32();
        Assert.Equal(parentRow == -2 ? -1 : parentRow, parent < 0 ? -1 : hierarchy.Row(parent));
        if (ids && parent >= 0)
        {
            Assert.Equal(e.OptionalText("parent"), hierarchy.Id(parent));
        }

        var children = e.GetProperty("children").EnumerateArray().Select(c => c.ValueKind == JsonValueKind.Number ? c.GetInt32().ToString(CultureInfo.InvariantCulture) : c.GetString()).ToList();
        var actual = hierarchy.Children(node).ToArray().Select(c => hierarchy.Row(c) >= 0 ? c.ToString(CultureInfo.InvariantCulture) : hierarchy.Id(c)).ToList();
        Assert.Equal(children, actual);
    }
}
