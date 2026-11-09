using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>
/// A piecewise constant line of horizontal and vertical segments, as d3's <c>curveStep</c> (the change at the
/// midpoint), <c>curveStepBefore</c> (position 0) and <c>curveStepAfter</c> (position 1).
/// </summary>
public struct StepCurve : ICurve
{
    private double t;
    private LineState line;
    private int point;
    private double x0;
    private double y0;

    /// <summary>Creates a step curve changing value at <paramref name="position"/> between two points.</summary>
    /// <param name="position">0 changes at the previous point, 1 at the next, 0.5 midway.</param>
    public StepCurve(double position)
    {
        t = position;
    }

    /// <inheritdoc />
    public void AreaStart() => line = LineState.Topline;

    /// <inheritdoc />
    public void AreaEnd() => line = LineState.Line;

    /// <inheritdoc />
    public void LineStart()
    {
        x0 = y0 = double.NaN;
        point = 0;
    }

    /// <inheritdoc />
    public void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink
    {
        if (0 < t && t < 1 && point == 2)
        {
            context.LineTo(x0, y0);
        }

        line.Close(ref context, point == 1);
        if (line != LineState.Line)
        {
            t = 1 - t;
            line = line.Next();
        }
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
            if (t <= 0)
            {
                context.LineTo(x0, y);
                context.LineTo(x, y);
            }
            else
            {
                var x1 = x0 * (1 - t) + x * t;
                context.LineTo(x1, y0);
                context.LineTo(x1, y);
            }
        }

        x0 = x;
        y0 = y;
    }
}
