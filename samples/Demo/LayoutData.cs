using System.Text.Json;

namespace Aioros.Graphs.Demo;

internal sealed record TreeNode(string Name, double Value, TreeNode[]? Children);

internal sealed record FlowMatrix(string[] Names, double[] Values);

internal sealed record Network(int[] Group, int[] Sources, int[] Targets);

internal sealed record Flows(string[] Names, int[] Sources, int[] Targets, double[] Values);

internal sealed record LayoutData(TreeNode Software, TreeNode Packages, FlowMatrix Trade, Network Network, Flows Energy);

internal static class LayoutDataSet
{
    private static readonly string[] Categories = ["analytics", "animate", "data", "display", "physics", "query", "util", "vis"];
    private static readonly string[] Syllables = ["Agg", "Bet", "Cluster", "Data", "Edge", "Flow", "Graph", "Heap", "Inter", "Join", "Link", "Merge", "Node", "Path", "Query", "Range", "Shape", "Tree", "Union", "Vector"];
    private static readonly string[] Suffixes = ["Builder", "Layout", "Operator", "Sprite", "Scale", "Source", "Filter", "Tween", "Sort", "Set"];

    public static LayoutData Generate()
    {
        var random = new SeededRandom(20261013);
        var software = Software(random);
        return new LayoutData(software, Packages(software), Trade(random), Network(random), Energy(random));
    }

    public static void Write(Utf8JsonWriter json, LayoutData data)
    {
        json.WritePropertyName("software");
        Tree(json, data.Software);
        json.WritePropertyName("packages");
        Tree(json, data.Packages);
        json.WriteStartObject("trade");
        json.WriteStartArray("names");
        foreach (var name in data.Trade.Names)
        {
            json.WriteStringValue(name);
        }

        json.WriteEndArray();
        Numbers(json, "values", data.Trade.Values);
        json.WriteEndObject();
        json.WriteStartObject("network");
        Numbers(json, "group", [.. data.Network.Group.Select(g => (double)g)]);
        Numbers(json, "sources", [.. data.Network.Sources.Select(g => (double)g)]);
        Numbers(json, "targets", [.. data.Network.Targets.Select(g => (double)g)]);
        json.WriteEndObject();
        json.WriteStartObject("energy");
        json.WriteStartArray("names");
        foreach (var name in data.Energy.Names)
        {
            json.WriteStringValue(name);
        }

        json.WriteEndArray();
        Numbers(json, "sources", [.. data.Energy.Sources.Select(g => (double)g)]);
        Numbers(json, "targets", [.. data.Energy.Targets.Select(g => (double)g)]);
        Numbers(json, "values", data.Energy.Values);
        json.WriteEndObject();
    }

    private static Flows Energy(SeededRandom random)
    {
        string[] names = ["Coal", "Natural gas", "Oil", "Nuclear", "Wind", "Solar", "Biomass", "Electricity", "Heat", "Refined fuels", "Industry", "Homes", "Transport", "Commerce", "Losses"];
        int[][] routes = [[7, 8, 10], [7, 8, 11, 13], [9, 10], [7], [7], [7, 11], [8, 9, 7], [10, 11, 12, 13, 14], [10, 11, 13, 14], [12, 10, 14]];
        var sources = new List<int>();
        var targets = new List<int>();
        var values = new List<double>();
        for (var source = 0; source < routes.Length; source++)
        {
            foreach (var target in routes[source])
            {
                var r = random.NextDouble();
                sources.Add(source);
                targets.Add(target);
                values.Add(Math.Round(20 + 400 * r * r) / 10);
            }
        }

        return new Flows(names, [.. sources], [.. targets], [.. values]);
    }

    private static TreeNode Software(SeededRandom random)
    {
        var categories = new TreeNode[Categories.Length];
        for (var c = 0; c < categories.Length; c++)
        {
            var packages = new List<TreeNode>();
            var count = 2 + (int)(random.NextDouble() * 3);
            for (var p = 0; p < count; p++)
            {
                var leaves = new TreeNode[2 + (int)(random.NextDouble() * 6)];
                for (var l = 0; l < leaves.Length; l++)
                {
                    leaves[l] = Leaf(random);
                }

                packages.Add(new TreeNode(Name(random).ToLowerInvariant(), 0, leaves));
            }

            if (random.NextDouble() < 0.5)
            {
                packages.Add(Leaf(random));
            }

            categories[c] = new TreeNode(Categories[c], 0, [.. packages]);
        }

        return new TreeNode("flare", 0, categories);
    }

    private static TreeNode Leaf(SeededRandom random)
    {
        var r = random.NextDouble();
        return new TreeNode(Name(random) + Suffixes[(int)(random.NextDouble() * Suffixes.Length)], Math.Round(400 + 24000 * r * r), null);
    }

    private static string Name(SeededRandom random) => Syllables[(int)(random.NextDouble() * Syllables.Length)];

    private static TreeNode Packages(TreeNode software) =>
        new(software.Name, 0, [.. software.Children!.Select(c => new TreeNode(c.Name, 0, [.. c.Children!.Where(p => p.Children is not null).Select(p => new TreeNode(p.Name, 0, null))]))]);

    private static FlowMatrix Trade(SeededRandom random)
    {
        string[] names = ["Americas", "Europe", "Africa", "Middle East", "South Asia", "East Asia", "Oceania", "Arctic"];
        var values = new double[names.Length * names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            for (var j = 0; j < names.Length; j++)
            {
                var r = random.NextDouble();
                values[i * names.Length + j] = i == j ? Math.Round(200 * r) : Math.Round(3000 * r * r * (names.Length - Math.Abs(i - j)) / names.Length);
            }
        }

        return new FlowMatrix(names, values);
    }

    private static Network Network(SeededRandom random)
    {
        const int count = 60;
        var group = new int[count];
        var sources = new List<int>();
        var targets = new List<int>();
        for (var i = 0; i < count; i++)
        {
            group[i] = i < 6 ? i : (int)(random.NextDouble() * 6);
            if (i < 6)
            {
                continue;
            }

            var hub = group[i];
            sources.Add(i);
            targets.Add(hub);
            for (var k = 6; k < i; k++)
            {
                if (group[k] == group[i] && random.NextDouble() < 0.12)
                {
                    sources.Add(i);
                    targets.Add(k);
                }
            }
        }

        for (var h = 0; h < 6; h++)
        {
            sources.Add(h);
            targets.Add((h + 1) % 6);
        }

        return new Network(group, [.. sources], [.. targets]);
    }

    private static void Tree(Utf8JsonWriter json, TreeNode node)
    {
        json.WriteStartObject();
        json.WriteString("name", node.Name);
        if (node.Children is null)
        {
            json.WriteNumber("value", node.Value);
        }
        else
        {
            json.WriteStartArray("children");
            foreach (var child in node.Children)
            {
                Tree(json, child);
            }

            json.WriteEndArray();
        }

        json.WriteEndObject();
    }

    private static void Numbers(Utf8JsonWriter json, string name, double[] values)
    {
        json.WriteStartArray(name);
        foreach (var value in values)
        {
            json.WriteNumberValue(value);
        }

        json.WriteEndArray();
    }
}
