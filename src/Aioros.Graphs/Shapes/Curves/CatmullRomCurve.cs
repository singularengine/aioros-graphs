using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>
/// A cubic Catmull–Rom spline parameterized by <c>alpha</c>, as d3's <c>curveCatmullRom.alpha(alpha)</c>: 0.5 is
/// centripetal, 0 uniform and 1 chordal.
/// </summary>
public struct CatmullRomCurve : ICurve
{
    private const double Epsilon = 1e-12;

    private readonly double alpha;
    private LineState line;
    private int point;
    private double x0;
    private double x1;
    private double x2;
    private double y0;
    private double y1;
    private double y2;
    private double l01A;
    private double l12A;
    private double l23A;
    private double l01TwoA;
    private double l12TwoA;
    private double l23TwoA;

    /// <summary>Creates a Catmull–Rom curve.</summary>
    /// <param name="alpha">The parameterization, from 0 to 1.</param>
    public CatmullRomCurve(double alpha)
    {
        this.alpha = alpha;
    }

    /// <inheritdoc />
    public void AreaStart() => line = LineState.Topline;

    /// <inheritdoc />
    public void AreaEnd() => line = LineState.Line;

    /// <inheritdoc />
    public void LineStart()
    {
        x0 = x1 = x2 = y0 = y1 = y2 = double.NaN;
        l01A = l12A = l23A = l01TwoA = l12TwoA = l23TwoA = 0;
        point = 0;
    }

    /// <inheritdoc />
    public void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink
    {
        if (point == 2)
        {
            context.LineTo(x2, y2);
        }
        else if (point == 3)
        {
            Point(ref context, x2, y2);
        }

        line.Close(ref context, point == 1);
        line = line.Next();
    }

    /// <inheritdoc />
    public void Point<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        if (point != 0)
        {
            var x23 = x2 - x;
            var y23 = y2 - y;
            l23TwoA = Math.Pow(x23 * x23 + y23 * y23, alpha);
            l23A = Math.Sqrt(l23TwoA);
        }

        switch (point)
        {
            case 0:
                point = 1;
                line.Start(ref context, x, y);
                break;
            case 1:
                point = 2;
                break;
            default:
                point = 3;
                Segment(ref context, x, y);
                break;
        }

        l01A = l12A;
        l12A = l23A;
        l01TwoA = l12TwoA;
        l12TwoA = l23TwoA;
        x0 = x1;
        x1 = x2;
        x2 = x;
        y0 = y1;
        y1 = y2;
        y2 = y;
    }

    private readonly void Segment<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        var (cx1, cy1, cx2, cy2) = (x1, y1, x2, y2);
        if (l01A > Epsilon)
        {
            var a = 2 * l01TwoA + 3 * l01A * l12A + l12TwoA;
            var n = 3 * l01A * (l01A + l12A);
            cx1 = (cx1 * a - x0 * l12TwoA + x2 * l01TwoA) / n;
            cy1 = (cy1 * a - y0 * l12TwoA + y2 * l01TwoA) / n;
        }

        if (l23A > Epsilon)
        {
            var b = 2 * l23TwoA + 3 * l23A * l12A + l12TwoA;
            var m = 3 * l23A * (l23A + l12A);
            cx2 = (cx2 * b + x1 * l23TwoA - x * l12TwoA) / m;
            cy2 = (cy2 * b + y1 * l23TwoA - y * l12TwoA) / m;
        }

        context.BezierCurveTo(cx1, cy1, cx2, cy2, x2, y2);
    }
}
