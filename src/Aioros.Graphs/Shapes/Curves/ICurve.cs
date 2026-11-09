using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

/// <summary>
/// A d3-shape curve: receives the points of a line or an area and writes the path between them. Curves are structs, so
/// the shapes that drive them are specialized for each one.
/// </summary>
public interface ICurve
{
    /// <summary>Starts an area: the next two lines are its topline and its baseline.</summary>
    void AreaStart();

    /// <summary>Ends an area.</summary>
    void AreaEnd();

    /// <summary>Starts a line segment.</summary>
    void LineStart();

    /// <summary>Ends a line segment, writing what the curve still holds.</summary>
    /// <typeparam name="TSink">The sink of the path.</typeparam>
    /// <param name="context">The path being written.</param>
    void LineEnd<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink;

    /// <summary>Adds a point to the current line segment.</summary>
    /// <typeparam name="TSink">The sink of the path.</typeparam>
    /// <param name="context">The path being written.</param>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    void Point<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink;
}
