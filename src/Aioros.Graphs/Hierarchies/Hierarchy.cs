using System.Buffers;

namespace Aioros.Graphs.Hierarchies;

/// <summary>A link of a hierarchy, from a parent to one of its children, as d3's <c>node.links()</c> returns it.</summary>
/// <param name="Source">The parent node.</param>
/// <param name="Target">The child node.</param>
public readonly record struct HierarchyLink(int Source, int Target);

/// <summary>
/// A rooted tree, as d3-hierarchy's <c>hierarchy</c> and <c>stratify</c> build it: each node is an index, with its
/// parent, depth, height, children and value. A hierarchy is immutable: <see cref="Sum"/>, <see cref="CountLeaves"/>
/// and <see cref="Sort"/> return a new hierarchy that shares the unchanged parts. Children come in the order of their
/// node indices until sorted.
/// </summary>
public sealed class Hierarchy
{
    private readonly int[] parents;
    private readonly int[] depths;
    private readonly int[] heights;
    private readonly int[] childStart;
    private readonly int[] childList;
    private readonly double[] values;
    private readonly string?[]? ids;
    private readonly int rows;

    internal Hierarchy(int root, int[] parents, int[] depths, int[] heights, int[] childStart, int[] childList, double[] values, string?[]? ids, int rows)
    {
        Root = root;
        this.parents = parents;
        this.depths = depths;
        this.heights = heights;
        this.childStart = childStart;
        this.childList = childList;
        this.values = values;
        this.ids = ids;
        this.rows = rows;
    }

    /// <summary>The number of nodes.</summary>
    public int Count => parents.Length;

    /// <summary>The root node.</summary>
    public int Root { get; }

    /// <summary>The parent of each node; -1 for the root.</summary>
    public ReadOnlySpan<int> Parents => parents;

    /// <summary>The depth of each node: 0 for the root.</summary>
    public ReadOnlySpan<int> Depths => depths;

    /// <summary>The height of each node: 0 for a leaf, the greatest distance to a descendant leaf otherwise.</summary>
    public ReadOnlySpan<int> Heights => heights;

    /// <summary>The value of each node, NaN (d3's undefined) until <see cref="Sum"/> or <see cref="CountLeaves"/>.</summary>
    public ReadOnlySpan<double> Values => values;

    /// <summary>Builds the hierarchy of nested data, as d3's <c>hierarchy(data, children)</c>.</summary>
    /// <typeparam name="T">The data of a node.</typeparam>
    /// <param name="root">The root's data.</param>
    /// <param name="children">The children of a node's data; null or empty for a leaf.</param>
    /// <param name="data">Each node's data, by node index: nodes are numbered in pre-order.</param>
    /// <returns>The hierarchy.</returns>
    public static Hierarchy FromNested<T>(T root, Func<T, IReadOnlyList<T>?> children, out T[] data) =>
        HierarchyBuilder.FromNested(root, children, out data);

    /// <summary>Builds the hierarchy whose node <c>i</c> has the parent <paramref name="parents"/>[i].</summary>
    /// <param name="parents">Each node's parent index, or a negative number for the root.</param>
    /// <returns>The hierarchy, as d3's <c>stratify()</c> with each index as id and its parent as parent id.</returns>
    /// <exception cref="ArgumentException">A parent is out of range, or there are several roots, none, or a cycle.</exception>
    public static Hierarchy FromParents(ReadOnlySpan<int> parents) => HierarchyBuilder.FromParents(parents);

    /// <summary>Builds a hierarchy from tabular rows, as d3's <c>stratify()</c>; row <c>i</c> becomes node <c>i</c>.</summary>
    /// <param name="ids">Each row's id; null or empty for none.</param>
    /// <param name="parentIds">Each row's parent id; null or empty for the root.</param>
    /// <returns>The hierarchy.</returns>
    /// <exception cref="ArgumentException">With d3's message: <c>missing: id</c>, <c>ambiguous: id</c>, <c>multiple roots</c>, <c>no root</c> or <c>cycle</c>.</exception>
    public static Hierarchy Stratify(ReadOnlySpan<string?> ids, ReadOnlySpan<string?> parentIds) => HierarchyBuilder.Stratify(ids, parentIds);

