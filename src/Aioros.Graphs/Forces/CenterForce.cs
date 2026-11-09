namespace Aioros.Graphs.Forces;

/// <summary>d3-force's centering force: translates every node so that their mean position moves to the center.</summary>
public sealed class CenterForce : IForce
{
    /// <summary>The center's horizontal coordinate, 0 by default.</summary>
    public double X { get; init; }

    /// <summary>The center's vertical coordinate, 0 by default.</summary>
    public double Y { get; init; }

    /// <summary>How far the nodes move toward the center each tick, 1 by default.</summary>
    public double Strength { get; init; } = 1;

    /// <inheritdoc/>
    public void Initialize(ForceSimulation simulation) => ArgumentNullException.ThrowIfNull(simulation);

    /// <inheritdoc/>
    public void Apply(ForceSimulation simulation, double alpha)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        var x = simulation.Xs;
        var y = simulation.Ys;
        var n = x.Length;
        double sx = 0, sy = 0;
        for (var i = 0; i < n; i++)
        {
            sx += x[i];
            sy += y[i];
        }

        sx = (sx / n - X) * Strength;
        sy = (sy / n - Y) * Strength;
        for (var i = 0; i < n; i++)
        {
            x[i] -= sx;
            y[i] -= sy;
        }
    }
}
