using System.Buffers;
using System.Text;
using Aioros.Graphs.Chords;
using Aioros.Graphs.Forces;
using Aioros.Graphs.Hierarchies;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Sankey;
using Aioros.Graphs.Shapes;
using Aioros.Graphs.Svg;

namespace Aioros.Graphs.Demo;

internal static class LayoutCharts
{
    private const double Width = 640;
    private const double Height = 360;

    public static Scene Treemap(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var (summed, nodes) = Software(data);
        var heights = summed.Heights.ToArray();
        var values = summed.Values.ToArray();
        var root = summed.Sort((a, b) => heights[b] - heights[a] is var d && d != 0 ? d : values[b] - values[a]);
        var tiles = Hierarchies.Treemap.Compute(root, new TreemapOptions
        {
            Width = Width,
            Height = Height,
            PaddingInner = 1,
            PaddingTop = 1,
            PaddingRight = 1,
            PaddingBottom = 1,
            PaddingLeft = 1,
            Round = true,
        });
        foreach (var leaf in root.Leaves())
        {
            var (x0, y0, x1, y1) = (tiles.X0[leaf], tiles.Y0[leaf], tiles.X1[leaf], tiles.Y1[leaf]);
            scene.Path("tile s" + (Category(summed, leaf) + 1), PathText.Rect(x0, y0, x1 - x0, y1 - y0), nodes[leaf].Name);
            if (x1 - x0 > 64 && y1 - y0 > 16)
            {
                scene.Text("tile-name", x0 + 4, y0 + 13, nodes[leaf].Name);
            }
        }

        return scene;
    }

    public static Scene Pack(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var (summed, nodes) = Software(data);
        var values = summed.Values.ToArray();
        var root = summed.Sort((a, b) => values[b] - values[a]);
        var circles = Hierarchies.Pack.Compute(root, new PackOptions { Width = Width - 2, Height = Height - 2, Padding = 3 });
        foreach (var node in root.Descendants())
        {
            var leaf = root.Children(node).Length == 0;
            var cls = leaf ? "bubble s" + (Category(summed, node) + 1) : "group-circle depth" + root.Depths[node];
            scene.Path(cls, PathText.Circle(circles.X[node], circles.Y[node], circles.R[node]), nodes[node].Name, [1, 1]);
        }

        foreach (var node in root.Leaves())
        {
            if (circles.R[node] > 20)
            {
                scene.Text("bubble-name", circles.X[node] + 1, circles.Y[node] + 4, nodes[node].Name[..Math.Min(nodes[node].Name.Length, (int)(circles.R[node] / 3.5))], "middle");
            }
        }

        return scene;
    }

    public static Scene Sunburst(DemoData data)
    {
        const double radius = Height / 2 - 4;
        var scene = new Scene(Width, Height);
        var (summed, nodes) = Software(data);
        var values = summed.Values.ToArray();
        var root = summed.Sort((a, b) => values[b] - values[a]);
        var parts = Partition.Compute(root, new PartitionOptions { Width = 2 * Math.PI, Height = radius });
        foreach (var node in root.Descendants())
        {
            if (root.Depths[node] == 0)
            {
                continue;
            }

            var arc = Arc(parts, node, radius);
            scene.Path("sector s" + (Category(summed, node) + 1) + " depth" + root.Depths[node], PathText.Arc(arc), nodes[node].Name, [Width / 2, Height / 2]);
        }

        foreach (var node in root.Children(root.Root))
        {
            var (cx, cy) = Shapes.Arc.Centroid(Arc(parts, node, radius));
            scene.Text("sector-name", Width / 2 + cx, Height / 2 + cy + 4, nodes[node].Name, "middle");
        }

        return scene;
    }

    public static Scene TidyTree(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var hierarchy = Hierarchy.FromNested(data.Layouts.Packages, n => n.Children, out var nodes);
        var points = Tree.Compute(hierarchy, new NodeLayoutOptions { Width = Height - 40, Height = Width - 200 });
        foreach (var link in hierarchy.Links())
        {
            scene.Path("link", PathText.LinkHorizontal(points.Y[link.Source], points.X[link.Source], points.Y[link.Target], points.X[link.Target]), null, [90, 20]);
        }

        foreach (var node in hierarchy.Descendants())
        {
            var inner = hierarchy.Children(node).Length > 0;
            scene.Circle(inner ? "node inner" : "node", 90 + points.Y[node], 20 + points.X[node], 2.5);
            scene.Text("label", 90 + points.Y[node] + (inner ? -6 : 6), 20 + points.X[node] + 3, nodes[node].Name, inner ? "end" : "start");
        }

        return scene;
    }

