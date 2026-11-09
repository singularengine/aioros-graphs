using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Scales;

/// <summary>
/// An ordinal scale of uniform bands, as d3-scale's <c>scaleBand</c>, over the indices of a domain: index <c>i</c> maps
/// to the start of its band. Immutable: the <c>With</c> methods return new scales.
/// </summary>
public sealed class BandScale
{
    private readonly double start;

    private BandScale(int count, double r0, double r1, bool round, double paddingInner, double paddingOuter, double align)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Count = count;
        RangeStart = r0;
        RangeEnd = r1;
        IsRounded = round;
        PaddingInner = paddingInner;
        PaddingOuter = paddingOuter;
        Align = align;
        IsReversed = r1 < r0;
        var (low, high) = IsReversed ? (r1, r0) : (r0, r1);
        var step = (high - low) / Math.Max(1, count - paddingInner + paddingOuter * 2);
        if (round)
        {
            step = Math.Floor(step);
        }

        low += (high - low - step * (count - paddingInner)) * align;
        var bandwidth = step * (1 - paddingInner);
        if (round)
        {
            low = JsNumber.Round(low);
            bandwidth = JsNumber.Round(bandwidth);
        }

        start = low;
        Step = step;
        Bandwidth = bandwidth;
    }

    /// <summary>The number of bands.</summary>
    public int Count { get; }

    /// <summary>The range's first value.</summary>
    public double RangeStart { get; }

    /// <summary>The range's second value.</summary>
    public double RangeEnd { get; }

    /// <summary>Whether the step and band starts are rounded to integers.</summary>
    public bool IsRounded { get; }

    /// <summary>The share of each step left between bands, at most 1.</summary>
    public double PaddingInner { get; }

    /// <summary>The space before the first band and after the last, in steps.</summary>
    public double PaddingOuter { get; }

    /// <summary>Where the bands sit within the range when there is space left, from 0 (start) to 1 (end).</summary>
    public double Align { get; }

    /// <summary>The distance between the starts of adjacent bands.</summary>
    public double Step { get; }

    /// <summary>The width of each band.</summary>
    public double Bandwidth { get; }

    private bool IsReversed { get; }

    /// <summary>Creates <paramref name="count"/> bands over the range from <paramref name="r0"/> to <paramref name="r1"/>, without padding.</summary>
    /// <param name="count">The number of bands.</param>
    /// <param name="r0">The range's first value.</param>
    /// <param name="r1">The range's second value.</param>
    /// <returns>The scale.</returns>
    public static BandScale Create(int count, double r0 = 0, double r1 = 1) => new(count, r0, r1, false, 0, 0, 0.5);

    /// <summary>Returns this scale with another number of bands.</summary>
    /// <param name="count">The number of bands.</param>
    /// <returns>The new scale.</returns>
    public BandScale WithCount(int count) => new(count, RangeStart, RangeEnd, IsRounded, PaddingInner, PaddingOuter, Align);

    /// <summary>Returns this scale with another range, not rounded.</summary>
    /// <param name="r0">The range's first value.</param>
    /// <param name="r1">The range's second value.</param>
    /// <returns>The new scale.</returns>
    public BandScale WithRange(double r0, double r1) => new(Count, r0, r1, false, PaddingInner, PaddingOuter, Align);

    /// <summary>Returns this scale with another range and rounding on, as d3's <c>rangeRound</c>.</summary>
    /// <param name="r0">The range's first value.</param>
    /// <param name="r1">The range's second value.</param>
    /// <returns>The new scale.</returns>
    public BandScale WithRangeRound(double r0, double r1) => new(Count, r0, r1, true, PaddingInner, PaddingOuter, Align);

    /// <summary>Returns this scale with rounding on or off.</summary>
    /// <param name="round">Whether to round.</param>
    /// <returns>The new scale.</returns>
    public BandScale WithRound(bool round) => new(Count, RangeStart, RangeEnd, round, PaddingInner, PaddingOuter, Align);

    /// <summary>Returns this scale with the same inner and outer padding, as d3's <c>padding</c>.</summary>
    /// <param name="padding">The padding; the inner padding is capped at 1.</param>
    /// <returns>The new scale.</returns>
    public BandScale WithPadding(double padding) => new(Count, RangeStart, RangeEnd, IsRounded, Math.Min(1, padding), padding, Align);

    /// <summary>Returns this scale with another inner padding.</summary>
    /// <param name="padding">The inner padding, capped at 1.</param>
    /// <returns>The new scale.</returns>
    public BandScale WithPaddingInner(double padding) => new(Count, RangeStart, RangeEnd, IsRounded, Math.Min(1, padding), PaddingOuter, Align);

    /// <summary>Returns this scale with another outer padding.</summary>
    /// <param name="padding">The outer padding, in steps.</param>
    /// <returns>The new scale.</returns>
    public BandScale WithPaddingOuter(double padding) => new(Count, RangeStart, RangeEnd, IsRounded, PaddingInner, padding, Align);

    /// <summary>Returns this scale with another alignment.</summary>
    /// <param name="align">The alignment, clamped to [0, 1].</param>
    /// <returns>The new scale.</returns>
    public BandScale WithAlign(double align) => new(Count, RangeStart, RangeEnd, IsRounded, PaddingInner, PaddingOuter, Math.Max(0, Math.Min(1, align)));

    /// <summary>Returns the start of band <paramref name="index"/>.</summary>
    /// <param name="index">The band's index in the domain.</param>
    /// <returns>The band's start, or NaN for an index outside the domain.</returns>
    public double Map(int index)
    {
        if ((uint)index >= (uint)Count)
        {
            return double.NaN;
        }

        return start + Step * (IsReversed ? Count - 1 - index : index);
    }
}
