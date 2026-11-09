using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>A Catmull–Rom spline from the second to the next-to-last point, as d3's <c>curveCatmullRomOpen.alpha(alpha)</c> with a nonzero alpha.</summary>
public struct CatmullRomOpenCurve : ICurve
{
    private CatmullRomHistory history;
    private LineState line;
    private int point;

    /// <summary>Creates an open Catmull–Rom curve.</summary>
    /// <param name="alpha">The parameterization, from 0 to 1.</param>
    public CatmullRomOpenCurve(double alpha)
    {
        history = new CatmullRomHistory(alpha);
    }

    /// <inheritdoc />
    public void AreaStart() => line = LineState.Topline;

    /// <inheritdoc />
    public void AreaEnd() => line = LineState.Line;

    /// <inheritdoc />
    public void LineStart()
    {
        history.Reset();
        point = 0;
    }

    /// <inheritdoc />
    public void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink
    {
        line.Close(ref context, point == 3);
        line = line.Next();
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
                break;
            case 1:
                point = 2;
                break;
            case 2:
                point = 3;
                line.Start(ref context, history.X2, history.Y2);
                break;
            default:
                point = 4;
                history.Segment(ref context, x, y);
                break;
        }

        history.Shift(x, y);
    }
}
