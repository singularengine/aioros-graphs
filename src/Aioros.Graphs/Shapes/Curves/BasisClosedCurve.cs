using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>A closed cubic basis spline, as d3's <c>curveBasisClosed</c>.</summary>
public struct BasisClosedCurve : ICurve
{
    private int point;
    private double x0;
    private double x1;
    private double x2;
    private double x3;
    private double x4;
    private double y0;
    private double y1;
    private double y2;
    private double y3;
    private double y4;

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
        x0 = x1 = x2 = x3 = x4 = y0 = y1 = y2 = y3 = y4 = double.NaN;
        point = 0;
    }

    /// <inheritdoc />
    public void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink
    {
        switch (point)
        {
            case 1:
                context.MoveTo(x2, y2);
                context.ClosePath();
                break;
            case 2:
                context.MoveTo((x2 + 2 * x3) / 3, (y2 + 2 * y3) / 3);
                context.LineTo((x3 + 2 * x2) / 3, (y3 + 2 * y2) / 3);
                context.ClosePath();
                break;
            case 3:
                Point(ref context, x2, y2);
                Point(ref context, x3, y3);
                Point(ref context, x4, y4);
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
                x2 = x;
                y2 = y;
                break;
            case 1:
                point = 2;
                x3 = x;
                y3 = y;
                break;
            case 2:
                point = 3;
                x4 = x;
                y4 = y;
                context.MoveTo((x0 + 4 * x1 + x) / 6, (y0 + 4 * y1 + y) / 6);
                break;
            default:
                Segment(ref context, x, y);
                break;
        }

        x0 = x1;
        x1 = x;
        y0 = y1;
        y1 = y;
    }

    private readonly void Segment<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink =>
        context.BezierCurveTo(
            (2 * x0 + x1) / 3,
            (2 * y0 + y1) / 3,
            (x0 + 2 * x1) / 3,
            (y0 + 2 * y1) / 3,
            (x0 + 4 * x1 + x) / 6,
            (y0 + 4 * y1 + y) / 6);
}
