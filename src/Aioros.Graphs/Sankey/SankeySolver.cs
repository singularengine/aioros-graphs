namespace Aioros.Graphs.Sankey;

internal sealed class SankeySolver
{
    private readonly SankeyLayout layout;
    private readonly SankeyOptions options;
    private readonly int[] source;
    private readonly int[] target;
    private readonly double[] linkValue;
    private readonly double[] width;
    private readonly double[] x0;
    private readonly double[] x1;
    private readonly double[] y0;
    private readonly double[] y1;
    private readonly int[] sourceStart;
    private readonly int[] targetStart;
    private readonly int[] sourceList;
    private readonly int[] targetList;
    private readonly bool reorderLinks;
    private readonly bool reorderNodes;
    private int[] columnStart = [];
    private int[] columnNodes = [];
    private double py;

    private SankeySolver(SankeyLayout layout, SankeyOptions options)
    {
        this.layout = layout;
        this.options = options;
        source = layout.LinkSource;
        target = layout.LinkTarget;
        linkValue = layout.LinkValueArray;
        width = layout.LinkWidthArray;
        x0 = layout.NodeX0;
        x1 = layout.NodeX1;
        y0 = layout.NodeY0;
        y1 = layout.NodeY1;
        sourceStart = layout.SourceStart;
        targetStart = layout.TargetStart;
        sourceList = layout.SourceList;
        targetList = layout.TargetList;
        reorderLinks = options.LinkComparison is null && options.LinkOrder == SankeyOrder.Breadth;
        reorderNodes = options.NodeComparison is null && options.NodeOrder == SankeyOrder.Breadth;
    }

    private int ColumnCount => columnStart.Length - 1;

    public static SankeyLayout Run(SankeyLayout layout, SankeyOptions options, ReadOnlySpan<double> fixedValues)
    {
        var solver = new SankeySolver(layout, options);
        solver.ComputeNodeLinks();
        solver.ComputeNodeValues(fixedValues);
        solver.ComputeDepths(layout.NodeDepth, solver.sourceStart, solver.sourceList, solver.target);
        solver.ComputeDepths(layout.NodeHeight, solver.targetStart, solver.targetList, solver.source);
        solver.ComputeNodeBreadths();
        solver.ComputeLinkBreadths();
        return layout;
    }

    private static double Sum(ReadOnlySpan<int> links, double[] values)
    {
        var sum = 0.0;
        foreach (var link in links)
        {
            var value = values[link];
            if (value != 0 && !double.IsNaN(value))
            {
                sum += value;
            }
        }

        return sum;
    }

    private static void Group(int[] ends, int[] start, int[] list)
    {
        foreach (var end in ends)
        {
            start[end + 1]++;
        }

        for (var i = 1; i < start.Length; i++)
        {
            start[i] += start[i - 1];
        }

        var next = start[..^1];
        for (var link = 0; link < ends.Length; link++)
        {
            list[next[ends[link]]++] = link;
        }
    }

    private Span<int> Sources(int node) => sourceList.AsSpan(sourceStart[node], sourceStart[node + 1] - sourceStart[node]);

    private Span<int> Targets(int node) => targetList.AsSpan(targetStart[node], targetStart[node + 1] - targetStart[node]);

    private Span<int> Column(int column) => columnNodes.AsSpan(columnStart[column], columnStart[column + 1] - columnStart[column]);

    private void ComputeNodeLinks()
    {
        Group(source, sourceStart, sourceList);
        Group(target, targetStart, targetList);
        if (options.LinkComparison is { } comparison)
        {
            var comparer = new CustomComparer(comparison, layout);
            for (var node = 0; node < layout.NodeCount; node++)
            {
                SankeySort.Sort(Sources(node), comparer);
                SankeySort.Sort(Targets(node), comparer);
            }
        }
    }

    private void ComputeNodeValues(ReadOnlySpan<double> fixedValues)
    {
        var values = layout.NodeValue;
        for (var node = 0; node < values.Length; node++)
        {
            values[node] = fixedValues.Length != 0 && !double.IsNaN(fixedValues[node])
                ? fixedValues[node]
                : Math.Max(Sum(Sources(node), linkValue), Sum(Targets(node), linkValue));
        }
    }

    private void ComputeDepths(int[] depth, int[] start, int[] list, int[] ends)
    {
        var n = layout.NodeCount;
        var current = new int[n];
        var next = new int[n];
        var stamp = new int[n];
        for (var i = 0; i < n; i++)
        {
            current[i] = i;
        }

        var count = n;
        var x = 0;
        while (count > 0)
        {
            var added = 0;
            foreach (var node in current.AsSpan(0, count))
            {
                depth[node] = x;
                for (var k = start[node]; k < start[node + 1]; k++)
                {
                    added = Add(ends[list[k]], x + 1, stamp, next, added);
                }
            }

            if (++x > n)
            {
                throw new ArgumentException("circular link");
            }

            (current, next) = (next, current);
            count = added;
        }
    }

