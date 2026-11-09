namespace Aioros.Graphs.Shapes;

/// <summary>The order series are stacked in, as d3's <c>stackOrder*</c>.</summary>
public enum StackOrder
{
    /// <summary>In series order.</summary>
    None,

    /// <summary>In reverse series order.</summary>
    Reverse,

    /// <summary>The smallest sum at the bottom.</summary>
    Ascending,

    /// <summary>The largest sum at the bottom.</summary>
    Descending,

    /// <summary>The earliest peaks in the middle, later ones outside, as for streamgraphs.</summary>
    InsideOut,

    /// <summary>The earliest peak at the bottom.</summary>
    Appearance,
}

/// <summary>How stacked series are positioned, as d3's <c>stackOffset*</c>.</summary>
public enum StackOffset
{
    /// <summary>From a zero baseline.</summary>
    None,

    /// <summary>Normalized so each point's total is 1.</summary>
    Expand,

    /// <summary>Positive values above zero, negative values below.</summary>
    Diverging,

    /// <summary>Centered around zero.</summary>
    Silhouette,

    /// <summary>Minimizing the weighted change of slope, for streamgraphs.</summary>
    Wiggle,
}

/// <summary>A stacked value: where its band starts and ends.</summary>
/// <param name="Lower">The band's lower bound.</param>
/// <param name="Upper">The band's upper bound.</param>
public readonly record struct StackPoint(double Lower, double Upper);

/// <summary>The stack generator of d3-shape: stacks series point by point, for stacked bars and areas.</summary>
public static class Stacks
{
    /// <summary>Stacks <paramref name="seriesCount"/> series.</summary>
    /// <param name="values">The values row by row: point <c>j</c> of series <c>i</c> at <c>j * seriesCount + i</c>.</param>
    /// <param name="seriesCount">The number of series.</param>
    /// <param name="points">Receives the bands series by series: point <c>j</c> of series <c>i</c> at <c>i * pointCount + j</c>.</param>
    /// <param name="order">Receives the stacking order: <c>order[k]</c> is the series at position <c>k</c>, from the bottom.</param>
    /// <param name="stackOrder">How series are ordered.</param>
    /// <param name="offset">How series are positioned.</param>
    public static void Compute(ReadOnlySpan<double> values, int seriesCount, Span<StackPoint> points, Span<int> order, StackOrder stackOrder, StackOffset offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seriesCount);
        if (values.Length % seriesCount != 0)
        {
            throw new ArgumentException("The values must be whole rows of one value per series.", nameof(values));
        }

        if (points.Length < values.Length)
        {
            throw new ArgumentException("There must be a point per value.", nameof(points));
        }

        if (order.Length < seriesCount)
        {
            throw new ArgumentException("There must be an order entry per series.", nameof(order));
        }

        var m = values.Length / seriesCount;
        for (var i = 0; i < seriesCount; i++)
        {
            for (var j = 0; j < m; j++)
            {
                points[i * m + j] = new StackPoint(0, values[j * seriesCount + i]);
            }
        }

        var series = new Series(points[..values.Length], seriesCount, m);
        StackOrders.Apply(series, order[..seriesCount], stackOrder);
        StackOffsets.Apply(series, order[..seriesCount], offset);
    }
}

internal readonly ref struct Series(Span<StackPoint> points, int count, int length)
{
    private readonly Span<StackPoint> points = points;

    public int Count { get; } = count;

    public int Length { get; } = length;

    public ref StackPoint this[int series, int point] => ref points[series * Length + point];
}