    /// <summary>
    /// Builds a hierarchy from slash-separated paths, as d3's <c>stratify().path()</c>: row <c>i</c> becomes node
    /// <c>i</c>, missing directories are imputed after the rows, and imputed roots with a single child are dropped.
    /// </summary>
    /// <param name="paths">Each row's path.</param>
    /// <returns>The hierarchy; <see cref="Id"/> returns each node's normalized path.</returns>
    /// <exception cref="ArgumentException">With d3's message, as <see cref="Stratify"/>.</exception>
    public static Hierarchy StratifyPaths(ReadOnlySpan<string> paths) => HierarchyBuilder.StratifyPaths(paths);

    /// <summary>Returns the children of <paramref name="node"/>, in order; empty for a leaf.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The children.</returns>
    public ReadOnlySpan<int> Children(int node) => childList.AsSpan(childStart[node], childStart[node + 1] - childStart[node]);

    /// <summary>Returns the id of <paramref name="node"/>, for hierarchies built by <see cref="Stratify"/> or <see cref="StratifyPaths"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The id, or null when the node has none.</returns>
    public string? Id(int node) => ids?[node];

    /// <summary>Returns the input row of <paramref name="node"/>: the node itself, or -1 for a directory imputed by <see cref="StratifyPaths"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The row.</returns>
    public int Row(int node) => node < rows ? node : -1;

    /// <summary>Returns a hierarchy whose node values are their own value plus their descendants', as d3's <c>node.sum</c>.</summary>
    /// <param name="values">Each node's own value; NaN and zero count as zero.</param>
    /// <returns>The summed hierarchy.</returns>
    /// <exception cref="ArgumentException">The span does not hold one value per node.</exception>
    public Hierarchy Sum(ReadOnlySpan<double> values)
    {
        if (values.Length != Count)
        {
            throw new ArgumentException("There must be one value per node.", nameof(values));
        }

        var sums = new double[Count];
        using var order = new NodeOrder(this, NodeOrderKind.Post);
        foreach (var node in order.Nodes)
        {
            var sum = values[node];
            if (sum == 0 || double.IsNaN(sum))
            {
                sum = 0;
            }

            var children = Children(node);
            for (var i = children.Length - 1; i >= 0; i--)
            {
                sum += sums[children[i]];
            }

            sums[node] = sum;
        }

        return With(sums, childList);
    }

    /// <summary>Returns a hierarchy whose node values are their number of leaves, as d3's <c>node.count()</c>.</summary>
    /// <returns>The counted hierarchy.</returns>
    public Hierarchy CountLeaves()
    {
        var counts = new double[Count];
        using var order = new NodeOrder(this, NodeOrderKind.Post);
        foreach (var node in order.Nodes)
        {
            var children = Children(node);
            var sum = 0.0;
            if (children.Length == 0)
            {
                sum = 1;
            }

            for (var i = children.Length - 1; i >= 0; i--)
            {
                sum += counts[children[i]];
            }

            counts[node] = sum;
        }

        return With(counts, childList);
    }

    /// <summary>
    /// Returns a hierarchy whose children are sorted by <paramref name="compare"/>, as d3's <c>node.sort</c>: a
    /// negative result puts the first node first; the sort is stable and follows V8's <c>Array.prototype.sort</c>.
    /// </summary>
    /// <param name="compare">Compares two sibling nodes, as d3's comparator (for example <c>(a, b) =&gt; values[b] - values[a]</c>).</param>
    /// <returns>The sorted hierarchy.</returns>
    public Hierarchy Sort(Func<int, int, double> compare)
    {
        ArgumentNullException.ThrowIfNull(compare);
        var sorted = (int[])childList.Clone();
        for (var node = 0; node < Count; node++)
        {
            var count = childStart[node + 1] - childStart[node];
            if (count > 1)
            {
                ChildSort.Sort(sorted.AsSpan(childStart[node], count), compare);
            }
        }

        return With(values, sorted);
    }

    /// <summary>Returns every node, breadth first from the root, as d3's <c>node.descendants()</c>.</summary>
    /// <returns>The nodes.</returns>
    public int[] Descendants()
    {
        var nodes = new int[Count];
        nodes[0] = Root;
        var end = 1;
        for (var i = 0; i < end; i++)
        {
            foreach (var child in Children(nodes[i]))
            {
                nodes[end++] = child;
            }
        }

        return nodes;
    }