    private static int Add(int node, int round, int[] stamp, int[] next, int added)
    {
        if (stamp[node] == round)
        {
            return added;
        }

        stamp[node] = round;
        next[added] = node;
        return added + 1;
    }

    private double Align(int node, int columns) => options.Align switch
    {
        SankeyAlign.Left => layout.NodeDepth[node],
        SankeyAlign.Right => columns - 1 - layout.NodeHeight[node],
        SankeyAlign.Center => Center(node),
        _ => Sources(node).Length != 0 ? layout.NodeDepth[node] : columns - 1,
    };

    private double Center(int node)
    {
        if (Targets(node).Length != 0)
        {
            return layout.NodeDepth[node];
        }

        if (Sources(node).Length == 0)
        {
            return 0;
        }

        var min = double.NaN;
        foreach (var link in Sources(node))
        {
            double depth = layout.NodeDepth[target[link]];
            if (double.IsNaN(min) || min > depth)
            {
                min = depth;
            }
        }

        return min - 1;
    }

    private void ComputeNodeLayers()
    {
        var n = layout.NodeCount;
        var maxDepth = 0;
        foreach (var depth in layout.NodeDepth)
        {
            maxDepth = Math.Max(maxDepth, depth);
        }

        var columns = maxDepth + 1;
        var kx = (options.X1 - options.X0 - options.NodeWidth) / (columns - 1);
        columnStart = new int[columns + 1];
        columnNodes = new int[n];
        for (var node = 0; node < n; node++)
        {
            var i = (int)Math.Max(0, Math.Min(columns - 1, Math.Floor(Align(node, columns))));
            layout.NodeLayer[node] = i;
            x0[node] = options.X0 + i * kx;
            x1[node] = x0[node] + options.NodeWidth;
        }

        Group(layout.NodeLayer, columnStart, columnNodes);
        if (options.NodeComparison is { } comparison)
        {
            var comparer = new CustomComparer(comparison, layout);
            for (var c = 0; c < columns; c++)
            {
                SankeySort.Sort(Column(c), comparer);
            }
        }
    }

    private void ComputeNodeBreadths()
    {
        ComputeNodeLayers();
        var tallest = 0;
        for (var c = 0; c < ColumnCount; c++)
        {
            tallest = Math.Max(tallest, Column(c).Length);
        }

        py = Math.Min(options.NodePadding, (options.Y1 - options.Y0) / (tallest - 1));
        InitializeNodeBreadths();
        for (var i = 0; i < options.Iterations; ++i)
        {
            var alpha = Math.Pow(0.99, i);
            var beta = Math.Max(1 - alpha, (i + 1) / (double)options.Iterations);
            RelaxRightToLeft(alpha, beta);
            RelaxLeftToRight(alpha, beta);
        }
    }

    private double BreadthScale()
    {
        var ky = double.NaN;
        for (var c = 0; c < ColumnCount; c++)
        {
            var column = Column(c);
            var value = (options.Y1 - options.Y0 - (column.Length - 1) * py) / Sum(column, layout.NodeValue);
            if (!double.IsNaN(value) && (double.IsNaN(ky) || ky > value))
            {
                ky = value;
            }
        }

        return ky;
    }

    private void InitializeNodeBreadths()
    {
        var ky = BreadthScale();
        for (var c = 0; c < ColumnCount; c++)
        {
            var nodes = Column(c);
            var y = options.Y0;
            foreach (var node in nodes)
            {
                y0[node] = y;
                y1[node] = y + layout.NodeValue[node] * ky;
                y = y1[node] + py;
                foreach (var link in Sources(node))
                {
                    width[link] = linkValue[link] * ky;
                }
            }

            y = (options.Y1 - y + py) / (nodes.Length + 1);
            for (var i = 0; i < nodes.Length; ++i)
            {
                var node = nodes[i];
                y0[node] += y * (i + 1);
                y1[node] += y * (i + 1);
            }

            ReorderLinks(nodes);
        }
    }

    private void RelaxLeftToRight(double alpha, double beta)
    {
        for (var c = 1; c < ColumnCount; ++c)
        {
            var column = Column(c);
            foreach (var node in column)
            {
                var y = 0.0;
                var w = 0.0;
                foreach (var link in Targets(node))
                {
                    var v = linkValue[link] * (layout.NodeLayer[node] - layout.NodeLayer[source[link]]);
                    y += TargetTop(source[link], node) * v;
                    w += v;
                }

                Move(node, y, w, alpha);
            }

            Settle(column, beta);
        }
    }

