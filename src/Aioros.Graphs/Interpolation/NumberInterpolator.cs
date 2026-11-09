namespace Aioros.Graphs.Interpolation;

/// <summary>Interpolates between two numbers, as d3's <c>interpolateNumber(a, b)</c>: <c>a * (1 - t) + b * t</c>.</summary>
/// <param name="A">The value at 0.</param>
/// <param name="B">The value at 1.</param>
public readonly record struct NumberInterpolator(double A, double B) : IInterpolator<double>
{
    /// <inheritdoc />
    public double Interpolate(double t) => A * (1 - t) + B * t;
}