    /// <summary>Returns every node before its descendants, as d3's <c>node.eachBefore</c> visits them.</summary>
    /// <returns>The nodes in pre-order.</returns>
    public int[] PreOrder()
    {
        var nodes = new int[Count];
        WritePreOrder(nodes);
        return nodes;
    }

    /// <summary>Returns every node after its descendants, as d3's <c>node.eachAfter</c> visits them.</summary>
    /// <returns>The nodes in post-order.</returns>
    public int[] PostOrder()
    {
        var nodes = new int[Count];
        WritePostOrder(nodes);
        return nodes;
    }

    /// <summary>Returns the leaves in pre-order, as d3's <c>node.leaves()</c>.</summary>
    /// <returns>The leaves.</returns>
    public int[] Leaves()
    {
        var leaves = new List<int>();
        using var order = new NodeOrder(this, NodeOrderKind.Pre);
        foreach (var node in order.Nodes)
        {
            if (childStart[node] == childStart[node + 1])
            {
                leaves.Add(node);
            }
        }

        return [.. leaves];
    }

    /// <summary>Returns the links from each parent to its children, breadth first, as d3's <c>node.links()</c>.</summary>
    /// <returns>The links.</returns>
    public HierarchyLink[] Links()
    {
        var nodes = Descendants();
        var links = new HierarchyLink[nodes.Length - 1];
        for (var i = 1; i < nodes.Length; i++)
        {
            links[i - 1] = new HierarchyLink(parents[nodes[i]], nodes[i]);
        }

        return links;
    }

    /// <summary>Returns <paramref name="node"/> and its ancestors up to the root, as d3's <c>node.ancestors()</c>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The node, its parent, and so on to the root.</returns>
    public int[] Ancestors(int node)
    {
        var path = new int[depths[node] + 1];
        for (var i = 0; i < path.Length; i++)
        {
            path[i] = node;
            node = parents[node];
        }

        return path;
    }

    /// <summary>Returns the shortest path from <paramref name="from"/> to <paramref name="to"/> through their least common ancestor, as d3's <c>node.path</c>.</summary>
    /// <param name="from">The first node.</param>
    /// <param name="to">The last node.</param>
    /// <returns>The nodes of the path.</returns>
    public int[] Path(int from, int to)
    {
        var a = from;
        var b = to;
        while (depths[a] > depths[b])
        {
            a = parents[a];
        }

        while (depths[b] > depths[a])
        {
            b = parents[b];
        }

        while (a != b)
        {
            a = parents[a];
            b = parents[b];
        }

        var up = depths[from] - depths[a];
        var path = new int[up + 1 + depths[to] - depths[a]];
        for (var i = 0; i <= up; i++, from = parents[from])
        {
            path[i] = from;
        }

        for (var i = path.Length - 1; i > up; i--, to = parents[to])
        {
            path[i] = to;
        }

        return path;
    }

    internal int ChildCount(int node) => childStart[node + 1] - childStart[node];

    internal void WritePreOrder(Span<int> nodes)
    {
        var stack = ArrayPool<int>.Shared.Rent(Count);
        var top = 0;
        var end = 0;
        stack[top++] = Root;
        while (top > 0)
        {
            var node = stack[--top];
            nodes[end++] = node;
            var children = Children(node);
            for (var i = children.Length - 1; i >= 0; i--)
            {
                stack[top++] = children[i];
            }
        }

        ArrayPool<int>.Shared.Return(stack);
    }

    internal void WritePostOrder(Span<int> nodes)
    {
        var stack = ArrayPool<int>.Shared.Rent(Count);
        var top = 0;
        var end = nodes.Length;
        stack[top++] = Root;
        while (top > 0)
        {
            var node = stack[--top];
            nodes[--end] = node;
            foreach (var child in Children(node))
            {
                stack[top++] = child;
            }
        }

        ArrayPool<int>.Shared.Return(stack);
    }

    private Hierarchy With(double[] newValues, int[] newChildren) =>
        new(Root, parents, depths, heights, childStart, newChildren, newValues, ids, rows);
}