    private void RelaxRightToLeft(double alpha, double beta)
    {
        for (var c = ColumnCount - 2; c >= 0; --c)
        {
            var column = Column(c);
            foreach (var node in column)
            {
                var y = 0.0;
                var w = 0.0;
                foreach (var link in Sources(node))
                {
                    var v = linkValue[link] * (layout.NodeLayer[target[link]] - layout.NodeLayer[node]);
                    y += SourceTop(node, target[link]) * v;
                    w += v;
                }

                Move(node, y, w, alpha);
            }

            Settle(column, beta);
        }
    }

    private void Move(int node, double y, double w, double alpha)
    {
        if (!(w > 0))
        {
            return;
        }

        var dy = (y / w - y0[node]) * alpha;
        y0[node] += dy;
        y1[node] += dy;
        ReorderNodeLinks(node);
    }

    private void Settle(Span<int> column, double beta)
    {
        if (reorderNodes)
        {
            SankeySort.Sort(column, new BreadthComparer(y0));
        }

        ResolveCollisions(column, beta);
    }

    private void ResolveCollisions(Span<int> nodes, double alpha)
    {
        if (nodes.IsEmpty)
        {
            return;
        }

        var i = nodes.Length >> 1;
        var subject = nodes[i];
        BottomToTop(nodes, y0[subject] - py, i - 1, alpha);
        TopToBottom(nodes, y1[subject] + py, i + 1, alpha);
        BottomToTop(nodes, options.Y1, nodes.Length - 1, alpha);
        TopToBottom(nodes, options.Y0, 0, alpha);
    }

    private void TopToBottom(Span<int> nodes, double y, int i, double alpha)
    {
        for (; i < nodes.Length; ++i)
        {
            var node = nodes[i];
            var dy = (y - y0[node]) * alpha;
            if (dy > 1e-6)
            {
                y0[node] += dy;
                y1[node] += dy;
            }

            y = y1[node] + py;
        }
    }

    private void BottomToTop(Span<int> nodes, double y, int i, double alpha)
    {
        for (; i >= 0; --i)
        {
            var node = nodes[i];
            var dy = (y1[node] - y) * alpha;
            if (dy > 1e-6)
            {
                y0[node] -= dy;
                y1[node] -= dy;
            }

            y = y0[node] - py;
        }
    }

    private void ReorderNodeLinks(int node)
    {
        if (!reorderLinks)
        {
            return;
        }

        foreach (var link in Targets(node))
        {
            SankeySort.Sort(Sources(source[link]), new LinkBreadthComparer(y0, target));
        }

        foreach (var link in Sources(node))
        {
            SankeySort.Sort(Targets(target[link]), new LinkBreadthComparer(y0, source));
        }
    }

    private void ReorderLinks(Span<int> nodes)
    {
        if (!reorderLinks)
        {
            return;
        }

        foreach (var node in nodes)
        {
            SankeySort.Sort(Sources(node), new LinkBreadthComparer(y0, target));
            SankeySort.Sort(Targets(node), new LinkBreadthComparer(y0, source));
        }
    }

    private double TargetTop(int from, int to)
    {
        var y = y0[from] - (Sources(from).Length - 1) * py / 2;
        foreach (var link in Sources(from))
        {
            if (target[link] == to)
            {
                break;
            }

            y += width[link] + py;
        }

        foreach (var link in Targets(to))
        {
            if (source[link] == from)
            {
                break;
            }

            y -= width[link];
        }

        return y;
    }

    private double SourceTop(int from, int to)
    {
        var y = y0[to] - (Targets(to).Length - 1) * py / 2;
        foreach (var link in Targets(to))
        {
            if (source[link] == from)
            {
                break;
            }

            y += width[link] + py;
        }

        foreach (var link in Sources(from))
        {
            if (target[link] == to)
            {
                break;
            }

            y -= width[link];
        }

        return y;
    }

    private void ComputeLinkBreadths()
    {
        for (var node = 0; node < layout.NodeCount; node++)
        {
            var top = y0[node];
            var bottom = top;
            foreach (var link in Sources(node))
            {
                layout.LinkY0Array[link] = top + width[link] / 2;
                top += width[link];
            }

            foreach (var link in Targets(node))
            {
                layout.LinkY1Array[link] = bottom + width[link] / 2;
                bottom += width[link];
            }
        }
    }
}
