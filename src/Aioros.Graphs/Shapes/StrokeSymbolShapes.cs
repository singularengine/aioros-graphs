using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes;

internal static class StrokeSymbolShapes
{
    private static readonly double Sqrt3 = Math.Sqrt(3);

    public static void Write<TSink>(ref PathContext<TSink> context, SymbolType type, double size, double cx, double cy)
        where TSink : IPathSink
    {
        switch (type)
        {
            case SymbolType.Plus:
                var plus = Math.Sqrt(size - Math.Min(size / 7, 2)) * 0.87559;
                Segment(ref context, -plus, 0, plus, 0, cx, cy);
                Segment(ref context, 0, plus, 0, -plus, cx, cy);
                break;
            case SymbolType.Times:
                var times = Math.Sqrt(size - Math.Min(size / 6, 1.7)) * 0.6189;
                Segment(ref context, -times, -times, times, times, cx, cy);
                Segment(ref context, -times, times, times, -times, cx, cy);
                break;
            case SymbolType.Triangle2:
                Triangle2(ref context, size, cx, cy);
                break;
            case SymbolType.Asterisk:
                Asterisk(ref context, size, cx, cy);
                break;
            case SymbolType.Square2:
                var r = Math.Sqrt(size) * 0.4431;
                Quad(ref context, (r, r), (r, -r), (-r, -r), (-r, r), cx, cy);
                break;
            case SymbolType.Diamond2:
                var d = Math.Sqrt(size) * 0.62625;
                Quad(ref context, (0, -d), (d, 0), (0, d), (-d, 0), cx, cy);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Not a symbol type.");
        }
    }

    private static void Triangle2<TSink>(ref PathContext<TSink> context, double size, double cx, double cy)
        where TSink : IPathSink
    {
        var s = Math.Sqrt(size) * 0.6824;
        var t = s / 2;
        var u = s * Sqrt3 / 2;
        SymbolShapes.MoveTo(ref context, 0, -s, cx, cy);
        SymbolShapes.LineTo(ref context, u, t, cx, cy);
        SymbolShapes.LineTo(ref context, -u, t, cx, cy);
        context.ClosePath();
    }

    private static void Asterisk<TSink>(ref PathContext<TSink> context, double size, double cx, double cy)
        where TSink : IPathSink
    {
        var r = Math.Sqrt(size + Math.Min(size / 28, 0.75)) * 0.59436;
        var t = r / 2;
        var u = t * Sqrt3;
        Segment(ref context, 0, r, 0, -r, cx, cy);
        Segment(ref context, -u, -t, u, t, cx, cy);
        Segment(ref context, -u, t, u, -t, cx, cy);
    }

    private static void Segment<TSink>(ref PathContext<TSink> context, double x0, double y0, double x1, double y1, double cx, double cy)
        where TSink : IPathSink
    {
        SymbolShapes.MoveTo(ref context, x0, y0, cx, cy);
        SymbolShapes.LineTo(ref context, x1, y1, cx, cy);
    }

    private static void Quad<TSink>(ref PathContext<TSink> context, (double X, double Y) a, (double X, double Y) b, (double X, double Y) c, (double X, double Y) d, double cx, double cy)
        where TSink : IPathSink
    {
        SymbolShapes.MoveTo(ref context, a.X, a.Y, cx, cy);
        SymbolShapes.LineTo(ref context, b.X, b.Y, cx, cy);
        SymbolShapes.LineTo(ref context, c.X, c.Y, cx, cy);
        SymbolShapes.LineTo(ref context, d.X, d.Y, cx, cy);
        context.ClosePath();
    }
}
