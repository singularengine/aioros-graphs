namespace Aioros.Graphs.Forces;

/// <summary>d3-force's x-positioning force: pushes each node's horizontal velocity toward a target position.</summary>
public sealed class XForce : IForce
{
    private PositionTargets targets;

    /// <summary>Every node's target, 0 by default.</summary>
    public double X { get; init; }

    /// <summary>Each node's target, in place of <see cref="X"/>; a NaN target gets strength 0, as in d3.</summary>
    public IReadOnlyList<double>? Targets { get; init; }

    /// <summary>Every node's strength, 0.1 by default.</summary>
    public double Strength { get; init; } = 0.1;

    /// <summary>Each node's strength, in place of <see cref="Strength"/>.</summary>
    public IReadOnlyList<double>? Strengths { get; init; }

    /// <inheritdoc/>
    public void Initialize(ForceSimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        targets = PositionTargets.Create(simulation.Count, X, Targets, Strength, Strengths);
    }

    /// <inheritdoc/>
    public void Apply(ForceSimulation simulation, double alpha)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        targets.Apply(simulation.Xs, simulation.Vxs, alpha);
    }
}

/// <summary>d3-force's y-positioning force: pushes each node's vertical velocity toward a target position.</summary>
public sealed class YForce : IForce
{
    private PositionTargets targets;

    /// <summary>Every node's target, 0 by default.</summary>
    public double Y { get; init; }

    /// <summary>Each node's target, in place of <see cref="Y"/>; a NaN target gets strength 0, as in d3.</summary>
    public IReadOnlyList<double>? Targets { get; init; }

    /// <summary>Every node's strength, 0.1 by default.</summary>
    public double Strength { get; init; } = 0.1;

    /// <summary>Each node's strength, in place of <see cref="Strength"/>.</summary>
    public IReadOnlyList<double>? Strengths { get; init; }

    /// <inheritdoc/>
    public void Initialize(ForceSimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        targets = PositionTargets.Create(simulation.Count, Y, Targets, Strength, Strengths);
    }

    /// <inheritdoc/>
    public void Apply(ForceSimulation simulation, double alpha)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        targets.Apply(simulation.Ys, simulation.Vys, alpha);
    }
}

internal readonly record struct PositionTargets(double[] Positions, double[] Strengths)
{
    public static PositionTargets Create(int n, double position, IReadOnlyList<double>? positions, double strength, IReadOnlyList<double>? strengths)
    {
        LinkForce.Check(positions, n, "Targets");
        LinkForce.Check(strengths, n, "Strengths");
        var target = new double[n];
        var k = new double[n];
        for (var i = 0; i < n; i++)
        {
            target[i] = positions is null ? position : positions[i];
            k[i] = double.IsNaN(target[i]) ? 0 : strengths is null ? strength : strengths[i];
        }

        return new PositionTargets(target, k);
    }

    public void Apply(double[] position, double[] velocity, double alpha)
    {
        var target = Positions ?? [];
        var k = Strengths ?? [];
        for (var i = 0; i < target.Length; i++)
        {
            velocity[i] += (target[i] - position[i]) * k[i] * alpha;
        }
    }
}
