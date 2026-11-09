namespace Aioros.Graphs.Chords;

/// <summary>
/// The two ends of a ribbon: a radius and an angular span for its source and for its target, as d3's ribbon reads them
/// from a chord.
/// </summary>
/// <param name="SourceRadius">The source end's radius.</param>
/// <param name="SourceStartAngle">The source end's start angle in radians.</param>
/// <param name="SourceEndAngle">The source end's end angle in radians.</param>
/// <param name="TargetRadius">The target end's radius.</param>
/// <param name="TargetStartAngle">The target end's start angle in radians.</param>
/// <param name="TargetEndAngle">The target end's end angle in radians.</param>
public readonly record struct RibbonShape(
    double SourceRadius,
    double SourceStartAngle,
    double SourceEndAngle,
    double TargetRadius,
    double TargetStartAngle,
    double TargetEndAngle)
{
    /// <summary>The angle taken off each end, half on each side, as d3's <c>ribbon.padAngle</c>.</summary>
    public double PadAngle { get; init; }

    /// <summary>Returns the ribbon of <paramref name="chord"/> with both ends at <paramref name="radius"/>.</summary>
    /// <param name="chord">The chord.</param>
    /// <param name="radius">The radius of both ends.</param>
    /// <returns>The ribbon.</returns>
    public static RibbonShape Of(in Chord chord, double radius) => Of(chord, radius, radius);

    /// <summary>Returns the ribbon of <paramref name="chord"/> with its ends at the given radii.</summary>
    /// <param name="chord">The chord.</param>
    /// <param name="sourceRadius">The source end's radius.</param>
    /// <param name="targetRadius">The target end's radius.</param>
    /// <returns>The ribbon.</returns>
    public static RibbonShape Of(in Chord chord, double sourceRadius, double targetRadius) =>
        new(sourceRadius, chord.Source.StartAngle, chord.Source.EndAngle, targetRadius, chord.Target.StartAngle, chord.Target.EndAngle);
}
