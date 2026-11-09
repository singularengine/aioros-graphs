using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>A cubic cardinal spline from the second to the next-to-last point, as d3's <c>curveCardinalOpen.tension(tension)</c>.</summary>
public struct CardinalOpenCurve : ICurve
{
    private readonly double k;
    private LineState line;
    private int point;
    private double x0;
    private double x1;
    private double x2;
    private double y0;
    private double y1;
    private double y2;

    /// <summary>Creates an open cardinal curve.</summary>
    /// <param name="tension">0 gives a Catmull–Rom spline, 1 straight lines.</param>
    public CardinalOpenCurve(double tension)
    {
        k = (1 - tension) / 6;
    }

    /// <inheritdoc />
    public void AreaStart() => line = LineState.Topline;

    /// <inheritdoc />
    public void AreaEnd() => line = LineState.Line;

    /// <inheritdoc />
    public void LineStart()
    {
        x0 = x1 = x2 = y0 = y1 = y2 = double.NaN;
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
                line.Start(ref context, x2, y2);
                break;
            default:
                point = 4;
                context.BezierCurveTo(x1 + k * (x2 - x0), y1 + k * (y2 - y0), x2 + k * (x1 - x), y2 + k * (y1 - y), x2, y2);
                break;
        }

        x0 = x1;
        x1 = x2;
        x2 = x;
        y0 = y1;
        y1 = y2;
        y2 = y;
    }
}
