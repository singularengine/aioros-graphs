using Aioros.Graphs.Paths;
using Aioros.Graphs.Shapes;

namespace Aioros.Graphs.Sankey;

/// <summary>
/// The sankey layout of d3-sankey and its result, the graph d3-sankey returns: per node its rectangle, value, depth,
/// height and column, and its outgoing and incoming links in drawing order; per link its ends and its width.
/// </summary>
public sealed class SankeyLayout
{
    /// <summary>Lays out a graph whose links name their ends by node index, as d3's <c>sankey()</c> with its default <c>nodeId</c>.</summary>
    /// <param name="nodeCount">The number of nodes.</param>
    /// <param name="sources">Each link's source node.</param>
    /// <param name="targets">Each link's target node.</param>
    /// <param name="values">Each link's value.</param>
    /// <param name="options">The settings.</param>
    /// <param name="fixedValues">Empty, or each node's <c>fixedValue</c>, NaN where it has none.</param>
    /// <returns>The layout.</returns>
    /// <exception cref="ArgumentException">A link names no node (<c>missing: id</c>), the graph has a cycle (<c>circular link</c>), there are no nodes, or the spans' lengths disagree.</exception>
    public static SankeyLayout Compute(int nodeCount, ReadOnlySpan<int> sources, ReadOnlySpan<int> targets, ReadOnlySpan<double> values, SankeyOptions options, ReadOnlySpan<double> fixedValues = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        Validate(nodeCount, sources.Length, targets.Length, values.Length, fixedValues.Length);
        var s = new int[sources.Length];
        var t = new int[sources.Length];
        for (var i = 0; i < s.Length; i++)
        {
            s[i] = Find(sources[i], nodeCount);
            t[i] = Find(targets[i], nodeCount);
        }

        return SankeySolver.Run(new SankeyLayout(nodeCount, s, t, values.ToArray()), options, fixedValues);
    }

    /// <summary>Lays out a graph whose nodes have string ids and whose links name their ends by id, as d3's <c>sankey().nodeId(d => d.id)</c>.</summary>
    /// <param name="ids">Each node's id; a repeated id names its last node, as d3's map does.</param>
    /// <param name="sources">Each link's source id.</param>
    /// <param name="targets">Each link's target id.</param>
    /// <param name="values">Each link's value.</param>
    /// <param name="options">The settings.</param>
    /// <param name="fixedValues">Empty, or each node's <c>fixedValue</c>, NaN where it has none.</param>
    /// <returns>The layout.</returns>
    /// <exception cref="ArgumentException">A link names no node (<c>missing: id</c>), the graph has a cycle (<c>circular link</c>), there are no nodes, or the spans' lengths disagree.</exception>
    public static SankeyLayout Compute(ReadOnlySpan<string> ids, ReadOnlySpan<string> sources, ReadOnlySpan<string> targets, ReadOnlySpan<double> values, SankeyOptions options, ReadOnlySpan<double> fixedValues = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        Validate(ids.Length, sources.Length, targets.Length, values.Length, fixedValues.Length);
        var index = new Dictionary<string, int>(ids.Length, StringComparer.Ordinal);
        for (var i = 0; i < ids.Length; i++)
        {
            index[ids[i]] = i;
        }

        var s = new int[sources.Length];
        var t = new int[sources.Length];
        for (var i = 0; i < s.Length; i++)
        {
            s[i] = Find(index, sources[i]);
            t[i] = Find(index, targets[i]);
        }

        return SankeySolver.Run(new SankeyLayout(ids.Length, s, t, values.ToArray()), options, fixedValues);
    }

    private static void Validate(int nodes, int sources, int targets, int values, int fixedValues)
    {
        if (nodes < 1)
        {
            throw new ArgumentException("A sankey needs at least one node.", nameof(nodes));
        }

        if (targets != sources || values != sources)
        {
            throw new ArgumentException("There must be one source, target and value per link.", nameof(values));
        }

        if (fixedValues != 0 && fixedValues != nodes)
        {
            throw new ArgumentException("There must be no fixed values or one per node.", nameof(fixedValues));
        }
    }

