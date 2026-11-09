using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>Cubic Bézier bumps between the points, as d3's <c>curveBumpX</c> (horizontal tangents) and <c>curveBumpY</c> (vertical tangents).</summary>
public struct BumpCurve : ICurve
{
    private readonly bool horizontal;
    private LineState line;
    private int point;
    private double x0;
    private double y0;

    /// <summary>Creates a bump curve.</summary>
    /// <param name="horizontal">Whether the tangents are horizontal (<c>curveBumpX</c>) rather than vertical (<c>curveBumpY</c>).</param>
    public BumpCurve(bool horizontal)
    {
        this.horizontal = horizontal;
    }

    /// <inheritdoc />
    public void AreaStart() => line = LineState.Topline;

    /// <inheritdoc />
    public void AreaEnd() => line = LineState.Line;

    /// <inheritdoc />
    public void LineStart() => point = 0;

    /// <inheritdoc />
    public void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink
    {
        line.Close(ref context, point == 1);
        line = line.Next();
    }

    /// <inheritdoc />
    public void Point<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        if (point == 0)
        {
            point = 1;
            line.Start(ref context, x, y);
        }
        else if (horizontal)
        {
            point = 2;
            x0 = (x0 + x) / 2;
            context.BezierCurveTo(x0, y0, x0, y, x, y);
        }
        else
        {
            point = 2;
            y0 = (y0 + y) / 2;
            context.BezierCurveTo(x0, y0, x, y0, x, y);
        }

        x0 = x;
        y0 = y;
    }
}
