using Aioros.Graphs.Scales;

namespace Aioros.Graphs.Forces;

/// <summary>A force of a <see cref="ForceSimulation"/>, as d3-force's force functions.</summary>
public interface IForce
{
    /// <summary>Prepares the force for the simulation's nodes, as d3's <c>force.initialize(nodes, random)</c>.</summary>
    /// <param name="simulation">The simulation the force is added to.</param>
    void Initialize(ForceSimulation simulation);

    /// <summary>Applies the force to the nodes' velocities (or positions), as d3's <c>force(alpha)</c>.</summary>
    /// <param name="simulation">The simulation.</param>
    /// <param name="alpha">The simulation's current alpha.</param>
    void Apply(ForceSimulation simulation, double alpha);
}

/// <summary>
/// d3-force's simulation, stepped explicitly: nodes are indices, their positions and velocities spans of numbers. It
/// places nodes without a position on d3's phyllotaxis spiral, applies its forces in the order they were added and
/// draws randomness from d3's seeded linear congruential generator, so every tick matches d3's bit for bit.
/// </summary>
public sealed class ForceSimulation
{
    private const double InitialRadius = 10;

    private static readonly double InitialAngle = Math.PI * (3 - Math.Sqrt(5));

    private readonly double[] x;
    private readonly double[] y;
    private readonly double[] vx;
    private readonly double[] vy;
    private readonly double[] fx;
    private readonly double[] fy;
    private readonly List<(string Name, IForce Force)> forces = [];
    private ulong seed = 1;
    private double velocityFactor = 0.6;

