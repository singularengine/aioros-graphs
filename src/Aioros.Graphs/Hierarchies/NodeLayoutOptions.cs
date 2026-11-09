namespace Aioros.Graphs.Hierarchies;

/// <summary>How far apart a tree or cluster layout places two neighboring nodes.</summary>
public enum NodeSeparation
{
    /// <summary>d3's default: 1 between siblings, 2 between cousins.</summary>
    Default,

    /// <summary>The radial separation of d3's examples: the default divided by the first node's depth.</summary>
    Radial,
}

/// <summary>The settings of a <see cref="Tree"/> or <see cref="Cluster"/> layout.</summary>
public sealed record NodeLayoutOptions
{
    /// <summary>The layout's width, or the horizontal spacing of nodes when <see cref="NodeSize"/> is set.</summary>
    public double Width { get; init; } = 1;

    /// <summary>The layout's height, or the spacing of depths when <see cref="NodeSize"/> is set.</summary>
    public double Height { get; init; } = 1;

    /// <summary>Whether <see cref="Width"/> and <see cref="Height"/> are a node's size (d3's <c>nodeSize</c>) instead of the layout's (<c>size</c>).</summary>
    public bool NodeSize { get; init; }

    /// <summary>The separation of neighboring nodes.</summary>
    public NodeSeparation Separation { get; init; }
}

/// <summary>The positions of a hierarchy's nodes, by node index.</summary>
/// <param name="X">Each node's x.</param>
/// <param name="Y">Each node's y.</param>
public readonly record struct NodePoints(double[] X, double[] Y);

internal static class Separations
{
    public static double Of(Hierarchy hierarchy, NodeSeparation separation, int a, int b)
    {
        var parents = hierarchy.Parents;
        var gap = parents[a] == parents[b] ? 1.0 : 2.0;
        return separation == NodeSeparation.Radial ? gap / hierarchy.Depths[a] : gap;
    }
}
