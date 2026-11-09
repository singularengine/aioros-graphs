using System.Buffers;
using System.Text;
using Aioros.Graphs.Arrays;
using Aioros.Graphs.Axes;
using Aioros.Graphs.Colors;
using Aioros.Graphs.Formatting;
using Aioros.Graphs.Scales;
using Aioros.Graphs.Shapes;
using Aioros.Graphs.Svg;
using Aioros.Graphs.Time;

namespace Aioros.Graphs.Demo;

internal static class MoreCharts
{
    private const double Width = 640;
    private const double Height = 360;
    private const double Left = 48;
    private const double Right = Width - 24;
    private const double Bottom = Height - 30;

    public static Scene Histogram(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var bins = Arrays.Histogram.Compute(data.Heights);
        var x = LinearScale.Create([bins[0].X0, bins[^1].X1], [Left, Right]);
        var max = 0;
        foreach (var bin in bins)
        {
            max = Math.Max(max, bin.Count);
        }

        var y = LinearScale.Create([0, max], [Bottom, 20]).Nice();
        Charts.YGrid(scene, y, Height / 50, null);
        var xs = new double[bins.Length];
        var ys = new double[bins.Length];
        var widths = new double[bins.Length];
        var heights = new double[bins.Length];
        for (var i = 0; i < bins.Length; i++)
        {
            xs[i] = x.Map(bins[i].X0) + 1;
            ys[i] = y.Map(bins[i].Count);
            widths[i] = Math.Max(0, x.Map(bins[i].X1) - x.Map(bins[i].X0) - 1);
            heights[i] = y.Map(0) - y.Map(bins[i].Count);
        }

        scene.Path("bar s1", PathText.Rects(xs, ys, widths, heights));
        var ticks = x.Ticks(Width / 80);
        var format = x.TickFormat(Width / 80);
        scene.AxisBottom(Charts.Map(x, ticks), [.. ticks.Select(format.Format)], Bottom, Left, Right);
        scene.Text("unit", Right, Bottom - 6, "height (cm)", "end");
        return scene;
    }

    public static Scene Scatter(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var f = data.Flowers;
        var x = LinearScale.Create([Extent.Of(f.X).Min, Extent.Of(f.X).Max], [Left, Right]).Nice();
        var y = LinearScale.Create([Extent.Of(f.Y).Min, Extent.Of(f.Y).Max], [Bottom, 36]).Nice();
        var r = PowScale.Sqrt([0, Extent.Of(f.Size).Max], [0, 7]);
        SymbolType[] types = [SymbolType.Circle, SymbolType.Star, SymbolType.Triangle];
        Charts.YGrid(scene, y, Height / 50, ".1f");
        for (var i = 0; i < f.X.Length; i++)
        {
            var radius = r.Map(f.Size[i]);
            scene.Path("symbol s" + (f.Group[i] + 1), PathText.Symbol(types[f.Group[i]], radius * radius * 4), null, [x.Map(f.X[i]), y.Map(f.Y[i])]);
        }

        var ticks = x.Ticks(Width / 80);
        var format = x.TickFormat(Width / 80, ".1f");
        scene.AxisBottom(Charts.Map(x, ticks), [.. ticks.Select(format.Format)], Bottom, Left, Right);
        scene.Legend(f.Keys, Left, 14);
        return scene;
    }

    public static Scene Radial(DemoData data)
    {
        const double cx = 320;
        const double cy = 184;
        var scene = new Scene(Width, Height);
        var t = data.Temperatures;
        var x = UtcScale.Create([t.Dates[0], t.Dates[^1]], [0, 2 * Math.PI]);
        var y = RadialScale.Create([Extent.Of(t.Low).Min, Extent.Of(t.High).Max], [40, 150]);
        var ticks = y.Ticks(5);
        var format = y.TickFormat(5, "+");
        foreach (var tick in ticks)
        {
            scene.Circle("ring", cx, cy, y.Map(tick));
        }

        var angles = Charts.Map(x, t.Dates);
        var inner = new double[t.Low.Length];
        var outer = new double[t.High.Length];
        y.Map(t.Low, inner);
        y.Map(t.High, outer);
        scene.Path("radial s1", PathText.AreaRadial(Curve.LinearClosed, angles, inner, outer), null, [cx, cy]);
        foreach (var tick in ticks)
        {
            scene.Text("label", cx, cy - y.Map(tick) - 2, format.Format(tick) + "°", "middle");
        }

        var month = UtcTimeFormat.Create("%b");
        foreach (var m in x.Ticks(UtcInterval.Month))
        {
            var a = x.Map(m);
            scene.Text("label", cx + 166 * Math.Sin(a), cy - 166 * Math.Cos(a) + 4, month.Format(m), "middle");
        }

        return scene;
    }

    public static Scene Heatmap(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var g = data.Anomalies;
        var x = BandScale.Create(g.Columns.Length, Left, Right);
        var y = BandScale.Create(g.Rows.Length, 16, Bottom);
        var max = 0.0;
        foreach (var row in g.Values)
        {
            foreach (var value in row)
            {
                max = Math.Max(max, Math.Abs(value));
            }
        }

        var scheme = ColorSchemes.Interpolator(BrewerScheme.RdBu);
        var color = DivergingScale<Rgb, SchemeInterpolator>.Create([max, 0, -max], scheme);
        for (var i = 0; i < g.Rows.Length; i++)
        {
            for (var j = 0; j < g.Columns.Length; j++)
            {
                scene.Cell("cell", x.Map(j), y.Map(i), x.Bandwidth, y.Bandwidth, scheme.Format(color.Position(g.Values[i][j])));
            }

            scene.Text("label", Left - 6, y.Map(i) + y.Bandwidth / 2 + 4, g.Rows[i], "end");
        }

        for (var j = 0; j < g.Columns.Length; j += 5)
        {
            scene.Text("label", x.Map(j) + x.Bandwidth / 2, Bottom + 17, JsNumber.ToShortestString(g.Columns[j]), "middle");
        }

        return scene;
    }

    public static Scene LogLine(DemoData data)
    {
        var scene = new Scene(Width, Height);
        var s = data.Transistors;
        var (min, max) = Extent.Of(s.Values);
        var x = LinearScale.Create([s.Dates[0], s.Dates[^1]], [Left + 12, Right]);
        var y = LogScale.Create([min, max], [Bottom, 20]).Nice();
        var ys = new double[s.Values.Length];
        y.Map(s.Values, ys);
        scene.Path("line s1", PathText.Line(Curve.MonotoneX, Charts.Map(x, s.Dates), ys));
        var left = Axis.Create(AxisOrientation.Left, y, 10, "~s");
        var bottom = Axis.Create(AxisOrientation.Bottom, x, Width / 80, "d");
        scene.Group("axis", Left, 0, Markup(left), left.Ticks.Select(t => t.Label));
        scene.Group("axis", 0, Bottom, Markup(bottom), bottom.Ticks.Select(t => t.Label));
        return scene;
    }

    private static string Markup(AxisLayout axis)
    {
        var buffer = new ArrayBufferWriter<byte>();
        SvgAxisWriter.Write(buffer, axis);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
