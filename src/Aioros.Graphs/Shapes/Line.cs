using Aioros.Graphs.Paths;
using Aioros.Graphs.Shapes.Curves;

namespace Aioros.Graphs.Shapes;

/// <summary>
/// The line generator of d3-shape: writes the points (x[i], y[i]) through a curve, breaking the line where
/// <c>defined[i]</c> is false.
/// </summary>
public static class Line
{
    /// <summary>Writes a line into a new path over <paramref name="sink"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="curve">The curve between the points.</param>
    /// <param name="x">The horizontal coordinates.</param>
    /// <param name="y">The vertical coordinates, as many as <paramref name="x"/>.</param>
    /// <param name="defined">Which points are defined; empty means all of them.</param>
    public static void Write<TSink>(ref TSink sink, Curve curve, ReadOnlySpan<double> x, ReadOnlySpan<double> y, ReadOnlySpan<bool> defined = default)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Write(ref context, curve, x, y, defined);
    }

    /// <summary>Writes a line into <paramref name="context"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="curve">The curve between the points.</param>
    /// <param name="x">The horizontal coordinates.</param>
    /// <param name="y">The vertical coordinates, as many as <paramref name="x"/>.</param>
    /// <param name="defined">Which points are defined; empty means all of them.</param>
    public static void Write<TSink>(ref PathContext<TSink> context, Curve curve, ReadOnlySpan<double> x, ReadOnlySpan<double> y, ReadOnlySpan<bool> defined = default)
        where TSink : IPathSink
    {
        Validate(x, y, defined);
        var drawing = new Drawing(x, y, defined);
        CurveDispatch.Run(curve, ref context, ref drawing);
    }

    /// <summary>Writes a line through a curve chosen at compile time.</summary>
    /// <typeparam name="TCurve">The curve.</typeparam>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="curve">The curve's state.</param>
    /// <param name="x">The horizontal coordinates.</param>
    /// <param name="y">The vertical coordinates, as many as <paramref name="x"/>.</param>
    /// <param name="defined">Which points are defined; empty means all of them.</param>
    public static void Write<TCurve, TSink>(ref PathContext<TSink> context, ref TCurve curve, ReadOnlySpan<double> x, ReadOnlySpan<double> y, ReadOnlySpan<bool> defined = default)
        where TCurve : struct, ICurve
        where TSink : IPathSink
    {
        Validate(x, y, defined);
        var drawing = new Drawing(x, y, defined);
        drawing.Draw(ref context, ref curve);
    }

    internal static void Validate(ReadOnlySpan<double> x, ReadOnlySpan<double> y, ReadOnlySpan<bool> defined)
    {
        if (y.Length != x.Length)
        {
            throw new ArgumentException("A line needs as many y as x coordinates.", nameof(y));
        }

        if (!defined.IsEmpty && defined.Length != x.Length)
        {
            throw new ArgumentException("The defined flags must be empty or one per point.", nameof(defined));
        }
    }

    internal readonly ref struct Drawing(ReadOnlySpan<double> x, ReadOnlySpan<double> y, ReadOnlySpan<bool> defined) : ICurveDrawing
    {
        private readonly ReadOnlySpan<double> x = x;
        private readonly ReadOnlySpan<double> y = y;
        private readonly ReadOnlySpan<bool> defined = defined;

        public void Draw<TCurve, TSink>(ref PathContext<TSink> context, ref TCurve curve)
            where TCurve : struct, ICurve
            where TSink : IPathSink
        {
            var n = x.Length;
            var inside = false;
            for (var i = 0; i <= n; i++)
            {
                var isDefined = i < n && (defined.IsEmpty || defined[i]);
                if (isDefined != inside)
                {
                    inside = isDefined;
                    if (inside)
                    {
                        curve.LineStart();
                    }
                    else
                    {
                        curve.LineEnd(ref context);
                    }
                }

                if (inside)
                {
                    curve.Point(ref context, x[i], y[i]);
                }
            }
        }
    }
}