    /// <summary>Creates a simulation of <paramref name="count"/> nodes on d3's phyllotaxis spiral.</summary>
    /// <param name="count">The number of nodes.</param>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    public ForceSimulation(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        x = new double[count];
        y = new double[count];
        vx = new double[count];
        vy = new double[count];
        fx = new double[count];
        fy = new double[count];
        x.AsSpan().Fill(double.NaN);
        fx.AsSpan().Fill(double.NaN);
        fy.AsSpan().Fill(double.NaN);
        Initialize();
    }

    /// <summary>Creates a simulation of nodes at the given positions; a node with a NaN coordinate goes on the spiral.</summary>
    /// <param name="x">The nodes' horizontal positions.</param>
    /// <param name="y">The nodes' vertical positions.</param>
    /// <exception cref="ArgumentException">The spans differ in length.</exception>
    public ForceSimulation(ReadOnlySpan<double> x, ReadOnlySpan<double> y)
        : this(x, y, [], [])
    {
    }

    /// <summary>
    /// Creates a simulation of nodes at the given positions, some of them fixed as d3's <c>fx</c> and <c>fy</c>: a
    /// fixed coordinate is NaN for a free node, and a fixed node starts at its fixed position.
    /// </summary>
    /// <param name="x">The nodes' horizontal positions.</param>
    /// <param name="y">The nodes' vertical positions.</param>
    /// <param name="fixedX">The fixed horizontal positions, NaN where free, or empty for none.</param>
    /// <param name="fixedY">The fixed vertical positions, NaN where free, or empty for none.</param>
    /// <exception cref="ArgumentException">The spans differ in length.</exception>
    public ForceSimulation(ReadOnlySpan<double> x, ReadOnlySpan<double> y, ReadOnlySpan<double> fixedX, ReadOnlySpan<double> fixedY)
    {
        if (y.Length != x.Length)
        {
            throw new ArgumentException("There must be one y per x.", nameof(y));
        }

        if (!fixedX.IsEmpty && fixedX.Length != x.Length)
        {
            throw new ArgumentException("There must be one fixed x per node, or none.", nameof(fixedX));
        }

        if (!fixedY.IsEmpty && fixedY.Length != x.Length)
        {
            throw new ArgumentException("There must be one fixed y per node, or none.", nameof(fixedY));
        }

        this.x = x.ToArray();
        this.y = y.ToArray();
        vx = new double[x.Length];
        vy = new double[x.Length];
        fx = fixedX.IsEmpty ? new double[x.Length] : fixedX.ToArray();
        fy = fixedY.IsEmpty ? new double[x.Length] : fixedY.ToArray();
        if (fixedX.IsEmpty)
        {
            fx.AsSpan().Fill(double.NaN);
        }

        if (fixedY.IsEmpty)
        {
            fy.AsSpan().Fill(double.NaN);
        }

        Initialize();
    }

    /// <summary>The number of nodes.</summary>
    public int Count => x.Length;

    /// <summary>The nodes' horizontal positions.</summary>
    public Span<double> X => x;

    /// <summary>The nodes' vertical positions.</summary>
    public Span<double> Y => y;

    /// <summary>The nodes' horizontal velocities.</summary>
    public Span<double> VelocityX => vx;

    /// <summary>The nodes' vertical velocities.</summary>
    public Span<double> VelocityY => vy;

    /// <summary>The nodes' fixed horizontal positions, NaN where free (d3's <c>fx</c>).</summary>
    public Span<double> FixedX => fx;

    /// <summary>The nodes' fixed vertical positions, NaN where free (d3's <c>fy</c>).</summary>
    public Span<double> FixedY => fy;

    /// <summary>The current alpha, 1 at the start.</summary>
    public double Alpha { get; set; } = 1;

    /// <summary>The alpha below which <see cref="Run"/> stops, 0.001 by default.</summary>
    public double AlphaMin { get; set; } = 0.001;

    /// <summary>How fast alpha moves toward its target each tick, 1 − 0.001^(1/300) by default.</summary>
    public double AlphaDecay { get; set; } = 0.02276277904418933;

    /// <summary>The alpha the simulation moves toward, 0 by default.</summary>
    public double AlphaTarget { get; set; }

    /// <summary>The fraction of velocity lost each tick, 0.4 by default.</summary>
    public double VelocityDecay
    {
        get => 1 - velocityFactor;
        set => velocityFactor = 1 - value;
    }

    /// <summary>Adds, replaces or removes the force named <paramref name="name"/>, as d3's <c>simulation.force(name, force)</c>: a replaced force keeps its place in the order.</summary>
    /// <param name="name">The force's name.</param>
    /// <param name="force">The force, or null to remove it.</param>
    /// <returns>This simulation.</returns>
    public ForceSimulation Force(string name, IForce? force)
    {
        ArgumentNullException.ThrowIfNull(name);
        var index = forces.Count - 1;
        while (index >= 0 && forces[index].Name != name)
        {
            index--;
        }

        if (force is null)
        {
            if (index >= 0)
            {
                forces.RemoveAt(index);
            }

            return this;
        }

        force.Initialize(this);
        if (index >= 0)
        {
            forces[index] = (name, force);
        }
        else
        {
            forces.Add((name, force));
        }

        return this;
    }

    /// <summary>Advances the simulation by <paramref name="iterations"/> ticks, as d3's <c>simulation.tick(iterations)</c>.</summary>
    /// <param name="iterations">The number of ticks.</param>
    public void Tick(int iterations = 1)
    {
        for (var k = 0; k < iterations; k++)
        {
            Alpha += (AlphaTarget - Alpha) * AlphaDecay;
            for (var f = 0; f < forces.Count; f++)
            {
                forces[f].Force.Apply(this, Alpha);
            }

            Move();
        }
    }

    /// <summary>Ticks as d3's timer does until alpha falls below <see cref="AlphaMin"/>, at least once.</summary>
    /// <param name="maxTicks">The most ticks to run, for a target alpha that never falls below the minimum.</param>
    /// <returns>The number of ticks run.</returns>
    public int Run(int maxTicks = 10_000)
    {
        var ticks = 0;
        do
        {
            Tick();
            ticks++;
        }
        while (!(Alpha < AlphaMin) && ticks < maxTicks);

        return ticks;
    }

    /// <summary>Returns the node closest to a point within <paramref name="radius"/>, as d3's <c>simulation.find</c>.</summary>
    /// <param name="px">The point's horizontal coordinate.</param>
    /// <param name="py">The point's vertical coordinate.</param>
    /// <param name="radius">The search radius, unbounded by default.</param>
    /// <returns>The node's index, or −1 when none is found.</returns>
    public int Find(double px, double py, double radius = double.PositiveInfinity)
    {
        var limit = radius * radius;
        var closest = -1;
        for (var i = 0; i < x.Length; i++)
        {
            var dx = px - x[i];
            var dy = py - y[i];
            var d2 = dx * dx + dy * dy;
            if (d2 < limit)
            {
                closest = i;
                limit = d2;
            }
        }

        return closest;
    }

    /// <summary>Returns the next number of d3's random source, a linear congruential generator seeded with 1.</summary>
    /// <returns>A number in [0, 1).</returns>
    public double NextRandom()
    {
        seed = (1664525 * seed + 1013904223) & 0xFFFFFFFF;
        return seed / 4294967296.0;
    }

    internal double[] Xs => x;

    internal double[] Ys => y;

    internal double[] Vxs => vx;

    internal double[] Vys => vy;

    internal double Jiggle() => (NextRandom() - 0.5) * 1e-6;

    private void Move()
    {
        var decay = velocityFactor;
        for (var i = 0; i < x.Length; i++)
        {
            if (double.IsNaN(fx[i]))
            {
                vx[i] *= decay;
                x[i] += vx[i];
            }
            else
            {
                x[i] = fx[i];
                vx[i] = 0;
            }

            if (double.IsNaN(fy[i]))
            {
                vy[i] *= decay;
                y[i] += vy[i];
            }
            else
            {
                y[i] = fy[i];
                vy[i] = 0;
            }
        }
    }

    private void Initialize()
    {
        for (var i = 0; i < x.Length; i++)
        {
            if (!double.IsNaN(fx[i]))
            {
                x[i] = fx[i];
            }

            if (!double.IsNaN(fy[i]))
            {
                y[i] = fy[i];
            }

            if (double.IsNaN(x[i]) || double.IsNaN(y[i]))
            {
                var radius = InitialRadius * Math.Sqrt(0.5 + i);
                var angle = i * InitialAngle;
                x[i] = radius * JsTrig.Cos(angle);
                y[i] = radius * JsTrig.Sin(angle);
            }
        }
    }
}
