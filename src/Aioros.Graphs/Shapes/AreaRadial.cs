using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes;

/// <summary>The radial area generator of d3-shape (<c>areaRadial</c>): an area between an inner and an outer radius.</summary>
public static class AreaRadial
{
    /// <summary>Writes a radial area into a new path over <paramref name="sink"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="curve">The curve between the points, applied to their Cartesian coordinates.</param>
    /// <param name="startAngle">The baseline's angles, one per point.</param>
    /// <param name="innerRadius">The baseline's radii, one per point or a single value for all.</param>
    /// <param name="outerRadius">The topline's radii, one per point.</param>
    /// <param name="defined">Which points are defined; empty means all of them.</param>
    /// <param name="endAngle">The topline's angles, one per point or a single value; empty means <paramref name="startAngle"/>.</param>
    public static void Write<TSink>(ref TSink sink, Curve curve, ReadOnlySpan<double> startAngle, ReadOnlySpan<double> innerRadius, ReadOnlySpan<double> outerRadius, ReadOnlySpan<bool> defined = default, ReadOnlySpan<double> endAngle = default)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Write(ref context, curve, startAngle, innerRadius, outerRadius, defined, endAngle);
    }

    /// <summary>Writes a radial area into <paramref name="context"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="curve">The curve between the points, applied to their Cartesian coordinates.</param>
    /// <param name="startAngle">The baseline's angles, one per point.</param>
    /// <param name="innerRadius">The baseline's radii, one per point or a single value for all.</param>
    /// <param name="outerRadius">The topline's radii, one per point.</param>
    /// <param name="defined">Which points are defined; empty means all of them.</param>
    /// <param name="endAngle">The topline's angles, one per point or a single value; empty means <paramref name="startAngle"/>.</param>
    public static void Write<TSink>(ref PathContext<TSink> context, Curve curve, ReadOnlySpan<double> startAngle, ReadOnlySpan<double> innerRadius, ReadOnlySpan<double> outerRadius, ReadOnlySpan<bool> defined = default, ReadOnlySpan<double> endAngle = default)
        where TSink : IPathSink
    {
        var drawing = new RadialDrawing<Area.Drawing>(new Area.Drawing(startAngle, innerRadius, outerRadius, defined, endAngle));
        CurveDispatch.Run(curve, ref context, ref drawing);
    }
}
