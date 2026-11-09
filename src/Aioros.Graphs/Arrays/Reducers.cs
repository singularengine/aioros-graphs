namespace Aioros.Graphs.Arrays;

/// <summary>Reduces the values of a group to one number, for <see cref="Grouping.Rollup{TKey, TReducer}"/>.</summary>
public interface IGroupReducer
{
    /// <summary>Reduces <paramref name="values"/>.</summary>
    /// <param name="values">The group's values, in item order.</param>
    /// <returns>The reduced value.</returns>
    double Reduce(ReadOnlySpan<double> values);
}

/// <summary>The sum of a group, as <c>d3.sum</c>.</summary>
public readonly struct SumReducer : IGroupReducer
{
    /// <inheritdoc />
    public double Reduce(ReadOnlySpan<double> values) => Statistics.Sum(values);
}

/// <summary>The mean of a group, as <c>d3.mean</c>.</summary>
public readonly struct MeanReducer : IGroupReducer
{
    /// <inheritdoc />
    public double Reduce(ReadOnlySpan<double> values) => Statistics.Mean(values);
}

/// <summary>The number of items in a group, NaN values included, as <c>values.length</c>.</summary>
public readonly struct CountReducer : IGroupReducer
{
    /// <inheritdoc />
    public double Reduce(ReadOnlySpan<double> values) => values.Length;
}

/// <summary>The minimum of a group, as <c>d3.min</c>.</summary>
public readonly struct MinReducer : IGroupReducer
{
    /// <inheritdoc />
    public double Reduce(ReadOnlySpan<double> values) => Statistics.Min(values);
}

/// <summary>The maximum of a group, as <c>d3.max</c>.</summary>
public readonly struct MaxReducer : IGroupReducer
{
    /// <inheritdoc />
    public double Reduce(ReadOnlySpan<double> values) => Statistics.Max(values);
}

/// <summary>The median of a group, as <c>d3.median</c>.</summary>
public readonly struct MedianReducer : IGroupReducer
{
    /// <inheritdoc />
    public double Reduce(ReadOnlySpan<double> values) => Statistics.Median(values);
}
