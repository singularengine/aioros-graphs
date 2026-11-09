namespace Aioros.Graphs.Chords;

/// <summary>
/// A group of a chord layout, or one end of a chord (a subgroup), as d3-chord returns them: the arc a group or a flow
/// takes around the circle.
/// </summary>
/// <param name="Index">The group's row in the matrix.</param>
/// <param name="StartAngle">The start angle in radians, clockwise from twelve o'clock.</param>
/// <param name="EndAngle">The end angle in radians.</param>
/// <param name="Value">The group's total (its row sum, plus its column sum when directed) or the flow's value.</param>
public readonly record struct ChordGroup(int Index, double StartAngle, double EndAngle, double Value);

/// <summary>A chord between two groups, as d3-chord returns it.</summary>
/// <param name="Source">The source end: the larger flow of an undirected pair, the outgoing flow when directed.</param>
/// <param name="Target">The target end.</param>
public readonly record struct Chord(ChordGroup Source, ChordGroup Target);

/// <summary>How a chord layout orders groups, subgroups or chords, as d3-array's <c>ascending</c> and <c>descending</c>.</summary>
public enum ChordSort
{
    /// <summary>In input order.</summary>
    None,

    /// <summary>Smallest value first.</summary>
    Ascending,

    /// <summary>Largest value first.</summary>
    Descending,
}

/// <summary>The settings of a chord layout.</summary>
public sealed record ChordOptions
{
    /// <summary>The angle between adjacent groups; negative values count as 0, as d3's <c>padAngle</c>.</summary>
    public double PadAngle { get; init; }

    /// <summary>The order of the groups around the circle, by their totals.</summary>
    public ChordSort SortGroups { get; init; }

    /// <summary>The order of the subgroups within each group, by their values.</summary>
    public ChordSort SortSubgroups { get; init; }

    /// <summary>The order of the chords (their drawing order), by the sum of their ends' values.</summary>
    public ChordSort SortChords { get; init; }

    /// <summary>Whether flows are directed, as d3's <c>chordDirected</c>: a chord per nonzero entry.</summary>
    public bool Directed { get; init; }

    /// <summary>Whether the matrix is read transposed, as d3's <c>chordTranspose</c>.</summary>
    public bool Transpose { get; init; }
}
