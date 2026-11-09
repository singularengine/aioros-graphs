using Aioros.Graphs.Arrays;
using Aioros.Graphs.Formatting;
using Aioros.Graphs.Scales;
using Aioros.Graphs.Shapes;

namespace Aioros.Graphs.Demo;

internal sealed record Chart(string Id, string Title, string Description, Func<DemoData, Scene> Draw);

internal static class Charts
{
    private const double Width = 640;
    private const double Height = 360;
    private const double Left = 48;
    private const double Right = Width - 24;
    private const double Bottom = Height - 30;

    public static Chart[] All { get; } =
    [
        new("line", "Daily close, two years", "Line, UTC time scale, multi-scale time format, nice linear y", StockLine),
        new("area", "Weekly visitors, 2024", "Area and line with curveMonotoneX, SI-prefix ticks", VisitorsArea),
        new("stacked-area", "Monthly sales by category", "stack() with order and offset none, five areas", SalesStack),
        new("stacked-bars", "Quarterly revenue by region", "Band scale with padding, stacked bars as path rects, currency ticks", RevenueBars),
        new("donut", "Browser share", "pie() with pad angle, arc() with corner radius, labels at centroids", BrowserDonut),
        new("streamgraph", "Streamgraph of eight series", "stack() with insideOut order and wiggle offset, curveBasis", Streamgraph),
        new("curves", "One series, six curves", "linear, monotoneX, natural, step, cardinal and catmullRom on the same points", CurveGallery),
        new("histogram", "Heights of 1,000 adults", "bin() with Sturges' thresholds, bars as path rects, nice linear y", MoreCharts.Histogram),
        new("scatter", "Sepal size by species", "Symbols (circle, star, triangle) sized by a sqrt scale, one path per point", MoreCharts.Scatter),
        new("radial", "Daily temperature range, 2023", "areaRadial with curveLinearClosed, scaleRadial, UTC angle scale, month labels", MoreCharts.Radial),
        new("heatmap", "Monthly temperature anomalies", "Band scales and scaleDiverging with interpolateRdBu, 360 colored cells", MoreCharts.Heatmap),
        new("log-line", "Transistors per chip", "scaleLog with nice and its label filter, d3-axis markup from Axis and SvgAxisWriter", MoreCharts.LogLine),
        new("treemap", "Treemap of a software library", "hierarchy().sum().sort(), treemap() with squarify, padding 1 and round, one rect path per leaf", LayoutCharts.Treemap),
        new("pack", "Circle packing", "pack() with padding 3 over the same hierarchy, every node as a d3-path circle", LayoutCharts.Pack),
        new("sunburst", "Sunburst", "partition() sized to 2π × radius, arc() with pad angle and pad radius per node", LayoutCharts.Sunburst),
        new("tidy-tree", "Tidy tree of the packages", "tree() (Reingold–Tilford) with linkHorizontal() between nodes", LayoutCharts.TidyTree),
        new("chord", "Trade between regions", "chord() with pad angle and descending subgroups and chords, ribbon() with pad angle, group arcs", LayoutCharts.ChordDiagram),
        new("sankey", "Energy flows", "d3-sankey 0.12.3: sankey() with nodeAlign justify, nodeWidth 15, nodePadding 10 and an extent, sankeyLinkHorizontal() stroked by link width", LayoutCharts.SankeyDiagram),
        new("force", "Force-directed graph", "forceSimulation with forceLink, forceManyBody (strength −60), forceX and forceY, 300 ticks from d3's phyllotaxis start", LayoutCharts.ForceGraph),
    ];

    private static Scene StockLine(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var s = data.Stock;
        var (min, max) = Extent.Of(s.Values);
        var x = UtcScale.Create([s.Dates[0], s.Dates[^1]], [Left, Right]);
        var y = LinearScale.Create([min, max], [Bottom, 20]).Nice();
        YGrid(scene, y, Height / 50, null);
        scene.Path("line s1", PathText.Line(Curve.Linear, Map(x, s.Dates), Map(y, s.Values)));
        XTime(scene, x);
        scene.Text("unit", 8, 12, "USD");
        return scene;
    }

