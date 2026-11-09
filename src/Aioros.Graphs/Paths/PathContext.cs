namespace Aioros.Graphs.Paths;

/// <summary>
/// The canvas-like path API of d3-path over a sink: it keeps the current point, turns center arcs and tangent arcs
/// into SVG arcs, and skips what d3 skips, so a sink receives exactly the commands d3 would write.
/// </summary>
/// <typeparam name="TSink">The sink the commands go to.</typeparam>
public ref struct PathContext<TSink>
    where TSink : IPathSink
{
    private const double Epsilon = 1e-6;
    private const double Tau = 2 * Math.PI;
    private const double TauEpsilon = Tau - Epsilon;

    private readonly ref TSink sink;
    private double x0;
    private double y0;
    private double x1;
    private double y1;
    private bool hasCurrentPoint;
    private bool hasSubpathStart;

    /// <summary>Starts an empty path over <paramref name="sink"/>.</summary>
    /// <param name="sink">The sink, written in place.</param>
    public PathContext(ref TSink sink)
    {
        this.sink = ref sink;
    }

    /// <summary>Starts a subpath at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    public void MoveTo(double x, double y)
    {
        x0 = x1 = x;
        y0 = y1 = y;
        hasCurrentPoint = hasSubpathStart = true;
        sink.MoveTo(x, y);
    }

    /// <summary>Closes the current subpath, if there is one.</summary>
    public void ClosePath()
    {
        if (hasCurrentPoint)
        {
            x1 = x0;
            y1 = y0;
            hasCurrentPoint = hasSubpathStart;
            sink.ClosePath();
        }
    }

    /// <summary>Draws a straight line to (<paramref name="x"/>, <paramref name="y"/>).</summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    public void LineTo(double x, double y)
    {
        SetCurrent(x, y);
        sink.LineTo(x, y);
    }

    /// <summary>Draws a quadratic Bézier curve.</summary>
    /// <param name="cx">The control point's horizontal coordinate.</param>
    /// <param name="cy">The control point's vertical coordinate.</param>
    /// <param name="x">The end point's horizontal coordinate.</param>
    /// <param name="y">The end point's vertical coordinate.</param>
    public void QuadraticCurveTo(double cx, double cy, double x, double y)
    {
        SetCurrent(x, y);
        sink.QuadraticCurveTo(cx, cy, x, y);
    }

    /// <summary>Draws a cubic Bézier curve.</summary>
    /// <param name="cx1">The first control point's horizontal coordinate.</param>
    /// <param name="cy1">The first control point's vertical coordinate.</param>
    /// <param name="cx2">The second control point's horizontal coordinate.</param>
    /// <param name="cy2">The second control point's vertical coordinate.</param>
    /// <param name="x">The end point's horizontal coordinate.</param>
    /// <param name="y">The end point's vertical coordinate.</param>
    public void BezierCurveTo(double cx1, double cy1, double cx2, double cy2, double x, double y)
    {
        SetCurrent(x, y);
        sink.BezierCurveTo(cx1, cy1, cx2, cy2, x, y);
    }

    /// <summary>Draws a closed rectangle as its own subpath.</summary>
    /// <param name="x">The left coordinate.</param>
    /// <param name="y">The top coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public void Rect(double x, double y, double width, double height)
    {
        x0 = x1 = x;
        y0 = y1 = y;
        hasCurrentPoint = hasSubpathStart = true;
        sink.Rect(x, y, width, height);
    }

    /// <summary>
    /// Draws an arc of radius <paramref name="r"/> tangent to the line from the current point to (<paramref name="ax"/>,
    /// <paramref name="ay"/>) and to the line from there to (<paramref name="bx"/>, <paramref name="by"/>), as the
    /// canvas <c>arcTo</c>.
    /// </summary>
    /// <param name="ax">The corner's horizontal coordinate.</param>
    /// <param name="ay">The corner's vertical coordinate.</param>
    /// <param name="bx">The second tangent's end, horizontal.</param>
    /// <param name="by">The second tangent's end, vertical.</param>
    /// <param name="r">The radius; must not be negative.</param>
    public void ArcTo(double ax, double ay, double bx, double by, double r)
    {
        ThrowIfNegative(r);
        var x21 = bx - ax;
        var y21 = by - ay;
        var x01 = x1 - ax;
        var y01 = y1 - ay;
        var l01Squared = x01 * x01 + y01 * y01;
        if (!hasCurrentPoint)
        {
            MoveToWithoutSubpath(ax, ay);
            return;
        }

        if (!(l01Squared > Epsilon))
        {
            return;
        }

        if (!(Math.Abs(y01 * x21 - y21 * x01) > Epsilon) || IsZeroOrNaN(r))
        {
            LineTo(ax, ay);
            return;
        }

        TangentArc(ax, ay, bx, by, r, l01Squared);
    }

    /// <summary>
    /// Draws an arc of the circle centered at (<paramref name="x"/>, <paramref name="y"/>) from angle
    /// <paramref name="a0"/> to <paramref name="a1"/>, as the canvas <c>arc</c>, joined to the current point by a line.
    /// </summary>
    /// <param name="x">The center's horizontal coordinate.</param>
    /// <param name="y">The center's vertical coordinate.</param>
    /// <param name="r">The radius; must not be negative.</param>
    /// <param name="a0">The start angle in radians.</param>
    /// <param name="a1">The end angle in radians.</param>
    /// <param name="counterclockwise">Whether to go in the direction of decreasing angles.</param>
    public void Arc(double x, double y, double r, double a0, double a1, bool counterclockwise = false)
    {
        ThrowIfNegative(r);
        var dx = r * Math.Cos(a0);
        var dy = r * Math.Sin(a0);
        var startX = x + dx;
        var startY = y + dy;
        var sweep = !counterclockwise;
        var da = counterclockwise ? a0 - a1 : a1 - a0;
        if (!hasCurrentPoint)
        {
            sink.MoveTo(startX, startY);
        }
        else if (Math.Abs(x1 - startX) > Epsilon || Math.Abs(y1 - startY) > Epsilon)
        {
            sink.LineTo(startX, startY);
        }

        if (IsZeroOrNaN(r))
        {
            return;
        }

        if (da < 0)
        {
            da = da % Tau + Tau;
        }

        if (da > TauEpsilon)
        {
            sink.ArcTo(r, true, sweep, x - dx, y - dy);
            SetCurrent(startX, startY);
            sink.ArcTo(r, true, sweep, startX, startY);
        }
        else if (da > Epsilon)
        {
            var endX = x + r * Math.Cos(a1);
            var endY = y + r * Math.Sin(a1);
            SetCurrent(endX, endY);
            sink.ArcTo(r, da >= Math.PI, sweep, endX, endY);
        }
    }

    private void TangentArc(double ax, double ay, double bx, double by, double r, double l01Squared)
    {
        var x21 = bx - ax;
        var y21 = by - ay;
        var x01 = x1 - ax;
        var y01 = y1 - ay;
        var x20 = bx - x1;
        var y20 = by - y1;
        var l21Squared = x21 * x21 + y21 * y21;
        var l20Squared = x20 * x20 + y20 * y20;
        var l21 = Math.Sqrt(l21Squared);
        var l01 = Math.Sqrt(l01Squared);
        var l = r * Math.Tan((Math.PI - Math.Acos((l21Squared + l01Squared - l20Squared) / (2 * l21 * l01))) / 2);
        var t01 = l / l01;
        var t21 = l / l21;
        if (Math.Abs(t01 - 1) > Epsilon)
        {
            sink.LineTo(ax + t01 * x01, ay + t01 * y01);
        }

        var endX = ax + t21 * x21;
        var endY = ay + t21 * y21;
        SetCurrent(endX, endY);
        sink.ArcTo(r, false, y01 * x20 > x01 * y20, endX, endY);
    }

    private static void ThrowIfNegative(double r)
    {
        if (r < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(r), r, "negative radius");
        }
    }

    private static bool IsZeroOrNaN(double r) => r == 0 || double.IsNaN(r);

    private void MoveToWithoutSubpath(double x, double y)
    {
        SetCurrent(x, y);
        sink.MoveTo(x, y);
    }

    private void SetCurrent(double x, double y)
    {
        x1 = x;
        y1 = y;
        hasCurrentPoint = true;
    }
}
