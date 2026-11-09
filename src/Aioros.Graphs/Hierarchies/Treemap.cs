using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Hierarchies;

/// <summary>How a treemap divides a node among its children: d3-hierarchy's tiling methods.</summary>
public enum TreemapTiling
{
    /// <summary>d3's <c>treemapSquarify</c>: rows of the given aspect ratio (the golden ratio by default).</summary>
    Squarify,

    /// <summary>d3's <c>treemapResquarify</c>: squarify, keeping the rows of a previous layout so that changes of value animate stably.</summary>
    Resquarify,

    /// <summary>d3's <c>treemapBinary</c>: balanced binary splits, horizontal for wide nodes and vertical for tall ones.</summary>
    Binary,

    /// <summary>d3's <c>treemapDice</c>: children side by side horizontally.</summary>
    Dice,

    /// <summary>d3's <c>treemapSlice</c>: children stacked vertically.</summary>
    Slice,

    /// <summary>d3's <c>treemapSliceDice</c>: slice at odd depths, dice at even ones.</summary>
    SliceDice,
}

/// <summary>The settings of a <see cref="Treemap"/>.</summary>
public sealed record TreemapOptions
{
    /// <summary>The golden ratio, d3's default aspect ratio for squarified rows.</summary>
    public static readonly double GoldenRatio = (1 + Math.Sqrt(5)) / 2;

    /// <summary>The layout's width.</summary>
    public double Width { get; init; } = 1;

    /// <summary>The layout's height.</summary>
    public double Height { get; init; } = 1;

    /// <summary>The tiling method.</summary>
    public TreemapTiling Tiling { get; init; }

    /// <summary>The target aspect ratio of squarified rows, as <c>treemapSquarify.ratio</c>; values below 1 count as 1.</summary>
    public double Ratio { get; init; } = GoldenRatio;

    /// <summary>The gap between siblings (<c>paddingInner</c>).</summary>
    public double PaddingInner { get; init; }

    /// <summary>The gap between a parent's top edge and its children (<c>paddingTop</c>).</summary>
    public double PaddingTop { get; init; }

    /// <summary>The gap between a parent's right edge and its children (<c>paddingRight</c>).</summary>
    public double PaddingRight { get; init; }

    /// <summary>The gap between a parent's bottom edge and its children (<c>paddingBottom</c>).</summary>
    public double PaddingBottom { get; init; }

    /// <summary>The gap between a parent's left edge and its children (<c>paddingLeft</c>).</summary>
    public double PaddingLeft { get; init; }

    /// <summary>Whether coordinates are rounded to integers (<c>round</c>).</summary>
    public bool Round { get; init; }
}

/// <summary>The rectangles of a hierarchy's nodes, by node index: a treemap or a partition.</summary>
public sealed class NodeRectangles
{
    internal NodeRectangles(double[] x0, double[] y0, double[] x1, double[] y1, SquarifyRow[]?[]? rows = null, double[]? ratios = null)
    {
        X0 = x0;
        Y0 = y0;
        X1 = x1;
        Y1 = y1;
        Rows = rows;
        Ratios = ratios;
    }

    /// <summary>Each node's left edge.</summary>
    public double[] X0 { get; }

    /// <summary>Each node's top edge.</summary>
    public double[] Y0 { get; }

    /// <summary>Each node's right edge.</summary>
    public double[] X1 { get; }

    /// <summary>Each node's bottom edge.</summary>
    public double[] Y1 { get; }

    internal SquarifyRow[]?[]? Rows { get; }

    internal double[]? Ratios { get; }

    internal static void RoundAll(NodeRectangles rectangles)
    {
        Round(rectangles.X0);
        Round(rectangles.Y0);
        Round(rectangles.X1);
        Round(rectangles.Y1);
    }

    private static void Round(double[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = JsNumber.Round(values[i]);
        }
    }
}

/// <summary>d3-hierarchy's <c>treemap</c>: nested rectangles whose areas follow the nodes' values.</summary>
public static class Treemap
{
    /// <summary>Lays out a summed <paramref name="hierarchy"/> as d3's <c>treemap()</c> with the given settings.</summary>
    /// <param name="hierarchy">The hierarchy, summed and in its current child order.</param>
    /// <param name="options">The size, tiling, padding and rounding.</param>
    /// <param name="previous">
    /// The previous layout of the same hierarchy, whose squarified rows <see cref="TreemapTiling.Resquarify"/> keeps,
    /// as d3's nodes keep them between two calls.
    /// </param>
    /// <returns>Each node's rectangle.</returns>
    /// <exception cref="ArgumentException">The previous layout is of a hierarchy with another node count.</exception>
    public static NodeRectangles Compute(Hierarchy hierarchy, TreemapOptions options, NodeRectangles? previous = null)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(options);
        var n = hierarchy.Count;
        if (previous is not null && previous.X0.Length != n)
        {
            throw new ArgumentException("The previous layout must be of the same hierarchy.", nameof(previous));
        }

