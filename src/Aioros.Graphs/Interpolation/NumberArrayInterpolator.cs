namespace Aioros.Graphs.Interpolation;

/// <summary>
/// Interpolates between two arrays of numbers, as d3's <c>interpolateNumberArray(a, b)</c>: the result has the end's
/// length, the values both arrays have are interpolated and the others are the end's. As in d3, <see cref="Interpolate(double)"/>
/// returns the same buffer each time; <see cref="Interpolate(double, Span{double})"/> writes into the caller's.
/// </summary>
public readonly struct NumberArrayInterpolator : IInterpolator<ReadOnlyMemory<double>>
{
    private readonly double[] a;
    private readonly double[] b;
    private readonly double[] buffer;

    /// <summary>Initializes a new instance of the <see cref="NumberArrayInterpolator"/> struct.</summary>
    /// <param name="a">The values at 0.</param>
    /// <param name="b">The values at 1.</param>
    public NumberArrayInterpolator(ReadOnlySpan<double> a, ReadOnlySpan<double> b)
    {
        this.a = a[..Math.Min(a.Length, b.Length)].ToArray();
        this.b = b.ToArray();
        buffer = b.ToArray();
    }

    /// <inheritdoc />
    public ReadOnlyMemory<double> Interpolate(double t)
    {
        Interpolate(t, buffer);
        return buffer;
    }

    /// <summary>Writes the values at <paramref name="t"/>.</summary>
    /// <param name="t">The position.</param>
    /// <param name="destination">At least as many values as the end array.</param>
    public void Interpolate(double t, Span<double> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, b.Length);
        for (var i = 0; i < a.Length; i++)
        {
            destination[i] = a[i] * (1 - t) + b[i] * t;
        }

        b.AsSpan(a.Length).CopyTo(destination[a.Length..]);
    }
}
