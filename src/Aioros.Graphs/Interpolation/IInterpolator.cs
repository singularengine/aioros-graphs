namespace Aioros.Graphs.Interpolation;

/// <summary>
/// A d3-interpolate interpolator: maps <c>t</c> in [0, 1] to a value between two or more stops. Interpolators are
/// structs, so the scales and the code that drive them are specialized for each one.
/// </summary>
/// <typeparam name="T">The interpolated value.</typeparam>
public interface IInterpolator<out T>
{
    /// <summary>Returns the value at <paramref name="t"/>.</summary>
    /// <param name="t">The position, usually in [0, 1]; values outside extrapolate as d3's do.</param>
    /// <returns>The value.</returns>
    T Interpolate(double t);
}