    public static Scene ChordDiagram(DemoData data)
    {
        const double outerRadius = Height / 2 - 30;
        const double innerRadius = outerRadius - 10;
        var scene = new Scene(Width, Height);
        var trade = data.Layouts.Trade;
        var layout = ChordLayout.Compute(trade.Values, trade.Names.Length, new ChordOptions { PadAngle = 10 / innerRadius, SortSubgroups = ChordSort.Descending, SortChords = ChordSort.Descending });
        foreach (var chord in layout.Chords)
        {
            var ribbon = RibbonShape.Of(chord, innerRadius - 1) with { PadAngle = 1 / innerRadius };
            scene.Path("ribbon s" + (chord.Source.Index + 1), PathText.Ribbon(ribbon), trade.Names[chord.Source.Index] + " → " + trade.Names[chord.Target.Index], [Width / 2, Height / 2]);
        }

        foreach (var group in layout.Groups)
        {
            scene.Path("chord-group s" + (group.Index + 1), PathText.Arc(new ArcShape(innerRadius, outerRadius, group.StartAngle, group.EndAngle)), trade.Names[group.Index], [Width / 2, Height / 2]);
            var angle = (group.StartAngle + group.EndAngle) / 2;
            scene.Text("label", Width / 2 + (outerRadius + 8) * Math.Sin(angle), Height / 2 - (outerRadius + 8) * Math.Cos(angle) + 4, trade.Names[group.Index], angle > Math.PI ? "end" : "start");
        }

        return scene;
    }

    public static Scene ForceGraph(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var network = data.Layouts.Network;
        var simulation = new ForceSimulation(network.Group.Length)
            .Force("link", new LinkForce(network.Sources, network.Targets))
            .Force("charge", new ManyBodyForce { Strength = -60 })
            .Force("x", new XForce())
            .Force("y", new YForce());
        simulation.Tick(300);
        var x = simulation.X;
        var y = simulation.Y;
        scene.Path("edge", PathText.Segments(x, y, network.Sources, network.Targets), null, [Width / 2, Height / 2]);
        for (var i = 0; i < network.Group.Length; i++)
        {
            scene.Path("vertex s" + (network.Group[i] + 1), PathText.Circle(x[i], y[i], i < 6 ? 7 : 4.5), null, [Width / 2, Height / 2]);
        }

        return scene;
    }

    public static Scene SankeyDiagram(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var energy = data.Layouts.Energy;
        var layout = SankeyLayout.Compute(energy.Names.Length, energy.Sources, energy.Targets, energy.Values, new SankeyOptions
        {
            NodeWidth = 15,
            NodePadding = 10,
            Align = SankeyAlign.Justify,
            X0 = 1,
            Y0 = 5,
            X1 = Width - 1,
            Y1 = Height - 5,
        });
        for (var node = 0; node < layout.NodeCount; node++)
        {
            scene.Path("flow-node s" + (node % 8 + 1), PathText.Rect(layout.X0[node], layout.Y0[node], layout.X1[node] - layout.X0[node], layout.Y1[node] - layout.Y0[node]), energy.Names[node]);
        }

        for (var link = 0; link < layout.LinkCount; link++)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new SvgPathWriter(buffer, null);
            layout.WriteLink(ref writer, link);
            scene.Stroke("flow s" + (layout.Source[link] % 8 + 1), Encoding.UTF8.GetString(buffer.WrittenSpan), Math.Max(1, layout.LinkWidth[link]), energy.Names[layout.Source[link]] + " → " + energy.Names[layout.Target[link]]);
        }

        for (var node = 0; node < layout.NodeCount; node++)
        {
            var left = layout.X0[node] < Width / 2;
            scene.Text("label", left ? layout.X1[node] + 6 : layout.X0[node] - 6, (layout.Y1[node] + layout.Y0[node]) / 2 + 4, energy.Names[node], left ? "start" : "end");
        }

        return scene;
    }

    private static (Hierarchy Summed, TreeNode[] Nodes) Software(DemoData data)
    {
        var hierarchy = Hierarchy.FromNested(data.Layouts.Software, n => n.Children, out var nodes);
        var values = new double[nodes.Length];
        for (var i = 0; i < nodes.Length; i++)
        {
            values[i] = nodes[i].Children is null ? nodes[i].Value : double.NaN;
        }

        return (hierarchy.Sum(values), nodes);
    }

    private static int Category(Hierarchy hierarchy, int node)
    {
        while (hierarchy.Depths[node] > 1)
        {
            node = hierarchy.Parents[node];
        }

        return hierarchy.Children(hierarchy.Root).IndexOf(node);
    }

    private static ArcShape Arc(NodeRectangles parts, int node, double radius) =>
        new(parts.Y0[node], parts.Y1[node] - 1, parts.X0[node], parts.X1[node])
        {
            PadAngle = Math.Min((parts.X1[node] - parts.X0[node]) / 2, 0.005),
            PadRadius = radius / 2,
        };
}
