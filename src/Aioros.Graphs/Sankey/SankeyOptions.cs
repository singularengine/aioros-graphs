namespace Aioros.Graphs.Sankey;

/// <summary>How a sankey assigns each node to a column, as d3-sankey's node alignments.</summary>
public enum SankeyAlign
{
    /// <summary>Sources by depth and sinks in the last column, d3's default <c>sankeyJustify</c>.</summary>
    Justify,

    /// <summary>By depth, d3's <c>sankeyLeft</c>.</summary>
    Left,

    /// <summary>By height from the last column, d3's <c>sankeyRight</c>.</summary>
    Right,

    /// <summary>By depth, sources just before their nearest target, d3's <c>sankeyCenter</c>.</summary>
    Center,
}

/// <summary>Whether a sankey keeps reordering nodes or links by their vertical position.</summary>
public enum SankeyOrder
{
    /// <summary>Reorder by vertical position as the layout relaxes, d3's default (an undefined sort).</summary>
    Breadth,

    /// <summary>Keep the input order, as d3's <c>nodeSort(null)</c> or <c>linkSort(null)</c>.</summary>
    Input,
}

/// <summary>
/// A comparison of two nodes or two links of a sankey being laid out, as the functions d3's <c>nodeSort</c> and
/// <c>linkSort</c> take: negative when <paramref name="a"/> comes first. It reads what the layout has computed so far
/// (link values and ends; for nodes, their values, depths, heights and layers).
/// </summary>
/// <param name="layout">The layout being computed.</param>
/// <param name="a">The first node or link index.</param>
/// <param name="b">The second node or link index.</param>
/// <returns>Negative, zero or positive, as a JavaScript comparator.</returns>
public delegate double SankeyComparison(SankeyLayout layout, int a, int b);

/// <summary>The settings of a sankey layout, with d3-sankey's defaults.</summary>
public sealed record SankeyOptions
{
    /// <summary>The left edge of the extent.</summary>
    public double X0 { get; init; }

    /// <summary>The top edge of the extent.</summary>
    public double Y0 { get; init; }

    /// <summary>The right edge of the extent.</summary>
    public double X1 { get; init; } = 1;

    /// <summary>The bottom edge of the extent.</summary>
    public double Y1 { get; init; } = 1;

    /// <summary>The width of each node.</summary>
    public double NodeWidth { get; init; } = 24;

    /// <summary>The vertical gap between nodes of a column, reduced when the tallest column needs it.</summary>
    public double NodePadding { get; init; } = 8;

    /// <summary>How nodes are assigned to columns.</summary>
    public SankeyAlign Align { get; init; }

    /// <summary>The number of relaxation passes.</summary>
    public int Iterations { get; init; } = 6;

    /// <summary>Whether columns are reordered by position; ignored when <see cref="NodeComparison"/> is set.</summary>
    public SankeyOrder NodeOrder { get; init; }

    /// <summary>When set, sorts each column once with it and keeps that order, as a function given to d3's <c>nodeSort</c>.</summary>
    public SankeyComparison? NodeComparison { get; init; }

    /// <summary>Whether links are reordered by position; ignored when <see cref="LinkComparison"/> is set.</summary>
    public SankeyOrder LinkOrder { get; init; }

    /// <summary>When set, sorts each node's links once with it and keeps that order, as a function given to d3's <c>linkSort</c>.</summary>
    public SankeyComparison? LinkComparison { get; init; }
}
