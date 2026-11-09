using Aioros.Graphs.Paths;
using Aioros.Graphs.Shapes.Curves;

namespace Aioros.Graphs.Shapes;

/// <summary>
/// The area generator of d3-shape: for each defined segment, the topline (x1[i], y1[i]) forward and the baseline
/// (x0[i], y0[i]) backward through a curve, closed.
/// </summary>
public static class Area
{
    /// <summary>Writes an area into a new path over <paramref name="sink"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="curve">The curve between the points.</param>
    /// <param name="x0">The baseline's horizontal coordinates, one per point.</param>
    /// <param name="y0">The baseline's vertical coordinates, one per point or a single value for all.</param>
    /// <param name="y1">The topline's vertical coordinates, one per point.</param>
    /// <param name="defined">Which points are defined; empty means all of them.</param>
    /// <param name="x1">The topline's horizontal coordinates, one per point or a single value; empty means <paramref name="x0"/>.</param>
    public static void Write<TSink>(ref TSink sink, Curve curve, ReadOnlySpan<double> x0, ReadOnlySpan<double> y0, ReadOnlySpan<double> y1, ReadOnlySpan<bool> defined = default, ReadOnlySpan<double> x1 = default)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Write(ref context, curve, x0, y0, y1, defined, x1);
    }

    /// <summary>Writes an area into <paramref name="context"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="curve">The curve between the points.</param>
    /// <param name="x0">The baseline's horizontal coordinates, one per point.</param>
    /// <param name="y0">The baseline's vertical coordinates, one per point or a single value for all.</param>
    /// <param name="y1">The topline's vertical coordinates, one per point.</param>
    /// <param name="defined">Which points are defined; empty means all of them.</param>
    /// <param name="x1">The topline's horizontal coordinates, one per point or a single value; empty means <paramref name="x0"/>.</param>
    public static void Write<TSink>(ref PathContext<TSink> context, Curve curve, ReadOnlySpan<double> x0, ReadOnlySpan<double> y0, ReadOnlySpan<double> y1, ReadOnlySpan<bool> defined = default, ReadOnlySpan<double> x1 = default)
        where TSink : IPathSink
    {
        var drawing = new Drawing(x0, y0, y1, defined, x1);
        CurveDispatch.Run(curve, ref context, ref drawing);
    }

    /// <summary>Writes an area through a curve chosen at compile time.</summary>
    /// <typeparam name="TCurve">The curve.</typeparam>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="curve">The curve's state.</param>
    /// <param name="x0">The baseline's horizontal coordinates, one per point.</param>
    /// <param name="y0">The baseline's vertical coordinates, one per point or a single value for all.</param>
    /// <param name="y1">The topline's vertical coordinates, one per point.</param>
    /// <param name="defined">Which points are defined; empty means all of them.</param>
    /// <param name="x1">The topline's horizontal coordinates, one per point or a single value; empty means <paramref name="x0"/>.</param>
    public static void Write<TCurve, TSink>(ref PathContext<TSink> context, ref TCurve curve, ReadOnlySpan<double> x0, ReadOnlySpan<double> y0, ReadOnlySpan<double> y1, ReadOnlySpan<bool> defined = default, ReadOnlySpan<double> x1 = default)
        where TCurve : struct, ICurve
        where TSink : IPathSink
    {
        var drawing = new Drawing(x0, y0, y1, defined, x1);
        drawing.Draw(ref context, ref curve);
    }

    internal readonly ref struct Drawing : ICurveDrawing
    {
        private readonly ReadOnlySpan<double> x0;
        private readonly ReadOnlySpan<double> y0;
        private readonly ReadOnlySpan<double> y1;
        private readonly ReadOnlySpan<bool> defined;
        private readonly ReadOnlySpan<double> x1;

        public Drawing(ReadOnlySpan<double> x0, ReadOnlySpan<double> y0, ReadOnlySpan<double> y1, ReadOnlySpan<bool> defined, ReadOnlySpan<double> x1)
        {
            var n = x0.Length;
            Check(y1.Length == n, "The topline needs one y per point.", nameof(y1));
            Check(y0.Length == n || y0.Length == 1, "The baseline needs one y per point or a single value.", nameof(y0));
            Check(x1.Length is 0 or 1 || x1.Length == n, "The topline's x must be empty, a single value or one per point.", nameof(x1));
            Check(defined.IsEmpty || defined.Length == n, "The defined flags must be empty or one per point.", nameof(defined));
            this.x0 = x0;
            this.y0 = y0;
            this.y1 = y1;
            this.defined = defined;
            this.x1 = x1.IsEmpty ? x0 : x1;
        }

        public void Draw<TCurve, TSink>(ref PathContext<TSink> context, ref TCurve curve)
            where TCurve : struct, ICurve
            where TSink : IPathSink
        {
            var n = x0.Length;
            var inside = false;
            var start = 0;
            for (var i = 0; i <= n; i++)
            {
                var isDefined = i < n && (defined.IsEmpty || defined[i]);
                if (isDefined != inside)
                {
                    inside = isDefined;
                    if (inside)
                    {
                        start = i;
                        curve.AreaStart();
                        curve.LineStart();
                    }
                    else
                    {
                        Baseline(ref context, ref curve, start, i - 1);
                    }
                }

                if (inside)
                {
                    curve.Point(ref context, At(x1, i), y1[i]);
                }
            }
        }

        private static double At(ReadOnlySpan<double> values, int i) => values.Length == 1 ? values[0] : values[i];

        private static void Check(bool condition, string message, string name)
        {
            if (!condition)
            {
                throw new ArgumentException(message, name);
            }
        }

        private void Baseline<TCurve, TSink>(ref PathContext<TSink> context, ref TCurve curve, int start, int end)
            where TCurve : struct, ICurve
            where TSink : IPathSink
        {
            curve.LineEnd(ref context);
            curve.LineStart();
            for (var k = end; k >= start; k--)
            {
                curve.Point(ref context, x0[k], At(y0, k));
            }

            curve.LineEnd(ref context);
            curve.AreaEnd();
        }
    }
}
