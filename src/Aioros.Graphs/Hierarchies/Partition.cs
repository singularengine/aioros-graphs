namespace Aioros.Graphs.Hierarchies;

/// <summary>The settings of a <see cref="Partition"/>.</summary>
public sealed record PartitionOptions
{
    /// <summary>The layout's width: the breadth of the root, or a full turn (2π) for a sunburst.</summary>
    public double Width { get; init; } = 1;

    /// <summary>The layout's height: the depth of all levels, or the radius for a sunburst.</summary>
    public double Height { get; init; } = 1;

    /// <summary>The gap subtracted from each node's right and bottom edges.</summary>
    public double Padding { get; init; }

    /// <summary>Whether coordinates are rounded to integers.</summary>
    public bool Round { get; init; }
}

/// <summary>d3-hierarchy's <c>partition</c>: an icicle, or a sunburst in polar coordinates.</summary>
public static class Partition
{
    /// <summary>Lays out a summed <paramref name="hierarchy"/> as d3's <c>partition()</c> with the given settings.</summary>
    /// <param name="hierarchy">The hierarchy, summed and in its current child order.</param>
    /// <param name="options">The size, padding and rounding.</param>
    /// <returns>Each node's rectangle: x along the breadth, y along the depth.</returns>
    public static NodeRectangles Compute(Hierarchy hierarchy, PartitionOptions options)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(options);
        var count = hierarchy.Count;
        var rectangles = new NodeRectangles(new double[count], new double[count], new double[count], new double[count]);
        var (x0s, y0s, x1s, y1s) = (rectangles.X0, rectangles.Y0, rectangles.X1, rectangles.Y1);
        var tiles = new Tiles(hierarchy.Values, x0s, y0s, x1s, y1s);
        var root = hierarchy.Root;
        var n = hierarchy.Heights[root] + 1;
        var dy = options.Height;
        var padding = options.Padding;
        x0s[root] = padding;
        y0s[root] = padding;
        x1s[root] = options.Width;
        y1s[root] = dy / n;
        using var pre = new NodeOrder(hierarchy, NodeOrderKind.Pre);
        foreach (var node in pre.Nodes)
        {
            var depth = hierarchy.Depths[node];
            var children = hierarchy.Children(node);
            if (children.Length > 0)
            {
                tiles.Dice(children, hierarchy.Values[node], x0s[node], dy * (depth + 1) / n, x1s[node], dy * (depth + 2) / n);
            }

            var x0 = x0s[node];
            var y0 = y0s[node];
            var x1 = x1s[node] - padding;
            var y1 = y1s[node] - padding;
            if (x1 < x0)
            {
                x0 = x1 = (x0 + x1) / 2;
            }

            if (y1 < y0)
            {
                y0 = y1 = (y0 + y1) / 2;
            }

            x0s[node] = x0;
            y0s[node] = y0;
            x1s[node] = x1;
            y1s[node] = y1;
        }

        if (options.Round)
        {
            NodeRectangles.RoundAll(rectangles);
        }

        return rectangles;
    }
}
