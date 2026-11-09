using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>
/// A curve over polar points, as d3's <c>curveRadial(curve)</c>: each point (angle, radius) is passed to the wrapped
/// curve as (radius × sin angle, −radius × cos angle).
/// </summary>
/// <typeparam name="TCurve">The wrapped curve.</typeparam>
public struct RadialCurve<TCurve> : ICurve
    where TCurve : struct, ICurve
{
    private TCurve curve;

    /// <summary>Wraps <paramref name="curve"/>.</summary>
    /// <param name="curve">The curve the Cartesian points go to.</param>
    public RadialCurve(TCurve curve)
    {
        this.curve = curve;
    }

    /// <inheritdoc />
    public void AreaStart() => curve.AreaStart();

    /// <inheritdoc />
    public void AreaEnd() => curve.AreaEnd();

    /// <inheritdoc />
    public void LineStart() => curve.LineStart();

    /// <inheritdoc />
    public void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink => curve.LineEnd(ref context);

    /// <inheritdoc />
    public void Point<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink => curve.Point(ref context, y * Math.Sin(x), y * -Math.Cos(x));
}
