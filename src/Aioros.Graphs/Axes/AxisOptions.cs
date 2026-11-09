namespace Aioros.Graphs.Axes;

/// <summary>The settings of an axis, with d3-axis's defaults.</summary>
public readonly record struct AxisOptions
{
    /// <summary>Creates the default settings.</summary>
    public AxisOptions()
    {
    }

    /// <summary>Gets the length of each tick line, as <c>axis.tickSizeInner</c>; 6 by default.</summary>
    public double TickSizeInner { get; init; } = 6;

    /// <summary>Gets the length of the domain path's end ticks, as <c>axis.tickSizeOuter</c>; 6 by default, 0 for none.</summary>
    public double TickSizeOuter { get; init; } = 6;

    /// <summary>Gets the space between a tick line and its label, as <c>axis.tickPadding</c>; 3 by default.</summary>
    public double TickPadding { get; init; } = 3;

    /// <summary>
    /// Gets the offset added to every position so one-pixel lines fall on pixel centers, as <c>axis.offset</c>: 0.5 by
    /// default, which is d3's value outside a browser with a device pixel ratio above 1.
    /// </summary>
    public double Offset { get; init; } = 0.5;
}
