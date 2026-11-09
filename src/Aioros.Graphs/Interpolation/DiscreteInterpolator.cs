namespace Aioros.Graphs.Interpolation;

/// <summary>
/// Picks one of <c>n</c> values, as d3's <c>interpolateDiscrete(values)</c>: the value at <c>floor(t * n)</c>, the
/// first below 0 and the last from 1; a NaN position gives the default value, where d3 gives undefined.
/// </summary>
/// <typeparam name="T">The values.</typeparam>
public readonly struct DiscreteInterpolator<T> : IInterpolator<T?>
{
    private readonly T[] values;

    /// <summary>Initializes a new instance of the <see cref="DiscreteInterpolator{T}"/> struct.</summary>
    /// <param name="values">The values.</param>
    public DiscreteInterpolator(ReadOnlySpan<T> values)
    {
        this.values = values.ToArray();
    }

    /// <inheritdoc />
    public T? Interpolate(double t)
    {
        var i = Math.Max(0, Math.Min(values.Length - 1, Math.Floor(t * values.Length)));
        return double.IsNaN(i) || values.Length == 0 ? default : values[(int)i];
    }
}
