namespace Aioros.Graphs.Hierarchies;

/// <summary>d3-hierarchy's <c>cluster</c>: a dendrogram, with every leaf at the same depth.</summary>
public static class Cluster
{
    /// <summary>Lays out <paramref name="hierarchy"/> as d3's <c>cluster()</c> with the given settings.</summary>
    /// <param name="hierarchy">The hierarchy, in its current child order.</param>
    /// <param name="options">The size and separation.</param>
    /// <returns>Each node's x (breadth) and y (depth).</returns>
    public static NodePoints Compute(Hierarchy hierarchy, NodeLayoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(options);
        var x = new double[hierarchy.Count];
        var y = new double[hierarchy.Count];
        using var post = new NodeOrder(hierarchy, NodeOrderKind.Post);
        FirstWalk(hierarchy, options.Separation, post.Nodes, x, y);
        var root = hierarchy.Root;
        var left = Leaf(hierarchy, root, first: true);
        var right = Leaf(hierarchy, root, first: false);
        var x0 = x[left] - Separations.Of(hierarchy, options.Separation, left, right) / 2;
        var x1 = x[right] + Separations.Of(hierarchy, options.Separation, right, left) / 2;
        foreach (var node in post.Nodes)
        {
            if (options.NodeSize)
            {
                x[node] = (x[node] - x[root]) * options.Width;
                y[node] = (y[root] - y[node]) * options.Height;
            }
            else
            {
                x[node] = (x[node] - x0) / (x1 - x0) * options.Width;
                y[node] = (1 - (y[root] != 0 && !double.IsNaN(y[root]) ? y[node] / y[root] : 1)) * options.Height;
            }
        }

        return new NodePoints(x, y);
    }

    private static void FirstWalk(Hierarchy hierarchy, NodeSeparation separation, ReadOnlySpan<int> order, double[] x, double[] y)
    {
        var previous = -1;
        var position = 0.0;
        foreach (var node in order)
        {
            var children = hierarchy.Children(node);
            if (children.Length > 0)
            {
                var sum = 0.0;
                var max = 0.0;
                foreach (var child in children)
                {
                    sum += x[child];
                    max = Math.Max(max, y[child]);
                }

                x[node] = sum / children.Length;
                y[node] = 1 + max;
                continue;
            }

            if (previous >= 0)
            {
                position += Separations.Of(hierarchy, separation, node, previous);
                x[node] = position;
            }
            else
            {
                x[node] = 0;
            }

            y[node] = 0;
            previous = node;
        }
    }

    private static int Leaf(Hierarchy hierarchy, int node, bool first)
    {
        while (hierarchy.Children(node) is { Length: > 0 } children)
        {
            node = first ? children[0] : children[^1];
        }

        return node;
    }
}
