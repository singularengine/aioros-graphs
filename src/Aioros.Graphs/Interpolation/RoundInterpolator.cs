using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Interpolation;

/// <summary>Interpolates between two numbers and rounds as JavaScript's <c>Math.round</c>, as d3's <c>interpolateRound(a, b)</c>.</summary>
/// <param name="A">The value at 0.</param>
/// <param name="B">The value at 1.</param>
public readonly record struct RoundInterpolator(double A, double B) : IInterpolator<double>
{
    /// <inheritdoc />
    public double Interpolate(double t) => JsNumber.Round(A * (1 - t) + B * t);
}
