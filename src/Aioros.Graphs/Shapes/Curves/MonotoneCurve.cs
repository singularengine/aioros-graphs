using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>
/// A cubic spline that preserves monotonicity, as d3's <c>curveMonotoneX</c> (monotone in y, for x ascending) and
/// <c>curveMonotoneY</c> (the same with the axes swapped), after Steffen (1990).
/// </summary>
public struct MonotoneCurve : ICurve
{
    private readonly bool swapped;
    private LineState line;
    private int point;
    private double x0;
    private double x1;
    private double y0;
    private double y1;
    private double t0;

    /// <summary>Creates a monotone curve.</summary>
    /// <param name="monotoneInX">True for monotoneY: the curve is monotone along y with x as the value.</param>
    public MonotoneCurve(bool monotoneInX)
    {
        swapped = monotoneInX;
    }

    /// <inheritdoc />
    public void AreaStart() => line = LineState.Topline;

    /// <inheritdoc />
    public void AreaEnd() => line = LineState.Line;

    /// <inheritdoc />
    public void LineStart()
    {
        x0 = x1 = y0 = y1 = t0 = double.NaN;
        point = 0;
    }

    /// <inheritdoc />
    public void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink
    {
        if (point == 2)
        {
            LineTo(ref context, x1, y1);
        }
        else if (point == 3)
        {
            Segment(ref context, t0, Slope2(t0));
        }

        line.Close(ref context, point == 1);
        line = line.Next();
    }

    /// <inheritdoc />
    public void Point<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        if (swapped)
        {
            (x, y) = (y, x);
        }

        if (x == x1 && y == y1)
        {
            return;
        }

        var t1 = double.NaN;
        switch (point)
        {
            case 0:
                point = 1;
                Start(ref context, x, y);
                break;
            case 1:
                point = 2;
                break;
            case 2:
                point = 3;
                t1 = Slope3(x, y);
                Segment(ref context, Slope2(t1), t1);
                break;
            default:
                t1 = Slope3(x, y);
                Segment(ref context, t0, t1);
                break;
        }

        x0 = x1;
        x1 = x;
        y0 = y1;
        y1 = y;
        t0 = t1;
    }

    private static double Sign(double x) => x < 0 ? -1 : 1;

    private static double Divisor(double h, double other) => h != 0 && !double.IsNaN(h) ? h : other < 0 ? -0.0 : 0.0;

    private readonly double Slope3(double x2, double y2)
    {
        var h0 = x1 - x0;
        var h1 = x2 - x1;
        var s0 = (y1 - y0) / Divisor(h0, h1);
        var s1 = (y2 - y1) / Divisor(h1, h0);
        var p = (s0 * h1 + s1 * h0) / (h0 + h1);
        var slope = (Sign(s0) + Sign(s1)) * Math.Min(Math.Min(Math.Abs(s0), Math.Abs(s1)), 0.5 * Math.Abs(p));
        return double.IsNaN(slope) ? 0 : slope;
    }

    private readonly double Slope2(double t)
    {
        var h = x1 - x0;
        return h != 0 && !double.IsNaN(h) ? (3 * (y1 - y0) / h - t) / 2 : t;
    }

    private readonly void Segment<TSink>(ref PathContext<TSink> context, double from, double to)
        where TSink : IPathSink
    {
        var dx = (x1 - x0) / 3;
        var (ax, ay, bx, by) = (x0 + dx, y0 + dx * from, x1 - dx, y1 - dx * to);
        if (swapped)
        {
            context.BezierCurveTo(ay, ax, by, bx, y1, x1);
        }
        else
        {
            context.BezierCurveTo(ax, ay, bx, by, x1, y1);
        }
    }

    private readonly void Start<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        if (swapped)
        {
            line.Start(ref context, y, x);
        }
        else
        {
            line.Start(ref context, x, y);
        }
    }

    private readonly void LineTo<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        if (swapped)
        {
            context.LineTo(y, x);
        }
        else
        {
            context.LineTo(x, y);
        }
    }
}
