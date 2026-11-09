using System.Buffers;

namespace Aioros.Graphs.Hierarchies;

/// <summary>The settings of a <see cref="Pack"/> layout.</summary>
public sealed record PackOptions
{
    /// <summary>The layout's width.</summary>
    public double Width { get; init; } = 1;

    /// <summary>The layout's height.</summary>
    public double Height { get; init; } = 1;

    /// <summary>The gap between sibling circles and between a parent and its children.</summary>
    public double Padding { get; init; }
}

/// <summary>The circles of a hierarchy's nodes, by node index.</summary>
/// <param name="X">Each node's center x.</param>
/// <param name="Y">Each node's center y.</param>
/// <param name="R">Each node's radius.</param>
public readonly record struct NodeCircles(double[] X, double[] Y, double[] R);

/// <summary>
/// d3-hierarchy's circle packing: <c>pack</c>, <c>packSiblings</c> and <c>packEnclose</c>, with d3's seeded random
/// source (a linear congruential generator), so the circles are d3's bit for bit.
/// </summary>
public static class Pack
{
    /// <summary>Lays out a summed <paramref name="hierarchy"/> as d3's <c>pack()</c>: each leaf's radius is the square root of its value, scaled to fit.</summary>
    /// <param name="hierarchy">The hierarchy, summed and in its current child order.</param>
    /// <param name="options">The size and padding.</param>
    /// <returns>Each node's circle.</returns>
    public static NodeCircles Compute(Hierarchy hierarchy, PackOptions options)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(options);
        var circles = Start(hierarchy, options);
        var values = hierarchy.Values;
        using var pre = new NodeOrder(hierarchy, NodeOrderKind.Pre);
        using var post = new NodeOrder(hierarchy, NodeOrderKind.Post);
        foreach (var node in pre.Nodes)
        {
            if (hierarchy.ChildCount(node) == 0)
            {
                circles.R[node] = Math.Max(0, Number(Math.Sqrt(values[node])));
            }
        }

