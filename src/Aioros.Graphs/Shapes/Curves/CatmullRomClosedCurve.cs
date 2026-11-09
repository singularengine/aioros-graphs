using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>A closed Catmull–Rom spline, as d3's <c>curveCatmullRomClosed.alpha(alpha)</c> with a nonzero alpha.</summary>
public struct CatmullRomClosedCurve : ICurve
{
    private CatmullRomHistory history;
    private int point;
    private double x3;
    private double x4;
    private double x5;
    private double y3;
    private double y4;
    private double y5;

    /// <summary>Creates a closed Catmull–Rom curve.</summary>
    /// <param name="alpha">The parameterization, from 0 to 1.</param>
    public CatmullRomClosedCurve(double alpha)
    {
        history = new CatmullRomHistory(alpha);
    }

    /// <inheritdoc />
    public readonly void AreaStart()
    {
    }

    /// <inheritdoc />
    public readonly void AreaEnd()
    {
    }

    /// <inheritdoc />
    public void LineStart()
    {
        history.Reset();
        x3 = x4 = x5 = y3 = y4 = y5 = double.NaN;
        point = 0;
    }

    /// <inheritdoc />
    public void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink
    {
        switch (point)
        {
            case 1:
                context.MoveTo(x3, y3);
                context.ClosePath();
                break;
            case 2:
                context.LineTo(x3, y3);
                context.ClosePath();
                break;
            case 3:
                Point(ref context, x3, y3);
                Point(ref context, x4, y4);
                Point(ref context, x5, y5);
                break;
        }
    }

    /// <inheritdoc />
    public void Point<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        if (point != 0)
        {
            history.Measure(x, y);
        }

        switch (point)
        {
            case 0:
                point = 1;
                x3 = x;
                y3 = y;
                break;
            case 1:
                point = 2;
                x4 = x;
                y4 = y;
                context.MoveTo(x, y);
                break;
            case 2:
                point = 3;
                x5 = x;
                y5 = y;
                break;
            default:
                history.Segment(ref context, x, y);
                break;
        }

        history.Shift(x, y);
    }
}
