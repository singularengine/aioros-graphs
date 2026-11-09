using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>A closed polygon through the points, as d3's <c>curveLinearClosed</c>.</summary>
public struct LinearClosedCurve : ICurve
{
    private bool started;

    /// <inheritdoc />
    public readonly void AreaStart()
    {
    }

    /// <inheritdoc />
    public readonly void AreaEnd()
    {
    }

    /// <inheritdoc />
    public void LineStart() => started = false;

    /// <inheritdoc />
    public readonly void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink
    {
        if (started)
        {
            context.ClosePath();
        }
    }

    /// <inheritdoc />
    public void Point<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        if (started)
        {
            context.LineTo(x, y);
        }
        else
        {
            started = true;
            context.MoveTo(x, y);
        }
    }
}
