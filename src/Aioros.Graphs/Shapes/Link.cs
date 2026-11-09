using Aioros.Graphs.Paths;
using Aioros.Graphs.Shapes.Curves;

namespace Aioros.Graphs.Shapes;

/// <summary>
/// The link generators of d3-shape: a curve from a source point to a target point. The overloads over a
/// <see cref="PathContext{TSink}"/> let many links share one path, as d3's links drawn on one context.
/// </summary>
public static class Link
{
    /// <summary>Writes a link through <paramref name="curve"/> into a new path, as d3's <c>link(curve)</c>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="curve">The curve from the source to the target.</param>
    /// <param name="sourceX">The source's horizontal coordinate.</param>
    /// <param name="sourceY">The source's vertical coordinate.</param>
    /// <param name="targetX">The target's horizontal coordinate.</param>
    /// <param name="targetY">The target's vertical coordinate.</param>
    public static void Write<TSink>(ref TSink sink, Curve curve, double sourceX, double sourceY, double targetX, double targetY)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Write(ref context, curve, sourceX, sourceY, targetX, targetY);
    }

    /// <summary>Writes a link through <paramref name="curve"/> into <paramref name="context"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="curve">The curve from the source to the target.</param>
    /// <param name="sourceX">The source's horizontal coordinate.</param>
    /// <param name="sourceY">The source's vertical coordinate.</param>
    /// <param name="targetX">The target's horizontal coordinate.</param>
    /// <param name="targetY">The target's vertical coordinate.</param>
    public static void Write<TSink>(ref PathContext<TSink> context, Curve curve, double sourceX, double sourceY, double targetX, double targetY)
        where TSink : IPathSink
    {
        var drawing = new Drawing(sourceX, sourceY, targetX, targetY);
        CurveDispatch.Run(curve, ref context, ref drawing);
    }

    /// <summary>Writes a link with horizontal tangents into a new path, as d3's <c>linkHorizontal()</c>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="sourceX">The source's horizontal coordinate.</param>
    /// <param name="sourceY">The source's vertical coordinate.</param>
    /// <param name="targetX">The target's horizontal coordinate.</param>
    /// <param name="targetY">The target's vertical coordinate.</param>
    public static void Horizontal<TSink>(ref TSink sink, double sourceX, double sourceY, double targetX, double targetY)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Horizontal(ref context, sourceX, sourceY, targetX, targetY);
    }

    /// <summary>Writes a link with horizontal tangents into <paramref name="context"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="sourceX">The source's horizontal coordinate.</param>
    /// <param name="sourceY">The source's vertical coordinate.</param>
    /// <param name="targetX">The target's horizontal coordinate.</param>
    /// <param name="targetY">The target's vertical coordinate.</param>
    public static void Horizontal<TSink>(ref PathContext<TSink> context, double sourceX, double sourceY, double targetX, double targetY)
        where TSink : IPathSink
    {
        var bump = new BumpCurve(true);
        new Drawing(sourceX, sourceY, targetX, targetY).Draw(ref context, ref bump);
    }

    /// <summary>Writes a link with vertical tangents into a new path, as d3's <c>linkVertical()</c>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="sourceX">The source's horizontal coordinate.</param>
    /// <param name="sourceY">The source's vertical coordinate.</param>
    /// <param name="targetX">The target's horizontal coordinate.</param>
    /// <param name="targetY">The target's vertical coordinate.</param>
    public static void Vertical<TSink>(ref TSink sink, double sourceX, double sourceY, double targetX, double targetY)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Vertical(ref context, sourceX, sourceY, targetX, targetY);
    }

    /// <summary>Writes a link with vertical tangents into <paramref name="context"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="sourceX">The source's horizontal coordinate.</param>
    /// <param name="sourceY">The source's vertical coordinate.</param>
    /// <param name="targetX">The target's horizontal coordinate.</param>
    /// <param name="targetY">The target's vertical coordinate.</param>
    public static void Vertical<TSink>(ref PathContext<TSink> context, double sourceX, double sourceY, double targetX, double targetY)
        where TSink : IPathSink
    {
        var bump = new BumpCurve(false);
        new Drawing(sourceX, sourceY, targetX, targetY).Draw(ref context, ref bump);
    }

    /// <summary>Writes a radial link into a new path, as d3's <c>linkRadial()</c>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="sourceAngle">The source's angle in radians, clockwise from 12 o'clock.</param>
    /// <param name="sourceRadius">The source's radius.</param>
    /// <param name="targetAngle">The target's angle.</param>
    /// <param name="targetRadius">The target's radius.</param>
    public static void Radial<TSink>(ref TSink sink, double sourceAngle, double sourceRadius, double targetAngle, double targetRadius)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Radial(ref context, sourceAngle, sourceRadius, targetAngle, targetRadius);
    }

    /// <summary>Writes a radial link into <paramref name="context"/>, with d3's <c>bumpRadial</c> curve.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="sourceAngle">The source's angle in radians, clockwise from 12 o'clock.</param>
    /// <param name="sourceRadius">The source's radius.</param>
    /// <param name="targetAngle">The target's angle.</param>
    /// <param name="targetRadius">The target's radius.</param>
    public static void Radial<TSink>(ref PathContext<TSink> context, double sourceAngle, double sourceRadius, double targetAngle, double targetRadius)
        where TSink : IPathSink
    {
        var middle = (sourceRadius + targetRadius) / 2;
        var (x0, y0) = Shapes.Radial.Point(sourceAngle, sourceRadius);
        var (x1, y1) = Shapes.Radial.Point(sourceAngle, middle);
        var (x2, y2) = Shapes.Radial.Point(targetAngle, middle);
        var (x3, y3) = Shapes.Radial.Point(targetAngle, targetRadius);
        context.MoveTo(x0, y0);
        context.BezierCurveTo(x1, y1, x2, y2, x3, y3);
    }

    private readonly ref struct Drawing(double sourceX, double sourceY, double targetX, double targetY) : ICurveDrawing
    {
        public void Draw<TCurve, TSink>(ref PathContext<TSink> context, ref TCurve curve)
            where TCurve : struct, ICurve
            where TSink : IPathSink
        {
            curve.LineStart();
            curve.Point(ref context, sourceX, sourceY);
            curve.Point(ref context, targetX, targetY);
            curve.LineEnd(ref context);
        }
    }
}