    private static Scene VisitorsArea(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var s = data.Visitors;
        var x = UtcScale.Create([s.Dates[0], s.Dates[^1]], [Left, Right]);
        var y = LinearScale.Create([0, Extent.Of(s.Values).Max], [Bottom, 20]).Nice();
        YGrid(scene, y, Height / 50, ".2~s");
        var xs = Map(x, s.Dates);
        var ys = Map(y, s.Values);
        scene.Path("area s1", PathText.Area(Curve.MonotoneX, xs, Fill(xs.Length, y.Map(0)), ys));
        scene.Path("line s1", PathText.Line(Curve.MonotoneX, xs, ys));
        XTime(scene, x);
        return scene;
    }

    private static Scene SalesStack(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var s = data.Sales;
        var (points, n) = Stack(s.Rows, s.Keys.Length, StackOrder.None, StackOffset.None);
        var x = UtcScale.Create([s.Dates[0], s.Dates[^1]], [Left, Right]);
        var y = LinearScale.Create([0, MaxUpper(points)], [Bottom, 36]).Nice();
        YGrid(scene, y, Height / 50, "$,f");
        var xs = Map(x, s.Dates);
        for (var i = 0; i < s.Keys.Length; i++)
        {
            var (lower, upper) = Bands(points, i, n, y);
            scene.Path("band s" + (i + 1), PathText.Area(Curve.Linear, xs, lower, upper), s.Keys[i]);
        }

        XTime(scene, x);
        scene.Legend(s.Keys, Left, 14);
        scene.Text("unit", Right, 14, "thousand USD", "end");
        return scene;
    }

    private static Scene RevenueBars(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var s = data.Revenue;
        var (points, n) = Stack(s.Rows, s.Keys.Length, StackOrder.None, StackOffset.None);
        var x = BandScale.Create(n, Left, Right).WithPadding(0.2);
        var y = LinearScale.Create([0, MaxUpper(points)], [Bottom, 36]).Nice();
        YGrid(scene, y, Height / 50, "$.2~s");
        var xs = new double[n];
        for (var j = 0; j < n; j++)
        {
            xs[j] = x.Map(j);
        }

        for (var i = 0; i < s.Keys.Length; i++)
        {
            var (lower, upper) = Bands(points, i, n, y);
            var heights = new double[n];
            for (var j = 0; j < n; j++)
            {
                heights[j] = lower[j] - upper[j];
            }

            scene.Path("bar s" + (i + 1), PathText.Rects(xs, upper, x.Bandwidth, heights), s.Keys[i]);
        }

        scene.Line("domain", Left, Bottom, Right, Bottom);
        for (var j = 0; j < n; j++)
        {
            scene.Text("label", xs[j] + x.Bandwidth / 2, Bottom + 17, s.Labels[j], "middle");
        }

        scene.Legend(s.Keys, Left, 14);
        return scene;
    }

    private static Scene BrowserDonut(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var s = data.Browsers;
        var total = 0.0;
        foreach (var value in s.Values)
        {
            total += value;
        }

        var percent = NumberFormat.Create(".1%");
        var slices = Pie.Compute(s.Values, new PieOptions { PadAngle = 0.02 });
        const double cx = 200;
        const double cy = 180;
        foreach (var slice in slices)
        {
            var arc = slice.ToArc(90, 160) with { CornerRadius = 4 };
            scene.Path("slice s" + (slice.Index + 1), PathText.Arc(arc), s.Names[slice.Index], [cx, cy]);
        }

        foreach (var slice in slices)
        {
            if (slice.EndAngle - slice.StartAngle > 0.3)
            {
                var (lx, ly) = Arc.Centroid(slice.ToArc(125, 125));
                scene.Text("slice-name", cx + lx, cy + ly - 3, s.Names[slice.Index], "middle");
                scene.Text("slice-value", cx + lx, cy + ly + 11, percent.Format(s.Values[slice.Index] / total), "middle");
            }
        }

        for (var i = 0; i < s.Names.Length; i++)
        {
            var row = 96 + i * 26;
            scene.Rect("swatch s" + (i + 1), 420, row - 10, 12, 12);
            scene.Text("legend", 440, row, s.Names[i]);
            scene.Text("legend-value", 600, row, percent.Format(s.Values[i] / total), "end");
        }

        return scene;
    }

