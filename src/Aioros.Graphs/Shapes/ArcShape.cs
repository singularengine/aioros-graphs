namespace Aioros.Graphs.Shapes;

/// <summary>
/// A circular or annular sector, as d3-shape's arc generator reads it: angles in radians, clockwise from twelve o'clock.
/// </summary>
/// <param name="InnerRadius">The inner radius; 0 for a pie slice.</param>
/// <param name="OuterRadius">The outer radius.</param>
/// <param name="StartAngle">The start angle.</param>
/// <param name="EndAngle">The end angle.</param>
public readonly record struct ArcShape(double InnerRadius, double OuterRadius, double StartAngle, double EndAngle)
{
    /// <summary>The angle left between adjacent arcs, split between both sides.</summary>
    public double PadAngle { get; init; }

    /// <summary>The radius of the rounded corners, at most half the ring's thickness.</summary>
    public double CornerRadius { get; init; }

    /// <summary>The radius the pad angle is measured at; NaN (the default) for the root mean square of the radii.</summary>
    public double PadRadius { get; init; } = double.NaN;
}
