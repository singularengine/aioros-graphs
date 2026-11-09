namespace Aioros.Graphs.Axes;

/// <summary>Where an axis's ticks and labels point, as d3's <c>axisTop</c>, <c>axisRight</c>, <c>axisBottom</c> and <c>axisLeft</c>.</summary>
public enum AxisOrientation
{
    /// <summary>A horizontal axis with ticks above it: <c>axisTop</c>.</summary>
    Top,

    /// <summary>A vertical axis with ticks to its right: <c>axisRight</c>.</summary>
    Right,

    /// <summary>A horizontal axis with ticks below it: <c>axisBottom</c>.</summary>
    Bottom,

    /// <summary>A vertical axis with ticks to its left: <c>axisLeft</c>.</summary>
    Left,
}