    private static int Find(int id, int nodes) =>
        id >= 0 && id < nodes ? id : throw new ArgumentException("missing: " + id.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static int Find(Dictionary<string, int> index, string id) =>
        index.TryGetValue(id, out var node) ? node : throw new ArgumentException("missing: " + id);

    internal SankeyLayout(int nodes, int[] sources, int[] targets, double[] values)
    {
        NodeCount = nodes;
        LinkSource = sources;
        LinkTarget = targets;
        LinkValueArray = values;
        NodeX0 = new double[nodes];
        NodeX1 = new double[nodes];
        NodeY0 = new double[nodes];
        NodeY1 = new double[nodes];
        NodeValue = new double[nodes];
        NodeDepth = new int[nodes];
        NodeHeight = new int[nodes];
        NodeLayer = new int[nodes];
        NodeY0.AsSpan().Fill(double.NaN);
        NodeY1.AsSpan().Fill(double.NaN);
        NodeValue.AsSpan().Fill(double.NaN);
        LinkY0Array = new double[values.Length];
        LinkY1Array = new double[values.Length];
        LinkWidthArray = new double[values.Length];
        SourceStart = new int[nodes + 1];
        TargetStart = new int[nodes + 1];
        SourceList = new int[values.Length];
        TargetList = new int[values.Length];
    }

    /// <summary>The number of nodes.</summary>
    public int NodeCount { get; }

    /// <summary>The number of links.</summary>
    public int LinkCount => LinkValueArray.Length;

    /// <summary>Each node's left edge.</summary>
    public ReadOnlySpan<double> X0 => NodeX0;

    /// <summary>Each node's right edge.</summary>
    public ReadOnlySpan<double> X1 => NodeX1;

    /// <summary>Each node's top edge.</summary>
    public ReadOnlySpan<double> Y0 => NodeY0;

    /// <summary>Each node's bottom edge.</summary>
    public ReadOnlySpan<double> Y1 => NodeY1;

    /// <summary>Each node's value: its fixed value, or the larger of its outgoing and incoming totals.</summary>
    public ReadOnlySpan<double> Value => NodeValue;

    /// <summary>Each node's depth: the longest path from a source.</summary>
    public ReadOnlySpan<int> Depth => NodeDepth;

    /// <summary>Each node's height: the longest path to a sink.</summary>
    public ReadOnlySpan<int> Height => NodeHeight;

    /// <summary>Each node's column.</summary>
    public ReadOnlySpan<int> Layer => NodeLayer;

    /// <summary>Each link's source node.</summary>
    public ReadOnlySpan<int> Source => LinkSource;

    /// <summary>Each link's target node.</summary>
    public ReadOnlySpan<int> Target => LinkTarget;

    /// <summary>Each link's value.</summary>
    public ReadOnlySpan<double> LinkValue => LinkValueArray;

    /// <summary>Each link's vertical center at its source.</summary>
    public ReadOnlySpan<double> LinkY0 => LinkY0Array;

    /// <summary>Each link's vertical center at its target.</summary>
    public ReadOnlySpan<double> LinkY1 => LinkY1Array;

    /// <summary>Each link's width.</summary>
    public ReadOnlySpan<double> LinkWidth => LinkWidthArray;

    internal int[] LinkSource { get; }

    internal int[] LinkTarget { get; }

    internal double[] LinkValueArray { get; }

    internal double[] NodeX0 { get; }

    internal double[] NodeX1 { get; }

    internal double[] NodeY0 { get; }

    internal double[] NodeY1 { get; }

    internal double[] NodeValue { get; }

    internal int[] NodeDepth { get; }

    internal int[] NodeHeight { get; }

    internal int[] NodeLayer { get; }

    internal double[] LinkY0Array { get; }

    internal double[] LinkY1Array { get; }

    internal double[] LinkWidthArray { get; }

    internal int[] SourceStart { get; }

    internal int[] TargetStart { get; }

    internal int[] SourceList { get; }

    internal int[] TargetList { get; }

    /// <summary>Returns a node's outgoing links, in d3's <c>sourceLinks</c> order.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The link indices.</returns>
    public ReadOnlySpan<int> SourceLinks(int node) => SourceList.AsSpan(SourceStart[node], SourceStart[node + 1] - SourceStart[node]);

    /// <summary>Returns a node's incoming links, in d3's <c>targetLinks</c> order.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The link indices.</returns>
    public ReadOnlySpan<int> TargetLinks(int node) => TargetList.AsSpan(TargetStart[node], TargetStart[node + 1] - TargetStart[node]);

    /// <summary>Writes a link into a new path, as d3-sankey's <c>sankeyLinkHorizontal()</c>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="link">The link.</param>
    public void WriteLink<TSink>(ref TSink sink, int link)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        WriteLink(ref context, link);
    }

    /// <summary>Writes a link into <paramref name="context"/>, as d3-sankey's <c>sankeyLinkHorizontal()</c> on a context.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="link">The link.</param>
    public void WriteLink<TSink>(ref PathContext<TSink> context, int link)
        where TSink : IPathSink =>
        Link.Horizontal(ref context, NodeX1[LinkSource[link]], LinkY0Array[link], NodeX0[LinkTarget[link]], LinkY1Array[link]);
}
