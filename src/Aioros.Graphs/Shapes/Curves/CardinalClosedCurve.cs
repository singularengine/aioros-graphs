using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>A closed cubic cardinal spline, as d3's <c>curveCardinalClosed.tension(tension)</c>.</summary>
public struct CardinalClosedCurve : ICurve
{
    private readonly double k;
    private int point;
    private double x0;
    private double x1;
    private double x2;
    private double x3;
    private double x4;
    private double x5;
    private double y0;
    private double y1;
    private double y2;
    private double y3;
    private double y4;
    private double y5;

    /// <summary>Creates a closed cardinal curve.</summary>
    /// <param name="tension">0 gives a Catmull–Rom spline, 1 straight lines.</param>
    public CardinalClosedCurve(double tension)
    {
        k = (1 - tension) / 6;
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
        x0 = x1 = x2 = x3 = x4 = x5 = y0 = y1 = y2 = y3 = y4 = y5 = double.NaN;
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
