namespace Aioros.Graphs.Interpolation;

/// <summary>
/// A uniform nonrational B-spline through numbers, as d3's <c>interpolateBasis(values)</c>, or a closed one, as
/// <c>interpolateBasisClosed(values)</c>, from <see cref="Closed(ReadOnlySpan{double})"/>.
/// </summary>
public readonly struct BasisInterpolator : IInterpolator<double>
{
    private readonly double[] values;
    private readonly bool closed;

    /// <summary>Initializes a new instance of the <see cref="BasisInterpolator"/> struct: an open spline through the first and last values.</summary>
    /// <param name="values">The control values.</param>
    public BasisInterpolator(ReadOnlySpan<double> values)
        : this(values.ToArray(), false)
    {
    }

    private BasisInterpolator(double[] values, bool closed)
    {
        this.values = values;
        this.closed = closed;
    }

    /// <summary>Returns a closed spline, as <c>interpolateBasisClosed(values)</c>: t wraps around 1.</summary>
    /// <param name="values">The control values.</param>
    /// <returns>The interpolator.</returns>
    public static BasisInterpolator Closed(ReadOnlySpan<double> values) => new(values.ToArray(), true);

    /// <inheritdoc />
    public double Interpolate(double t) => closed ? ClosedAt(values, t) : OpenAt(values, t);

    internal static double OpenAt(ReadOnlySpan<double> values, double t)
    {
        var n = values.Length - 1;
        double i;
        if (t <= 0)
        {
            t = 0;
            i = 0;
        }
        else if (t >= 1)
        {
            t = 1;
            i = n - 1;
        }
        else
        {
            i = Math.Floor(t * n);
        }

        var v1 = At(values, i);
        var v2 = At(values, i + 1);
        var v0 = i > 0 ? At(values, i - 1) : 2 * v1 - v2;
        var v3 = i < n - 1 ? At(values, i + 2) : 2 * v2 - v1;
        return Basis((t - i / n) * n, v0, v1, v2, v3);
    }

    internal static double ClosedAt(ReadOnlySpan<double> values, double t)
    {
        var n = values.Length;
        t %= 1;
        if (t < 0)
        {
            ++t;
        }

        var i = Math.Floor(t * n);
        var v0 = At(values, (i + n - 1) % n);
        var v1 = At(values, i % n);
        var v2 = At(values, (i + 1) % n);
        var v3 = At(values, (i + 2) % n);
        return Basis((t - i / n) * n, v0, v1, v2, v3);
    }

    private static double Basis(double t1, double v0, double v1, double v2, double v3)
    {
        var t2 = t1 * t1;
        var t3 = t2 * t1;
        return ((1 - 3 * t1 + 3 * t2 - t3) * v0
            + (4 - 6 * t2 + 3 * t3) * v1
            + (1 + 3 * t1 + 3 * t2 - 3 * t3) * v2
            + t3 * v3) / 6;
    }

    private static double At(ReadOnlySpan<double> values, double i) =>
        i >= 0 && i < values.Length && i == Math.Floor(i) ? values[(int)i] : double.NaN;
}
