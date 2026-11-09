namespace Aioros.Graphs.Forces;

/// <summary>
/// d3-force's collision force: treats nodes as circles and pushes overlapping ones apart, using a quadtree of the
/// positions each node is about to take (position plus velocity), as d3 does.
/// </summary>
public sealed class CollideForce : IForce
{
    private readonly QuadTree tree = new();
    private double[] radii = [];
    private double[] nextX = [];
    private double[] nextY = [];

    /// <summary>Every node's radius, 1 by default.</summary>
    public double Radius { get; init; } = 1;

    /// <summary>Each node's radius, in place of <see cref="Radius"/>.</summary>
    public IReadOnlyList<double>? Radii { get; init; }

    /// <summary>How much of an overlap is resolved each iteration, 1 by default.</summary>
    public double Strength { get; init; } = 1;

    /// <summary>How many times the force is applied per tick, 1 by default.</summary>
    public int Iterations { get; init; } = 1;

    /// <inheritdoc/>
    public void Initialize(ForceSimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        var n = simulation.Count;
        LinkForce.Check(Radii, n, nameof(Radii));
        radii = new double[n];
        nextX = new double[n];
        nextY = new double[n];
        for (var i = 0; i < n; i++)
        {
            radii[i] = Radii is { } each ? each[i] : Radius;
        }
    }

    /// <inheritdoc/>
    public void Apply(ForceSimulation simulation, double alpha)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        var x = simulation.Xs;
        var y = simulation.Ys;
        var vx = simulation.Vxs;
        var vy = simulation.Vys;
        for (var k = 0; k < Iterations; k++)
        {
            for (var i = 0; i < x.Length; i++)
            {
                nextX[i] = x[i] + vx[i];
                nextY[i] = y[i] + vy[i];
            }

            tree.Build(nextX, nextY, x.Length);
            var prepare = new Prepare(radii);
            tree.VisitAfter(ref prepare);
            var visitor = new Visitor(simulation, radii, Strength);
            for (var i = 0; i < x.Length; i++)
            {
                visitor.Start(i, x[i] + vx[i], y[i] + vy[i]);
                tree.Visit(ref visitor);
            }
        }
    }

    private readonly struct Prepare(double[] radii) : IQuadAccumulator
    {
        public void Accumulate(QuadTree tree, int quad)
        {
            var r = tree.Value;
            if (!tree.IsInternal(quad))
            {
                r[quad] = radii[tree.Data(quad)];
                return;
            }

            r[quad] = 0;
            for (var i = 0; i < 4; i++)
            {
                var child = tree.Child(quad, i);
                if (child >= 0 && r[child] > r[quad])
                {
                    r[quad] = r[child];
                }
            }
        }
    }

    private struct Visitor(ForceSimulation simulation, double[] radii, double strength) : IQuadVisitor
    {
        private int node;
        private double xi;
        private double yi;
        private double ri;
        private double ri2;

        public void Start(int index, double x, double y)
        {
            node = index;
            xi = x;
            yi = y;
            ri = radii[index];
            ri2 = ri * ri;
        }

        public readonly bool Visit(QuadTree tree, int quad, double x0, double y0, double x1, double y1)
        {
            var rj = tree.Value[quad];
            var r = ri + rj;
            if (tree.IsInternal(quad))
            {
                return x0 > xi + r || x1 < xi - r || y0 > yi + r || y1 < yi - r;
            }

            var data = tree.Data(quad);
            if (data > node)
            {
                Push(data, rj, r);
            }

            return false;
        }

        private readonly void Push(int data, double rj, double r)
        {
            var vx = simulation.Vxs;
            var vy = simulation.Vys;
            var x = xi - simulation.Xs[data] - vx[data];
            var y = yi - simulation.Ys[data] - vy[data];
            var l = x * x + y * y;
            if (!(l < r * r))
            {
                return;
            }

            if (x == 0)
            {
                x = simulation.Jiggle();
                l += x * x;
            }

            if (y == 0)
            {
                y = simulation.Jiggle();
                l += y * y;
            }

            l = Math.Sqrt(l);
            l = (r - l) / l * strength;
            rj *= rj;
            var share = rj / (ri2 + rj);
            x *= l;
            vx[node] += x * share;
            y *= l;
            vy[node] += y * share;
            share = 1 - share;
            vx[data] -= x * share;
            vy[data] -= y * share;
        }
    }
}
