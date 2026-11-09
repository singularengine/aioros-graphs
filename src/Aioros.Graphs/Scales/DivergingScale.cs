using Aioros.Graphs.Interpolation;

namespace Aioros.Graphs.Scales;

/// <summary>
/// A diverging scale, as d3-scale's <c>scaleDiverging</c>, <c>scaleDivergingLog</c>, <c>scaleDivergingPow</c>,
/// <c>scaleDivergingSqrt</c> and <c>scaleDivergingSymlog</c>: maps a three-stop domain to [0, 0.5] and [0.5, 1] through
/// its transform, then through an interpolator, such as a diverging color scheme. The interpolator is a struct, so each scale is specialized for
/// it. Immutable: the <c>With</c> methods and <see cref="Nice"/> return new scales.
/// </summary>
/// <typeparam name="T">The interpolated value, such as a color.</typeparam>
/// <typeparam name="TInterpolator">The interpolator.</typeparam>
public sealed class DivergingScale<T, TInterpolator>
    where TInterpolator : struct, IInterpolator<T>
{
    private readonly InterpolatedDomain domain;
    private readonly TInterpolator interpolator;
    private readonly double t1;
    private readonly double k10;
    private readonly double k21;
    private readonly double s;

    private DivergingScale(InterpolatedDomain domain, TInterpolator interpolator, bool clamp, T unknown)
    {
        this.domain = domain;
        this.interpolator = interpolator;
        IsClamped = clamp;
        Unknown = unknown;
        var t0 = domain.Transform.Apply(domain.Stops[0]);
        t1 = domain.Transform.Apply(domain.Stops[1]);
        var t2 = domain.Transform.Apply(domain.Stops[2]);
        k10 = t0 == t1 ? 0 : 0.5 / (t1 - t0);
        k21 = t1 == t2 ? 0 : 0.5 / (t2 - t1);
        s = t1 < t0 ? -1 : 1;
    }

    /// <summary>The domain's three stops: the low end, the midpoint and the high end.</summary>
    public ReadOnlySpan<double> Domain => domain.Stops;

    /// <summary>The interpolator.</summary>
    public TInterpolator Interpolator => interpolator;

    /// <summary>Whether positions are clamped to [0, 1].</summary>
    public bool IsClamped { get; }

    /// <summary>The value NaN maps to; <c>default</c> unless set with <see cref="WithUnknown"/>.</summary>
    public T Unknown { get; }

    /// <summary>The interpolator's values at 0, 0.5 and 1, as d3's <c>scale.range()</c>.</summary>
    public (T Start, T Middle, T End) Range => (interpolator.Interpolate(0), interpolator.Interpolate(0.5), interpolator.Interpolate(1));

    /// <summary>Creates a linear sequential scale, as d3's <c>scaleDiverging(domain, interpolator)</c>.</summary>
    /// <param name="domain">Exactly three stops.</param>
    /// <param name="interpolator">The interpolator.</param>
    /// <returns>The scale.</returns>
    /// <exception cref="ArgumentException">The domain does not have three stops.</exception>
    public static DivergingScale<T, TInterpolator> Create(ReadOnlySpan<double> domain, TInterpolator interpolator) =>
        New(domain, DomainKind.Linear, 0, interpolator);

    /// <summary>Creates a logarithmic sequential scale, as d3's <c>scaleDivergingLog</c>.</summary>
    /// <param name="domain">Exactly three stops of the same sign.</param>
    /// <param name="interpolator">The interpolator.</param>
    /// <param name="base">The base of the ticks and of <see cref="Nice"/>.</param>
    /// <returns>The scale.</returns>
    /// <exception cref="ArgumentException">The domain does not have three stops.</exception>
    public static DivergingScale<T, TInterpolator> CreateLog(ReadOnlySpan<double> domain, TInterpolator interpolator, double @base = 10) =>
        New(domain, DomainKind.Log, @base, interpolator);

    /// <summary>Creates a power sequential scale, as d3's <c>scaleDivergingPow</c>.</summary>
    /// <param name="domain">Exactly three stops.</param>
    /// <param name="interpolator">The interpolator.</param>
    /// <param name="exponent">The exponent.</param>
    /// <returns>The scale.</returns>
    /// <exception cref="ArgumentException">The domain does not have three stops.</exception>
    public static DivergingScale<T, TInterpolator> CreatePow(ReadOnlySpan<double> domain, TInterpolator interpolator, double exponent) =>
        New(domain, DomainKind.Pow, exponent, interpolator);

    /// <summary>Creates a square-root sequential scale, as d3's <c>scaleDivergingSqrt</c>.</summary>
    /// <param name="domain">Exactly three stops.</param>
    /// <param name="interpolator">The interpolator.</param>
    /// <returns>The scale.</returns>
    /// <exception cref="ArgumentException">The domain does not have three stops.</exception>
    public static DivergingScale<T, TInterpolator> CreateSqrt(ReadOnlySpan<double> domain, TInterpolator interpolator) =>
        New(domain, DomainKind.Pow, 0.5, interpolator);

    /// <summary>Creates a bi-symmetric log sequential scale, as d3's <c>scaleDivergingSymlog</c>.</summary>
    /// <param name="domain">Exactly three stops.</param>
    /// <param name="interpolator">The interpolator.</param>
    /// <param name="constant">The constant where the scale turns logarithmic.</param>
    /// <returns>The scale.</returns>
    /// <exception cref="ArgumentException">The domain does not have three stops.</exception>
    public static DivergingScale<T, TInterpolator> CreateSymlog(ReadOnlySpan<double> domain, TInterpolator interpolator, double constant = 1) =>
        New(domain, DomainKind.Symlog, constant, interpolator);

    /// <summary>Returns this scale with another domain.</summary>
    /// <param name="stops">Exactly three stops.</param>
    /// <returns>The new scale.</returns>
    /// <exception cref="ArgumentException">The domain does not have three stops.</exception>
    public DivergingScale<T, TInterpolator> WithDomain(ReadOnlySpan<double> stops) =>
        new(domain.With(InterpolatedDomain.Check(stops, 3, nameof(stops))), interpolator, IsClamped, Unknown);

    /// <summary>Returns this scale with another interpolator.</summary>
    /// <param name="value">The interpolator.</param>
    /// <returns>The new scale.</returns>
    public DivergingScale<T, TInterpolator> WithInterpolator(TInterpolator value) => new(domain, value, IsClamped, Unknown);

    /// <summary>Returns this scale with clamping on or off.</summary>
    /// <param name="clamp">Whether positions are clamped to [0, 1].</param>
    /// <returns>The new scale.</returns>
    public DivergingScale<T, TInterpolator> WithClamp(bool clamp) => new(domain, interpolator, clamp, Unknown);

    /// <summary>Returns this scale with another value for NaN.</summary>
    /// <param name="value">The value NaN maps to.</param>
    /// <returns>The new scale.</returns>
    public DivergingScale<T, TInterpolator> WithUnknown(T value) => new(domain, interpolator, IsClamped, value);

    /// <summary>Returns the position of <paramref name="x"/> in the domain, the value given to the interpolator.</summary>
    /// <param name="x">The domain value.</param>
    /// <returns>0 at the first stop, 0.5 at the midpoint, 1 at the last, NaN for NaN.</returns>
    public double Position(double x)
    {
        if (double.IsNaN(x))
        {
            return double.NaN;
        }

        var tx = domain.Transform.Apply(x);
        var t = 0.5 + (tx - t1) * (s * tx < s * t1 ? k10 : k21);
        return IsClamped ? Math.Max(0, Math.Min(1, t)) : t;
    }

    /// <summary>Maps a domain value through the interpolator.</summary>
    /// <param name="x">The domain value.</param>
    /// <returns>The interpolated value, or <see cref="Unknown"/> for NaN.</returns>
    public T Map(double x) => double.IsNaN(x) ? Unknown : interpolator.Interpolate(Position(x));

    /// <summary>Maps domain values without allocating.</summary>
    /// <param name="values">The domain values.</param>
    /// <param name="destination">At least as many values as <paramref name="values"/>.</param>
    public void Map(ReadOnlySpan<double> values, Span<T> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            destination[i] = Map(values[i]);
        }
    }

    /// <summary>Returns about <paramref name="count"/> ticks, as d3's <c>scale.ticks(count)</c>: log ticks for the log kind, linear ticks otherwise.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <returns>The ticks.</returns>
    public double[] Ticks(double count = 10) => domain.Ticks(count);

    /// <summary>Writes the ticks of <see cref="Ticks(double)"/> without allocating.</summary>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="destination">Where the ticks go.</param>
    /// <returns>The number of ticks; when it exceeds the destination's length, the destination holds no meaningful values.</returns>
    public int Ticks(double count, Span<double> destination) => domain.Ticks(count, destination);

    /// <summary>Returns the tick format, as d3's <c>scale.tickFormat(count, specifier)</c>.</summary>
    /// <param name="count">The tick count the labels are for.</param>
    /// <param name="specifier">A d3-format specifier, or null for the scale kind's default.</param>
    /// <returns>The format.</returns>
    public TickLabelFormat TickFormat(double count = 10, string? specifier = null) => domain.TickFormat(count, specifier);

    /// <summary>Returns this scale with a nice domain, as d3's <c>nice</c>: rounded to powers of the base for the log kind, to tick steps otherwise.</summary>
    /// <param name="count">The tick count the nice domain is built for; ignored by the log kind.</param>
    /// <returns>The new scale.</returns>
    public DivergingScale<T, TInterpolator> Nice(double count = 10) =>
        domain.Nice(count) is { } stops ? new(domain.With(stops), interpolator, IsClamped, Unknown) : this;

    private static DivergingScale<T, TInterpolator> New(ReadOnlySpan<double> stops, DomainKind kind, double parameter, TInterpolator interpolator) =>
        new(new InterpolatedDomain(InterpolatedDomain.Check(stops, 3, nameof(stops)), kind, parameter), interpolator, false, default!);
}