        var random = Lcg.Create();
        var root = hierarchy.Root;
        var size = Math.Min(options.Width, options.Height);
        PackChildren(hierarchy, post.Nodes, circles, 0, 1, ref random);
        PackChildren(hierarchy, post.Nodes, circles, options.Padding, circles.R[root] / size, ref random);
        Translate(hierarchy, pre.Nodes, circles, size / (2 * circles.R[root]));
        return circles;
    }

    /// <summary>Lays out <paramref name="hierarchy"/> as d3's <c>pack().radius(r)</c>: the leaves keep the given radii.</summary>
    /// <param name="hierarchy">The hierarchy, in its current child order.</param>
    /// <param name="options">The size and padding.</param>
    /// <param name="radii">Each node's radius; only the leaves' are read, and NaN and negative radii count as zero.</param>
    /// <returns>Each node's circle.</returns>
    /// <exception cref="ArgumentException">The radii are not one per node.</exception>
    public static NodeCircles Compute(Hierarchy hierarchy, PackOptions options, ReadOnlySpan<double> radii)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(options);
        if (radii.Length != hierarchy.Count)
        {
            throw new ArgumentException("There must be one radius per node.", nameof(radii));
        }

        var circles = Start(hierarchy, options);
        using var pre = new NodeOrder(hierarchy, NodeOrderKind.Pre);
        using var post = new NodeOrder(hierarchy, NodeOrderKind.Post);
        foreach (var node in pre.Nodes)
        {
            if (hierarchy.ChildCount(node) == 0)
            {
                circles.R[node] = Math.Max(0, Number(radii[node]));
            }
        }

        var random = Lcg.Create();
        PackChildren(hierarchy, post.Nodes, circles, options.Padding, 0.5, ref random);
        Translate(hierarchy, pre.Nodes, circles, 1);
        return circles;
    }

    /// <summary>Packs circles of the given radii around the origin without overlap, as d3's <c>packSiblings</c>.</summary>
    /// <param name="radii">The circles' radii, in the order they are placed.</param>
    /// <param name="x">Receives each circle's center x.</param>
    /// <param name="y">Receives each circle's center y.</param>
    /// <returns>The radius of the circle that encloses them, centered on the origin; 0 for no circle.</returns>
    /// <exception cref="ArgumentException">The coordinate spans are shorter than the radii.</exception>
    public static double Siblings(ReadOnlySpan<double> radii, Span<double> x, Span<double> y)
    {
        var n = radii.Length;
        if (x.Length < n || y.Length < n)
        {
            throw new ArgumentException("There must be a coordinate per radius.", nameof(x));
        }

        var xs = new double[n];
        var ys = new double[n];
        var rs = radii.ToArray();
        var order = new int[n];
        for (var i = 0; i < n; i++)
        {
            order[i] = i;
        }

        var random = Lcg.Create();
        var radius = Siblings(order, new NodeCircles(xs, ys, rs), ref random);
        xs.CopyTo(x);
        ys.CopyTo(y);
        return radius;
    }

    /// <summary>Finds the smallest circle that encloses the given circles, as d3's <c>packEnclose</c> (Welzl's algorithm).</summary>
    /// <param name="circles">The circles.</param>
    /// <param name="enclosing">The enclosing circle.</param>
    /// <returns>False when there is no circle, as d3 returns undefined.</returns>
    public static bool Enclose(ReadOnlySpan<Circle> circles, out Circle enclosing)
    {
        var copy = circles.ToArray();
        var random = Lcg.Create();
        return Enclosure.Enclose(copy, ref random, out enclosing);
    }

    private static double Number(double value) => value == 0 || double.IsNaN(value) ? 0 : value;

    private static NodeCircles Start(Hierarchy hierarchy, PackOptions options)
    {
        var n = hierarchy.Count;
        var circles = new NodeCircles(new double[n], new double[n], new double[n]);
        circles.X[hierarchy.Root] = options.Width / 2;
        circles.Y[hierarchy.Root] = options.Height / 2;
        return circles;
    }

    private static void PackChildren(Hierarchy hierarchy, ReadOnlySpan<int> order, NodeCircles circles, double padding, double k, ref Lcg random)
    {
        var r = Number(padding * k);
        foreach (var node in order)
        {
            var children = hierarchy.Children(node);
            if (children.Length == 0)
            {
                continue;
            }

            if (r != 0)
            {
                foreach (var child in children)
                {
                    circles.R[child] += r;
                }
            }

            var e = Siblings(children, circles, ref random);
            if (r != 0)
            {
                foreach (var child in children)
                {
                    circles.R[child] -= r;
                }
            }

            circles.R[node] = e + r;
        }
    }

    private static void Translate(Hierarchy hierarchy, ReadOnlySpan<int> order, NodeCircles circles, double k)
    {
        var parents = hierarchy.Parents;
        foreach (var node in order)
        {
            circles.R[node] *= k;
            var parent = parents[node];
            if (parent >= 0)
            {
                circles.X[node] = circles.X[parent] + k * circles.X[node];
                circles.Y[node] = circles.Y[parent] + k * circles.Y[node];
            }
        }
    }

    private static double Siblings(ReadOnlySpan<int> circles, NodeCircles c, ref Lcg random)
    {
        var n = circles.Length;
        if (n == 0)
        {
            return 0;
        }

        var (x, y, r) = (c.X, c.Y, c.R);
        var first = circles[0];
        x[first] = 0;
        y[first] = 0;
        if (n == 1)
        {
            return r[first];
        }

        var second = circles[1];
        x[first] = -r[second];
        x[second] = r[first];
        y[second] = 0;
        if (n == 2)
        {
            return r[first] + r[second];
        }

        Place(c, second, first, circles[2]);
        var chain = new FrontChain(n);
        try
        {
            return chain.Pack(circles, c, ref random);
        }
        finally
        {
            chain.Dispose();
        }
    }

    private static void Place(NodeCircles c, int b, int a, int target)
    {
        var (x, y, r) = (c.X, c.Y, c.R);
        var dx = x[b] - x[a];
        var dy = y[b] - y[a];
        var d2 = dx * dx + dy * dy;
        if (d2 != 0 && !double.IsNaN(d2))
        {
            var a2 = r[a] + r[target];
            a2 *= a2;
            var b2 = r[b] + r[target];
            b2 *= b2;
            if (a2 > b2)
            {
                var px = (d2 + b2 - a2) / (2 * d2);
                var py = Math.Sqrt(Math.Max(0, b2 / d2 - px * px));
                x[target] = x[b] - px * dx - py * dy;
                y[target] = y[b] - px * dy + py * dx;
            }
            else
            {
                var px = (d2 + a2 - b2) / (2 * d2);
                var py = Math.Sqrt(Math.Max(0, a2 / d2 - px * px));
                x[target] = x[a] + px * dx - py * dy;
                y[target] = y[a] + px * dy + py * dx;
            }
        }
        else
        {
            x[target] = x[a] + r[target];
            y[target] = y[a];
        }
    }

    private readonly struct FrontChain(int n) : IDisposable
    {
        private readonly int[] next = ArrayPool<int>.Shared.Rent(n);
        private readonly int[] previous = ArrayPool<int>.Shared.Rent(n);

        public double Pack(ReadOnlySpan<int> circles, NodeCircles c, ref Lcg random)
        {
            var (x, y, r) = (c.X, c.Y, c.R);
            int a = 0, b = 1;
            next[0] = 1;
            previous[2] = 1;
            next[1] = 2;
            previous[0] = 2;
            next[2] = 0;
            previous[1] = 0;
            var i = 3;
            while (i < circles.Length)
            {
                Place(c, circles[a], circles[b], circles[i]);
                if (Intersecting(circles, c, ref a, ref b, i))
                {
                    continue;
                }

                previous[i] = a;
                next[i] = b;
                previous[b] = i;
                next[a] = i;
                b = i;
                var best = Score(circles, c, a);
                for (var k = next[i]; k != b; k = next[k])
                {
                    var score = Score(circles, c, k);
                    if (score < best)
                    {
                        a = k;
                        best = score;
                    }
                }

                b = next[a];
                i++;
            }

            var count = 1;
            for (var k = next[b]; k != b; k = next[k])
            {
                count++;
            }

            var chain = new Circle[count];
            chain[0] = new Circle(x[circles[b]], y[circles[b]], r[circles[b]]);
            count = 1;
            for (var k = next[b]; k != b; k = next[k])
            {
                chain[count++] = new Circle(x[circles[k]], y[circles[k]], r[circles[k]]);
            }

            Enclosure.Enclose(chain, ref random, out var e);
            foreach (var circle in circles)
            {
                x[circle] -= e.X;
                y[circle] -= e.Y;
            }

            return e.R;
        }

        public void Dispose()
        {
            ArrayPool<int>.Shared.Return(next);
            ArrayPool<int>.Shared.Return(previous);
        }

        private bool Intersecting(ReadOnlySpan<int> circles, NodeCircles c, ref int a, ref int b, int target)
        {
            var r = c.R;
            var j = next[b];
            var k = previous[a];
            var sj = r[circles[b]];
            var sk = r[circles[a]];
            do
            {
                if (sj <= sk)
                {
                    if (Intersects(c, circles[j], circles[target]))
                    {
                        b = j;
                        next[a] = b;
                        previous[b] = a;
                        return true;
                    }

                    sj += r[circles[j]];
                    j = next[j];
                }
                else
                {
                    if (Intersects(c, circles[k], circles[target]))
                    {
                        a = k;
                        next[a] = b;
                        previous[b] = a;
                        return true;
                    }

                    sk += r[circles[k]];
                    k = previous[k];
                }
            }
            while (j != next[k]);

            return false;
        }

        private static bool Intersects(NodeCircles c, int a, int b)
        {
            var dr = c.R[a] + c.R[b] - 1e-6;
            var dx = c.X[b] - c.X[a];
            var dy = c.Y[b] - c.Y[a];
            return dr > 0 && dr * dr > dx * dx + dy * dy;
        }

        private double Score(ReadOnlySpan<int> circles, NodeCircles c, int node)
        {
            var a = circles[node];
            var b = circles[next[node]];
            var ab = c.R[a] + c.R[b];
            var dx = (c.X[a] * c.R[b] + c.X[b] * c.R[a]) / ab;
            var dy = (c.Y[a] * c.R[b] + c.Y[b] * c.R[a]) / ab;
            return dx * dx + dy * dy;
        }
    }
}
