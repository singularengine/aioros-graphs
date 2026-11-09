namespace Aioros.Graphs.Paths;

/// <summary>
/// Receives the commands of a path as SVG path data names them. A sink is usually a struct, so the shapes that
/// write to it are specialized for it and call it without virtual dispatch.
/// </summary>
public interface IPathSink
{
    /// <summary>Starts a subpath at (<paramref name="x"/>, <paramref name="y"/>): SVG's <c>M</c>.</summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    void MoveTo(double x, double y);

    /// <summary>Draws a straight line to (<paramref name="x"/>, <paramref name="y"/>): SVG's <c>L</c>.</summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    void LineTo(double x, double y);

    /// <summary>Draws a quadratic Bézier curve: SVG's <c>Q</c>.</summary>
    /// <param name="x1">The control point's horizontal coordinate.</param>
    /// <param name="y1">The control point's vertical coordinate.</param>
    /// <param name="x">The end point's horizontal coordinate.</param>
    /// <param name="y">The end point's vertical coordinate.</param>
    void QuadraticCurveTo(double x1, double y1, double x, double y);

    /// <summary>Draws a cubic Bézier curve: SVG's <c>C</c>.</summary>
    /// <param name="x1">The first control point's horizontal coordinate.</param>
    /// <param name="y1">The first control point's vertical coordinate.</param>
    /// <param name="x2">The second control point's horizontal coordinate.</param>
    /// <param name="y2">The second control point's vertical coordinate.</param>
    /// <param name="x">The end point's horizontal coordinate.</param>
    /// <param name="y">The end point's vertical coordinate.</param>
    void BezierCurveTo(double x1, double y1, double x2, double y2, double x, double y);

    /// <summary>Draws a circular arc to (<paramref name="x"/>, <paramref name="y"/>): SVG's <c>A</c> with equal radii and no rotation.</summary>
    /// <param name="radius">The circle's radius.</param>
    /// <param name="largeArc">Whether the arc spans more than 180 degrees.</param>
    /// <param name="sweep">Whether the arc turns in the direction of increasing angles (clockwise with y down).</param>
    /// <param name="x">The end point's horizontal coordinate.</param>
    /// <param name="y">The end point's vertical coordinate.</param>
    void ArcTo(double radius, bool largeArc, bool sweep, double x, double y);

    /// <summary>Draws a closed rectangle as its own subpath: SVG's <c>M x,y h w v h h -w Z</c>.</summary>
    /// <param name="x">The left coordinate.</param>
    /// <param name="y">The top coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    void Rect(double x, double y, double width, double height);

    /// <summary>Closes the current subpath: SVG's <c>Z</c>.</summary>
    void ClosePath();
}
