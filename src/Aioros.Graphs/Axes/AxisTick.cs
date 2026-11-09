namespace Aioros.Graphs.Axes;

/// <summary>A tick of an axis.</summary>
/// <param name="Value">The tick's domain value; for band and point scales, the key's index.</param>
/// <param name="Position">Where the tick is along the axis, offset included: the number in d3's <c>translate</c>.</param>
/// <param name="Label">The tick's label.</param>
public readonly record struct AxisTick(double Value, double Position, string Label);
