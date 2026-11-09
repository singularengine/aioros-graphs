using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes;

internal static class SymbolShapes
{
    private const double Ka = 0.89081309152928522810;
    private static readonly double Sqrt3 = Math.Sqrt(3);
    private static readonly double Tan30 = Math.Sqrt(1.0 / 3);
    private static readonly double Tan30Twice = Tan30 * 2;
    private static readonly double Kr = Math.Sin(Math.PI / 10) / Math.Sin(7 * Math.PI / 10);
    private static readonly double Kx = Math.Sin(2 * Math.PI / 10) * Kr;
    private static readonly double Ky = -Math.Cos(2 * Math.PI / 10) * Kr;
    private static readonly double WyeS = Math.Sqrt(3) / 2;
    private static readonly double WyeK = 1 / Math.Sqrt(12);
    private static readonly double WyeA = (WyeK / 2 + 1) * 3;

    public static void MoveTo<TSink>(ref PathContext<TSink> context, double x, double y, double cx, double cy)
        where TSink : IPathSink => context.MoveTo(x + cx, y + cy);

    public static void LineTo<TSink>(ref PathContext<TSink> context, double x, double y, double cx, double cy)
        where TSink : IPathSink => context.LineTo(x + cx, y + cy);

    public static void Circle<TSink>(ref PathContext<TSink> context, double size, double cx, double cy)
        where TSink : IPathSink
    {
        var r = Math.Sqrt(size / Math.PI);
        MoveTo(ref context, r, 0, cx, cy);
        context.Arc(0 + cx, 0 + cy, r, 0, 2 * Math.PI);
    }

    public static void Cross<TSink>(ref PathContext<TSink> context, double size, double cx, double cy)
        where TSink : IPathSink
    {
        var r = Math.Sqrt(size / 5) / 2;
        MoveTo(ref context, -3 * r, -r, cx, cy);
        LineTo(ref context, -r, -r, cx, cy);
        LineTo(ref context, -r, -3 * r, cx, cy);
        LineTo(ref context, r, -3 * r, cx, cy);
        LineTo(ref context, r, -r, cx, cy);
        LineTo(ref context, 3 * r, -r, cx, cy);
        LineTo(ref context, 3 * r, r, cx, cy);
        LineTo(ref context, r, r, cx, cy);
        LineTo(ref context, r, 3 * r, cx, cy);
        LineTo(ref context, -r, 3 * r, cx, cy);
        LineTo(ref context, -r, r, cx, cy);
        LineTo(ref context, -3 * r, r, cx, cy);
        context.ClosePath();
    }

    public static void Diamond<TSink>(ref PathContext<TSink> context, double size, double cx, double cy)
        where TSink : IPathSink
    {
        var y = Math.Sqrt(size / Tan30Twice);
        var x = y * Tan30;
        MoveTo(ref context, 0, -y, cx, cy);
        LineTo(ref context, x, 0, cx, cy);
        LineTo(ref context, 0, y, cx, cy);
        LineTo(ref context, -x, 0, cx, cy);
        context.ClosePath();
    }

    public static void Square<TSink>(ref PathContext<TSink> context, double size, double cx, double cy)
        where TSink : IPathSink
    {
        var w = Math.Sqrt(size);
        var x = -w / 2;
        context.Rect(x + cx, x + cy, w, w);
    }

    public static void Star<TSink>(ref PathContext<TSink> context, double size, double cx, double cy)
        where TSink : IPathSink
    {
        var r = Math.Sqrt(size * Ka);
        var x = Kx * r;
        var y = Ky * r;
        MoveTo(ref context, 0, -r, cx, cy);
        LineTo(ref context, x, y, cx, cy);
        for (var i = 1; i < 5; i++)
        {
            var a = 2 * Math.PI * i / 5;
            var c = Math.Cos(a);
            var s = Math.Sin(a);
            LineTo(ref context, s * r, -c * r, cx, cy);
            LineTo(ref context, c * x - s * y, s * x + c * y, cx, cy);
        }

        context.ClosePath();
    }

    public static void Triangle<TSink>(ref PathContext<TSink> context, double size, double cx, double cy)
        where TSink : IPathSink
    {
        var y = -Math.Sqrt(size / (Sqrt3 * 3));
        MoveTo(ref context, 0, y * 2, cx, cy);
        LineTo(ref context, -Sqrt3 * y, -y, cx, cy);
        LineTo(ref context, Sqrt3 * y, -y, cx, cy);
        context.ClosePath();
    }

    public static void Wye<TSink>(ref PathContext<TSink> context, double size, double cx, double cy)
        where TSink : IPathSink
    {
        const double c = -0.5;
        var s = WyeS;
        var r = Math.Sqrt(size / WyeA);
        var (x0, y0) = (r / 2, r * WyeK);
        var (x1, y1) = (x0, r * WyeK + r);
        var (x2, y2) = (-x1, y1);
        MoveTo(ref context, x0, y0, cx, cy);
        LineTo(ref context, x1, y1, cx, cy);
        LineTo(ref context, x2, y2, cx, cy);
        LineTo(ref context, c * x0 - s * y0, s * x0 + c * y0, cx, cy);
        LineTo(ref context, c * x1 - s * y1, s * x1 + c * y1, cx, cy);
        LineTo(ref context, c * x2 - s * y2, s * x2 + c * y2, cx, cy);
        LineTo(ref context, c * x0 + s * y0, c * y0 - s * x0, cx, cy);
        LineTo(ref context, c * x1 + s * y1, c * y1 - s * x1, cx, cy);
        LineTo(ref context, c * x2 + s * y2, c * y2 - s * x2, cx, cy);
        context.ClosePath();
    }
}
