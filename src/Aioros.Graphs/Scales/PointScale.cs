namespace Aioros.Graphs.Scales;

/// <summary>
/// An ordinal scale of evenly spaced points, as d3-scale's <c>scalePoint</c>: a band scale whose bands have no width.
/// Immutable: the <c>With</c> methods return new scales.
/// </summary>
public sealed class PointScale
{
    private readonly BandScale bands;

    private PointScale(BandScale bands)
    {
        this.bands = bands;
    }

    /// <summary>The number of points.</summary>
    public int Count => bands.Count;

    /// <summary>The space before the first point and after the last, in steps.</summary>
    public double Padding => bands.PaddingOuter;

    /// <summary>The distance between adjacent points.</summary>
    public double Step => bands.Step;

    /// <summary>Always 0.</summary>
    public double Bandwidth => bands.Bandwidth;

    internal BandScale Bands => bands;

    /// <summary>Creates <paramref name="count"/> points over the range from <paramref name="r0"/> to <paramref name="r1"/>.</summary>
    /// <param name="count">The number of points.</param>
    /// <param name="r0">The range's first value.</param>
    /// <param name="r1">The range's second value.</param>
    /// <returns>The scale.</returns>
    public static PointScale Create(int count, double r0 = 0, double r1 = 1) => new(BandScale.Create(count, r0, r1).WithPaddingInner(1));

    /// <summary>Returns this scale with another number of points.</summary>
    /// <param name="count">The number of points.</param>
    /// <returns>The new scale.</returns>
    public PointScale WithCount(int count) => new(bands.WithCount(count));

    /// <summary>Returns this scale with another range, not rounded.</summary>
    /// <param name="r0">The range's first value.</param>
    /// <param name="r1">The range's second value.</param>
    /// <returns>The new scale.</returns>
    public PointScale WithRange(double r0, double r1) => new(bands.WithRange(r0, r1));

    /// <summary>Returns this scale with another range and rounding on.</summary>
    /// <param name="r0">The range's first value.</param>
    /// <param name="r1">The range's second value.</param>
    /// <returns>The new scale.</returns>
    public PointScale WithRangeRound(double r0, double r1) => new(bands.WithRangeRound(r0, r1));

    /// <summary>Returns this scale with another outer padding.</summary>
    /// <param name="padding">The padding, in steps.</param>
    /// <returns>The new scale.</returns>
    public PointScale WithPadding(double padding) => new(bands.WithPaddingOuter(padding));

    /// <summary>Returns this scale with another alignment.</summary>
    /// <param name="align">The alignment, clamped to [0, 1].</param>
    /// <returns>The new scale.</returns>
    public PointScale WithAlign(double align) => new(bands.WithAlign(align));

    /// <summary>Returns point <paramref name="index"/>.</summary>
    /// <param name="index">The point's index in the domain.</param>
    /// <returns>The point, or NaN for an index outside the domain.</returns>
    public double Map(int index) => bands.Map(index);
}
