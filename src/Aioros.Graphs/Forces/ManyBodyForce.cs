namespace Aioros.Graphs.Forces;

/// <summary>
/// d3-force's many-body force: every node attracts (positive strength) or repels (negative) every other, approximated
/// with the Barnes–Hut algorithm over a quadtree of the nodes' positions, built each tick as d3-quadtree builds it.
/// </summary>
public sealed class ManyBodyForce : IForce
{
    private readonly QuadTree tree = new();
    private double[] strengths = [];
    private double theta2 = 0.81;
    private double distanceMin2 = 1;
    private double distanceMax2 = double.PositiveInfinity;

    /// <summary>Every node's strength, −30 by default.</summary>
    public double Strength { get; init; } = -30;

    /// <summary>Each node's strength, in place of <see cref="Strength"/>.</summary>
    public IReadOnlyList<double>? Strengths { get; init; }

    /// <summary>The Barnes–Hut accuracy, 0.9 by default; 0 computes every pair.</summary>
    public double Theta
    {
        get => Math.Sqrt(theta2);
        init => theta2 = value * value;
    }

    /// <summary>The distance below which forces are bounded, 1 by default.</summary>
    public double DistanceMin
    {
        get => Math.Sqrt(distanceMin2);
        init => distanceMin2 = value * value;
    }

    /// <summary>The distance beyond which forces are ignored, unbounded by default.</summary>
    public double DistanceMax
    {
        get => Math.Sqrt(distanceMax2);
        init => distanceMax2 = value * value;
    }

    /// <inheritdoc/>
    public void Initialize(ForceSimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        LinkForce.Check(Strengths, simulation.Count, nameof(Strengths));
        strengths = new double[simulation.Count];
        for (var i = 0; i < strengths.Length; i++)
        {
            strengths[i] = Strengths is { } each ? each[i] : Strength;
        }
    }

    /// <inheritdoc/>
    public void Apply(ForceSimulation simulation, double alpha)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        tree.Build(simulation.Xs, simulation.Ys, simulation.Count);
        var accumulator = new Accumulator(strengths, simulation.Xs, simulation.Ys);
        tree.VisitAfter(ref accumulator);
        var visitor = new Visitor(simulation, strengths, alpha, theta2, distanceMin2, distanceMax2);
        for (var i = 0; i < simulation.Count; i++)
        {
            visitor.Node = i;
            tree.Visit(ref visitor);
        }
    }

    private readonly struct Accumulator(double[] strengths, double[] x, double[] y) : IQuadAccumulator
    {
        public void Accumulate(QuadTree tree, int quad)
        {
            var value = tree.Value;
            var strength = 0.0;
            if (tree.IsInternal(quad))
            {
                double weight = 0, cx = 0, cy = 0;
                for (var i = 0; i < 4; i++)
                {
                    var q = tree.Child(quad, i);
                    double c;
                    if (q >= 0 && (c = Math.Abs(value[q])) != 0 && !double.IsNaN(c))
                    {
                        strength += value[q];
                        weight += c;
                        cx += c * tree.CenterX[q];
                        cy += c * tree.CenterY[q];
                    }
                }

                tree.CenterX[quad] = cx / weight;
                tree.CenterY[quad] = cy / weight;
            }
            else
            {
                var d = tree.Data(quad);
                tree.CenterX[quad] = x[d];
                tree.CenterY[quad] = y[d];
                for (var q = quad; q >= 0; q = tree.Next(q))
                {
                    strength += strengths[tree.Data(q)];
                }
            }

            value[quad] = strength;
        }
    }

    private struct Visitor(ForceSimulation simulation, double[] strengths, double alpha, double theta2, double distanceMin2, double distanceMax2) : IQuadVisitor
    {
        public int Node { get; set; }

        public readonly bool Visit(QuadTree tree, int quad, double x0, double y0, double x1, double y1)
        {
            var value = tree.Value[quad];
            if (value == 0 || double.IsNaN(value))
            {
                return true;
            }

            var node = Node;
            var x = tree.CenterX[quad] - simulation.Xs[node];
            var y = tree.CenterY[quad] - simulation.Ys[node];
            var w = x1 - x0;
            var l = x * x + y * y;
            if (w * w / theta2 < l)
            {
                if (l < distanceMax2)
                {
                    Separate(ref x, ref y, ref l);
                    simulation.Vxs[node] += x * value * alpha / l;
                    simulation.Vys[node] += y * value * alpha / l;
                }

                return true;
            }

            if (tree.IsInternal(quad) || l >= distanceMax2)
            {
                return false;
            }

            if (tree.Data(quad) != node || tree.Next(quad) >= 0)
            {
                Separate(ref x, ref y, ref l);
            }

            for (var q = quad; q >= 0; q = tree.Next(q))
            {
                var d = tree.Data(q);
                if (d != node)
                {
                    w = strengths[d] * alpha / l;
                    simulation.Vxs[node] += x * w;
                    simulation.Vys[node] += y * w;
                }
            }

            return false;
        }

        private readonly void Separate(ref double x, ref double y, ref double l)
        {
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

            if (l < distanceMin2)
            {
                l = Math.Sqrt(distanceMin2 * l);
            }
        }
    }
}
