namespace Aioros.Graphs.Interpolation;

/// <summary>
/// Interpolates between two angles in degrees along the shorter arc, as d3's <c>interpolateHue(a, b)</c>: the result
/// is within [0, 360), and a NaN end takes the other.
/// </summary>
public readonly struct HueInterpolator : IInterpolator<double>
{
    private readonly Channel hue;

    /// <summary>Initializes a new instance of the <see cref="HueInterpolator"/> struct.</summary>
    /// <param name="a">The angle at 0.</param>
    /// <param name="b">The angle at 1.</param>
    public HueInterpolator(double a, double b)
    {
        hue = Channel.Hue(a, b);
    }

    /// <inheritdoc />
    public double Interpolate(double t)
    {
        var x = hue.At(t);
        return x - 360 * Math.Floor(x / 360);
    }
}
