using System.Buffers;
using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>
/// A natural cubic spline through the points, its second derivative zero at both ends, as d3's <c>curveNatural</c>.
/// It holds the points of a segment in pooled buffers until the segment ends.
/// </summary>
public struct NaturalCurve : ICurve
{
    private LineState line;
    private double[]? xs;
    private double[]? ys;
    private int count;

    /// <inheritdoc />
    public void AreaStart() => line = LineState.Topline;

    /// <inheritdoc />
    public void AreaEnd() => line = LineState.Line;

    /// <inheritdoc />
    public void LineStart()
    {
        xs ??= ArrayPool<double>.Shared.Rent(16);
        ys ??= ArrayPool<double>.Shared.Rent(16);
        count = 0;
    }

    /// <inheritdoc />
    public void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink
    {
        var n = count;
        if (n > 0)
        {
            var x = xs.AsSpan(0, n);
            var y = ys.AsSpan(0, n);
            line.Start(ref context, x[0], y[0]);
            if (n == 2)
            {
                context.LineTo(x[1], y[1]);
            }
            else if (n > 2)
            {
                Spline(ref context, x, y);
            }
        }

        line.Close(ref context, n == 1);
        line = line.Next();
        Release();
    }

    /// <inheritdoc />
    public void Point<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        if (count == xs!.Length)
        {
            xs = Grow(xs, count);
            ys = Grow(ys!, count);
        }

        xs[count] = x;
        ys![count] = y;
        count++;
    }

    private static void Spline<TSink>(ref PathContext<TSink> context, ReadOnlySpan<double> x, ReadOnlySpan<double> y)
        where TSink : IPathSink
    {
        var n = x.Length - 1;
        var buffer = ArrayPool<double>.Shared.Rent(n * 6);
        var px0 = buffer.AsSpan(0, n);
        var px1 = buffer.AsSpan(n, n);
        var py0 = buffer.AsSpan(2 * n, n);
        var py1 = buffer.AsSpan(3 * n, n);
        var scratch = buffer.AsSpan(4 * n, n);
        ControlPoints(x, px0, px1, scratch);
        ControlPoints(y, py0, py1, scratch);
        for (var i = 0; i < n; i++)
        {
            context.BezierCurveTo(px0[i], py0[i], px1[i], py1[i], x[i + 1], y[i + 1]);
        }

        ArrayPool<double>.Shared.Return(buffer);
    }

    private static void ControlPoints(ReadOnlySpan<double> x, Span<double> a, Span<double> b, Span<double> r)
    {
        var n = x.Length - 1;
        a[0] = 0;
        b[0] = 2;
        r[0] = x[0] + 2 * x[1];
        for (var i = 1; i < n - 1; i++)
        {
            a[i] = 1;
            b[i] = 4;
            r[i] = 4 * x[i] + 2 * x[i + 1];
        }

        a[n - 1] = 2;
        b[n - 1] = 7;
        r[n - 1] = 8 * x[n - 1] + x[n];
        for (var i = 1; i < n; i++)
        {
            var m = a[i] / b[i - 1];
            b[i] -= m;
            r[i] -= m * r[i - 1];
        }

        a[n - 1] = r[n - 1] / b[n - 1];
        for (var i = n - 2; i >= 0; i--)
        {
            a[i] = (r[i] - a[i + 1]) / b[i];
        }

        b[n - 1] = (x[n] + a[n - 1]) / 2;
        for (var i = 0; i < n - 1; i++)
        {
            b[i] = 2 * x[i + 1] - a[i + 1];
        }
    }

    private static double[] Grow(double[] buffer, int count)
    {
        var larger = ArrayPool<double>.Shared.Rent(count * 2);
        buffer.AsSpan(0, count).CopyTo(larger);
        ArrayPool<double>.Shared.Return(buffer);
        return larger;
    }

    private void Release()
    {
        if (xs is not null)
        {
            ArrayPool<double>.Shared.Return(xs);
            ArrayPool<double>.Shared.Return(ys!);
            xs = ys = null;
        }

        count = 0;
    }
}
