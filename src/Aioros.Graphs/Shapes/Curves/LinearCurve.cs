using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>Straight lines between the points, as d3's <c>curveLinear</c>.</summary>
public struct LinearCurve : ICurve
{
    private LineState line;
    private int point;

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
        else
        {
            point = 2;
            context.LineTo(x, y);
        }
    }
}
