using System.Buffers;
using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>
/// A bundle, as d3's <c>curveBundle.beta(beta)</c>: each segment's points are pulled toward the straight line between
/// its ends by <c>1 - beta</c>, then drawn as a basis spline. Lines only: like d3's, it cannot start an area. The
/// points of a segment are held in pooled buffers until it ends.
/// </summary>
public struct BundleCurve : ICurve
{
    private readonly double beta;
    private BasisCurve basis;
    private double[]? xs;
    private double[]? ys;
    private int count;

    /// <summary>Creates a bundle curve.</summary>
    /// <param name="beta">0 gives a straight line, 1 a basis spline.</param>
    public BundleCurve(double beta)
    {
        this.beta = beta;
        basis = default;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Always: d3's bundle curve has no <c>areaStart</c>.</exception>
    public readonly void AreaStart() => throw new InvalidOperationException("The bundle curve draws lines only, as in d3.");

    /// <inheritdoc />
    public readonly void AreaEnd()
    {
    }

    /// <inheritdoc />
    public void LineStart()
    {
        count = 0;
        basis.LineStart();
    }

    /// <inheritdoc />
    public void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink
    {
        var j = count - 1;
        if (j > 0)
        {
            var (x0, y0) = (xs![0], ys![0]);
            var dx = xs[j] - x0;
            var dy = ys[j] - y0;
            for (var i = 0; i <= j; i++)
            {
                var t = (double)i / j;
                basis.Point(ref context, beta * xs[i] + (1 - beta) * (x0 + t * dx), beta * ys[i] + (1 - beta) * (y0 + t * dy));
            }
        }

        Release();
        basis.LineEnd(ref context);
    }

    /// <inheritdoc />
    public void Point<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        if (xs is null || count == xs.Length)
        {
            Grow();
        }

        xs![count] = x;
        ys![count] = y;
        count++;
    }

    private void Grow()
    {
        var length = xs is null ? 16 : xs.Length * 2;
        var newXs = ArrayPool<double>.Shared.Rent(length);
        var newYs = ArrayPool<double>.Shared.Rent(length);
        if (xs is not null)
        {
            xs.AsSpan(0, count).CopyTo(newXs);
            ys.AsSpan(0, count).CopyTo(newYs);
            Release();
        }

        xs = newXs;
        ys = newYs;
    }

    private void Release()
    {
        if (xs is not null)
        {
            ArrayPool<double>.Shared.Return(xs);
            ArrayPool<double>.Shared.Return(ys!);
            xs = ys = null;
        }
    }
}
