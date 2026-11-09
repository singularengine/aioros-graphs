using Aioros.Graphs.Time;

namespace Aioros.Graphs.Interpolation;

/// <summary>
/// Interpolates between two time values, as d3's <c>interpolateDate(a, b)</c>: <c>a * (1 - t) + b * t</c> set on a
/// <c>Date</c>, so clipped to whole milliseconds within ±8.64e15 (NaN beyond).
/// </summary>
/// <param name="A">The time value at 0, in milliseconds since the Unix epoch.</param>
/// <param name="B">The time value at 1.</param>
public readonly record struct DateInterpolator(double A, double B) : IInterpolator<double>
{
    /// <inheritdoc />
    public double Interpolate(double t) => UtcCalendar.Clip(A * (1 - t) + B * t);
}
