using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes;

/// <summary>The radial line generator of d3-shape (<c>lineRadial</c>): a line through points given by angle and radius.</summary>
public static class LineRadial
{
    /// <summary>Writes a radial line into a new path over <paramref name="sink"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="curve">The curve between the points, applied to their Cartesian coordinates.</param>
    /// <param name="angle">The angles in radians, clockwise from 12 o'clock.</param>
    /// <param name="radius">The radii, as many as <paramref name="angle"/>.</param>
    /// <param name="defined">Which points are defined; empty means all of them.</param>
    public static void Write<TSink>(ref TSink sink, Curve curve, ReadOnlySpan<double> angle, ReadOnlySpan<double> radius, ReadOnlySpan<bool> defined = default)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Write(ref context, curve, angle, radius, defined);
    }

    /// <summary>Writes a radial line into <paramref name="context"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="curve">The curve between the points, applied to their Cartesian coordinates.</param>
    /// <param name="angle">The angles in radians, clockwise from 12 o'clock.</param>
    /// <param name="radius">The radii, as many as <paramref name="angle"/>.</param>
    /// <param name="defined">Which points are defined; empty means all of them.</param>
    public static void Write<TSink>(ref PathContext<TSink> context, Curve curve, ReadOnlySpan<double> angle, ReadOnlySpan<double> radius, ReadOnlySpan<bool> defined = default)
        where TSink : IPathSink
    {
        Line.Validate(angle, radius, defined);
        var drawing = new RadialDrawing<Line.Drawing>(new Line.Drawing(angle, radius, defined));
        CurveDispatch.Run(curve, ref context, ref drawing);
    }
}
