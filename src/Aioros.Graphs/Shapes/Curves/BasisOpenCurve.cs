using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>A cubic basis spline that starts and ends inside the first and last points, as d3's <c>curveBasisOpen</c>.</summary>
public struct BasisOpenCurve : ICurve
{
    private LineState line;
    private int point;
    private double x0;
    private double x1;
    private double y0;
    private double y1;

    /// <inheritdoc />
    public void AreaStart() => line = LineState.Topline;

    /// <inheritdoc />
    public void AreaEnd() => line = LineState.Line;

    /// <inheritdoc />
    public void LineStart()
    {
        x0 = x1 = y0 = y1 = double.NaN;
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
                line.Start(ref context, (x0 + 4 * x1 + x) / 6, (y0 + 4 * y1 + y) / 6);
                break;
            default:
                point = 4;
                context.BezierCurveTo(
                    (2 * x0 + x1) / 3,
                    (2 * y0 + y1) / 3,
                    (x0 + 2 * x1) / 3,
                    (y0 + 2 * y1) / 3,
                    (x0 + 4 * x1 + x) / 6,
                    (y0 + 4 * y1 + y) / 6);
                break;
        }

        x0 = x1;
        x1 = x;
        y0 = y1;
        y1 = y;
    }
}
