namespace Aioros.Graphs.Interpolation;

/// <summary>
/// Chains interpolators between consecutive stops, as d3's <c>piecewise(interpolate, values)</c>: with n pieces, t in
/// [i / n, (i + 1) / n] runs the i-th piece, and t outside [0, 1] extrapolates the first or the last.
/// </summary>
/// <typeparam name="TInterpolator">The interpolator of each piece.</typeparam>
/// <typeparam name="T">The interpolated value.</typeparam>
public readonly struct PiecewiseInterpolator<TInterpolator, T> : IInterpolator<T>
    where TInterpolator : struct, IInterpolator<T>
{
    private readonly TInterpolator[] pieces;

    /// <summary>Initializes a new instance of the <see cref="PiecewiseInterpolator{TInterpolator, T}"/> struct.</summary>
    /// <param name="pieces">The interpolators between consecutive stops, at least one.</param>
    public PiecewiseInterpolator(ReadOnlySpan<TInterpolator> pieces)
    {
        if (pieces.IsEmpty)
        {
            throw new ArgumentException("A piecewise interpolator needs at least two stops.", nameof(pieces));
        }

        this.pieces = pieces.ToArray();
    }

    /// <inheritdoc />
    public T Interpolate(double t)
    {
        var n = pieces.Length;
        t *= n;
        var i = Math.Max(0, Math.Min(n - 1, Math.Floor(t)));
        var index = double.IsNaN(i) ? 0 : (int)i;
        return pieces[index].Interpolate(t - index);
    }
}
