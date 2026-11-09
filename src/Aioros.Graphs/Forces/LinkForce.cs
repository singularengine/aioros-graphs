namespace Aioros.Graphs.Forces;

/// <summary>
/// d3-force's link force: pulls or pushes each link's ends toward its distance. Links join node indices; by default a
/// link's strength is 1 over the smaller link count of its ends, and the ends move in proportion to their counts.
/// </summary>
public sealed class LinkForce : IForce
{
    private readonly int[] sources;
    private readonly int[] targets;
    private double[] bias = [];
    private double[] strengths = [];
    private double[] distances = [];

    /// <summary>Creates a link force over links from <paramref name="sources"/> to <paramref name="targets"/>.</summary>
    /// <param name="sources">Each link's source node.</param>
    /// <param name="targets">Each link's target node.</param>
    /// <exception cref="ArgumentException">The spans differ in length.</exception>
    public LinkForce(ReadOnlySpan<int> sources, ReadOnlySpan<int> targets)
    {
        if (targets.Length != sources.Length)
        {
            throw new ArgumentException("There must be one target per source.", nameof(targets));
        }

        this.sources = sources.ToArray();
        this.targets = targets.ToArray();
    }

    /// <summary>Every link's distance, 30 by default.</summary>
    public double Distance { get; init; } = 30;

    /// <summary>Each link's distance, in place of <see cref="Distance"/>.</summary>
    public IReadOnlyList<double>? Distances { get; init; }

    /// <summary>Every link's strength, in place of d3's default of 1 over the smaller link count of its ends.</summary>
    public double? Strength { get; init; }

    /// <summary>Each link's strength, in place of <see cref="Strength"/>.</summary>
    public IReadOnlyList<double>? Strengths { get; init; }

    /// <summary>How many times the force is applied per tick, 1 by default.</summary>
    public int Iterations { get; init; } = 1;

    /// <inheritdoc/>
    public void Initialize(ForceSimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        var n = simulation.Count;
        var m = sources.Length;
        Check(Distances, m, nameof(Distances));
        Check(Strengths, m, nameof(Strengths));
        var count = new int[n];
        for (var i = 0; i < m; i++)
        {
            Endpoint(sources[i], n, i);
            Endpoint(targets[i], n, i);
            count[sources[i]]++;
            count[targets[i]]++;
        }

        bias = new double[m];
        strengths = new double[m];
        distances = new double[m];
        for (var i = 0; i < m; i++)
        {
            double s = count[sources[i]];
            double t = count[targets[i]];
            bias[i] = s / (s + t);
            strengths[i] = Strengths is { } each ? each[i] : Strength ?? 1 / Math.Min(s, t);
            distances[i] = Distances is { } lengths ? lengths[i] : Distance;
        }
    }

    /// <inheritdoc/>
    public void Apply(ForceSimulation simulation, double alpha)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        var x = simulation.X;
        var y = simulation.Y;
        var vx = simulation.VelocityX;
        var vy = simulation.VelocityY;
        for (var k = 0; k < Iterations; k++)
        {
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                var target = targets[i];
                var dx = x[target] + vx[target] - x[source] - vx[source];
                if (dx == 0 || double.IsNaN(dx))
                {
                    dx = simulation.Jiggle();
                }

                var dy = y[target] + vy[target] - y[source] - vy[source];
                if (dy == 0 || double.IsNaN(dy))
                {
                    dy = simulation.Jiggle();
                }

                var l = Math.Sqrt(dx * dx + dy * dy);
                l = (l - distances[i]) / l * alpha * strengths[i];
                dx *= l;
                dy *= l;
                var b = bias[i];
                vx[target] -= dx * b;
                vy[target] -= dy * b;
                b = 1 - b;
                vx[source] += dx * b;
                vy[source] += dy * b;
            }
        }
    }

    private static void Endpoint(int node, int count, int link)
    {
        if ((uint)node >= (uint)count)
        {
            throw new ArgumentOutOfRangeException(nameof(node), node, $"Link {link} names a node outside the simulation's {count}.");
        }
    }

    internal static void Check(IReadOnlyList<double>? values, int count, string name)
    {
        if (values is not null && values.Count != count)
        {
            throw new ArgumentException($"There must be one value per item ({count}).", name);
        }
    }
}
