using System.Text.Json;
using Aioros.Graphs.Hierarchies;

namespace Aioros.Graphs.Tests.Hierarchies;

internal static class HierarchyFixture
{
    public static async Task<Dictionary<string, JsonElement>> TreesAsync() =>
        (await GoldenFixture.CasesAsync("hierarchy", "trees")).ToDictionary(t => t.Text("name"), t => t.GetProperty("data"));

    public static Hierarchy Build(JsonElement data, out double[] values)
    {
        var hierarchy = Hierarchy.FromNested(data, Children, out var nodes);
        values = new double[nodes.Length];
        for (var i = 0; i < nodes.Length; i++)
        {
            Assert.Equal(i, nodes[i].GetProperty("i").GetInt32());
            values[i] = nodes[i].TryGetProperty("value", out var value) ? value.Number() : double.NaN;
        }

        return hierarchy;
    }

    public static Hierarchy Summed(JsonElement data, string? sort)
    {
        var hierarchy = Build(data, out var values).Sum(values);
        return sort is null ? hierarchy : hierarchy.Sort(Comparator(hierarchy, sort));
    }

    public static Func<int, int, double> Comparator(Hierarchy hierarchy, string name)
    {
        var values = hierarchy.Values.ToArray();
        var heights = hierarchy.Heights.ToArray();
        return name switch
        {
            "valueDescending" => (a, b) => values[b] - values[a],
            "valueAscending" => (a, b) => values[a] - values[b],
            "heightThenValue" => (a, b) => heights[b] - heights[a] is var h && h != 0 ? h : values[b] - values[a],
            "nanForThirds" => (a, b) => a % 3 == 0 ? double.NaN : values[b] - values[a],
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
        };
    }

    public static int[] Ints(this JsonElement element, string property) => [.. element.GetProperty(property).EnumerateArray().Select(e => e.GetInt32())];

    public static string? OptionalSort(this JsonElement item) => item.OptionalText("sort");

    public static void SameRectangles(JsonElement item, NodeRectangles actual)
    {
        GoldenFixture.Same(item.Numbers("x0"), actual.X0, $"{item.Text("tree")} x0");
        GoldenFixture.Same(item.Numbers("y0"), actual.Y0, $"{item.Text("tree")} y0");
        GoldenFixture.Same(item.Numbers("x1"), actual.X1, $"{item.Text("tree")} x1");
        GoldenFixture.Same(item.Numbers("y1"), actual.Y1, $"{item.Text("tree")} y1");
    }

    private static List<JsonElement>? Children(JsonElement node) =>
        node.TryGetProperty("children", out var children) ? [.. children.EnumerateArray()] : null;
}