        var resquarify = options.Tiling == TreemapTiling.Resquarify;
        var rows = resquarify || previous?.Rows is not null ? (SquarifyRow[]?[]?)(previous?.Rows?.Clone() ?? new SquarifyRow[]?[n]) : null;
        var ratios = rows is null ? null : (double[])(previous?.Ratios?.Clone() ?? new double[n]);
        var rectangles = new NodeRectangles(new double[n], new double[n], new double[n], new double[n], rows, ratios);
        var tiles = new Tiles(hierarchy.Values, rectangles.X0, rectangles.Y0, rectangles.X1, rectangles.Y1);
        var layout = new Layout(hierarchy, options, rectangles);
        rectangles.X1[hierarchy.Root] = options.Width;
        rectangles.Y1[hierarchy.Root] = options.Height;
        using var pre = new NodeOrder(hierarchy, NodeOrderKind.Pre);
        foreach (var node in pre.Nodes)
        {
            layout.Position(tiles, node);
        }

        if (options.Round)
        {
            NodeRectangles.RoundAll(rectangles);
        }

        return rectangles;
    }

    private readonly struct Layout(Hierarchy hierarchy, TreemapOptions options, NodeRectangles rectangles)
    {
        private readonly double[] paddingStack = new double[MaxDepth(hierarchy) + 2];
        private readonly double ratio = options.Ratio > 1 ? options.Ratio : 1;
        private readonly SquarifyRow[]?[] rows = rectangles.Rows ?? [];
        private readonly double[] ratios = rectangles.Ratios ?? [];

        public void Position(Tiles tiles, int node)
        {
            var depth = hierarchy.Depths[node];
            var p = paddingStack[depth];
            var x0 = rectangles.X0[node] + p;
            var y0 = rectangles.Y0[node] + p;
            var x1 = rectangles.X1[node] - p;
            var y1 = rectangles.Y1[node] - p;
            Collapse(ref x0, ref x1);
            Collapse(ref y0, ref y1);
            rectangles.X0[node] = x0;
            rectangles.Y0[node] = y0;
            rectangles.X1[node] = x1;
            rectangles.Y1[node] = y1;
            var children = hierarchy.Children(node);
            if (children.Length == 0)
            {
                return;
            }

            p = paddingStack[depth + 1] = options.PaddingInner / 2;
            x0 += options.PaddingLeft - p;
            y0 += options.PaddingTop - p;
            x1 -= options.PaddingRight - p;
            y1 -= options.PaddingBottom - p;
            Collapse(ref x0, ref x1);
            Collapse(ref y0, ref y1);
            Tile(tiles, node, children, x0, y0, x1, y1);
        }

        private void Tile(Tiles tiles, int node, ReadOnlySpan<int> children, double x0, double y0, double x1, double y1)
        {
            var value = hierarchy.Values[node];
            switch (options.Tiling)
            {
                case TreemapTiling.Dice:
                    tiles.Dice(children, value, x0, y0, x1, y1);
                    break;
                case TreemapTiling.Slice:
                    tiles.Slice(children, value, x0, y0, x1, y1);
                    break;
                case TreemapTiling.SliceDice when (hierarchy.Depths[node] & 1) != 0:
                    tiles.Slice(children, value, x0, y0, x1, y1);
                    break;
                case TreemapTiling.SliceDice:
                    tiles.Dice(children, value, x0, y0, x1, y1);
                    break;
                case TreemapTiling.Binary:
                    tiles.Binary(children, value, x0, y0, x1, y1);
                    break;
                case TreemapTiling.Resquarify when rows[node] is { } kept && ratios[node].Equals(ratio):
                    tiles.Resquarify(kept, value, x0, y0, x1, y1);
                    break;
                case TreemapTiling.Resquarify:
                    var list = new List<SquarifyRow>();
                    tiles.Squarify(children, value, ratio, x0, y0, x1, y1, list);
                    rows[node] = [.. list];
                    ratios[node] = ratio;
                    break;
                default:
                    tiles.Squarify(children, value, ratio, x0, y0, x1, y1, null);
                    break;
            }
        }

        private static void Collapse(ref double low, ref double high)
        {
            if (high < low)
            {
                low = high = (low + high) / 2;
            }
        }

        private static int MaxDepth(Hierarchy hierarchy)
        {
            var max = 0;
            foreach (var depth in hierarchy.Depths)
            {
                max = Math.Max(max, depth);
            }

            return max;
        }
    }
}