    private static Scene Streamgraph(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var s = data.Stream;
        var (points, n) = Stack(s.Rows, s.Keys.Length, StackOrder.InsideOut, StackOffset.Wiggle);
        var x = LinearScale.Create([0, n - 1], [16, Width - 16]);
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var point in points)
        {
            min = Math.Min(min, point.Lower);
            max = Math.Max(max, point.Upper);
        }

        var y = LinearScale.Create([min, max], [Bottom - 8, 12]);
        var xs = Map(x, s.Dates);
        for (var i = 0; i < s.Keys.Length; i++)
        {
            var (lower, upper) = Bands(points, i, n, y);
            scene.Path("stream s" + (i + 1), PathText.Area(Curve.Basis, xs, lower, upper), s.Keys[i]);
        }

        var ticks = x.Ticks(Width / 80);
        var format = x.TickFormat(Width / 80);
        scene.AxisBottom(Map(x, ticks), [.. ticks.Select(format.Format)], Bottom, 16, Width - 16);
        return scene;
    }

    private static Scene CurveGallery(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var p = data.Curves;
        (string Name, Curve Curve)[] curves =
        [
            ("curveLinear", Curve.Linear), ("curveMonotoneX", Curve.MonotoneX), ("curveNatural", Curve.Natural),
            ("curveStep", Curve.Step), ("curveCardinal", Curve.Cardinal()), ("curveCatmullRom", Curve.CatmullRom()),
        ];
        for (var k = 0; k < curves.Length; k++)
        {
            var px = k % 3 * (Width / 3);
            var py = k / 3 * (Height / 2);
            var x = LinearScale.Create([0, 8], [px + 16, px + Width / 3 - 16]);
            var y = LinearScale.Create([0, 10], [py + Height / 2 - 12, py + 30]);
            var xs = Map(x, p.X);
            var ys = Map(y, p.Y);
            scene.Line("grid", px + 16, py + Height / 2 - 12, px + Width / 3 - 16, py + Height / 2 - 12);
            scene.Text("panel", px + 16, py + 18, curves[k].Name);
            scene.Path("line s1", PathText.Line(curves[k].Curve, xs, ys));
            for (var i = 0; i < xs.Length; i++)
            {
                scene.Circle("dot", xs[i], ys[i], 3);
            }
        }

        return scene;
    }

    public static void YGrid(Scene scene, LinearScale y, double count, string? specifier)
    {
        var ticks = y.Ticks(count);
        var format = y.TickFormat(count, specifier);
        scene.GridLeft(Map(y, ticks), [.. ticks.Select(format.Format)], Left, Right);
    }

    private static void XTime(Scene scene, UtcScale x)
    {
        var ticks = x.Ticks(Width / 100);
        var format = UtcScale.TickFormat();
        scene.AxisBottom(Map(x, ticks), [.. ticks.Select(format)], Bottom, Left, Right);
    }

    private static (StackPoint[] Points, int Length) Stack(double[][] rows, int seriesCount, StackOrder order, StackOffset offset)
    {
        var values = new double[rows.Length * seriesCount];
        for (var j = 0; j < rows.Length; j++)
        {
            rows[j].CopyTo(values, j * seriesCount);
        }

        var points = new StackPoint[values.Length];
        Stacks.Compute(values, seriesCount, points, stackalloc int[seriesCount], order, offset);
        return (points, rows.Length);
    }

    private static (double[] Lower, double[] Upper) Bands(StackPoint[] points, int series, int n, LinearScale y)
    {
        var lower = new double[n];
        var upper = new double[n];
        for (var j = 0; j < n; j++)
        {
            lower[j] = y.Map(points[series * n + j].Lower);
            upper[j] = y.Map(points[series * n + j].Upper);
        }

        return (lower, upper);
    }

    private static double MaxUpper(StackPoint[] points)
    {
        var max = double.NegativeInfinity;
        foreach (var point in points)
        {
            max = Math.Max(max, point.Upper);
        }

        return max;
    }

    public static double[] Map(LinearScale scale, double[] values)
    {
        var mapped = new double[values.Length];
        scale.Map(values, mapped);
        return mapped;
    }

    public static double[] Map(UtcScale scale, double[] values)
    {
        var mapped = new double[values.Length];
        scale.Map(values, mapped);
        return mapped;
    }

    private static double[] Fill(int length, double value)
    {
        var values = new double[length];
        Array.Fill(values, value);
        return values;
    }
}
