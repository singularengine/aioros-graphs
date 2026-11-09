using Aioros.Graphs.Formatting;
using Aioros.Graphs.Scales;

namespace Aioros.Graphs.Axes;

/// <summary>Lays out d3-axis's axes (<c>axisTop</c>, <c>axisRight</c>, <c>axisBottom</c>, <c>axisLeft</c>) from the engine's scales, as data.</summary>
public static class Axis
{
    /// <summary>Lays out an axis from values already mapped to positions and labelled.</summary>
    /// <param name="orientation">The orientation.</param>
    /// <param name="range">The scale's range; the domain path runs from its first to its last stop.</param>
    /// <param name="values">The tick values.</param>
    /// <param name="positions">The scale's position of each value, without the offset.</param>
    /// <param name="labels">The label of each value.</param>
    /// <param name="options">The settings, d3's defaults when null.</param>
    /// <returns>The layout.</returns>
    /// <exception cref="ArgumentException">The range is empty, or the positions or labels are fewer than the values.</exception>
    public static AxisLayout Create(AxisOrientation orientation, ReadOnlySpan<double> range, ReadOnlySpan<double> values, ReadOnlySpan<double> positions, IReadOnlyList<string> labels, AxisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(labels);
        if (range.Length == 0)
        {
            throw new ArgumentException("An axis needs a range.", nameof(range));
        }

        if (positions.Length < values.Length || labels.Count < values.Length)
        {
            throw new ArgumentException("Every tick value needs a position and a label.", nameof(values));
        }

        var settings = options ?? new AxisOptions();
        var ticks = new AxisTick[values.Length];
        for (var i = 0; i < ticks.Length; i++)
        {
            ticks[i] = new AxisTick(values[i], positions[i] + settings.Offset, labels[i]);
        }

        return new AxisLayout(orientation, settings, range[0] + settings.Offset, range[^1] + settings.Offset, ticks);
    }

    /// <summary>Lays out the axis of a linear scale with its ticks and tick format, as <c>axis.ticks(count, specifier)</c>.</summary>
    /// <param name="orientation">The orientation.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="specifier">The tick format's specifier, or null for the scale's default.</param>
    /// <param name="options">The settings, d3's defaults when null.</param>
    /// <returns>The layout.</returns>
    public static AxisLayout Create(AxisOrientation orientation, LinearScale scale, double count = 10, string? specifier = null, AxisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scale);
        return Create(orientation, scale, scale.Ticks(count), scale.TickFormat(count, specifier), options);
    }

    /// <summary>Lays out the axis of a linear scale at explicit values, as <c>axis.tickValues(values)</c>.</summary>
    /// <param name="orientation">The orientation.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="values">The tick values.</param>
    /// <param name="format">The labels' format.</param>
    /// <param name="options">The settings, d3's defaults when null.</param>
    /// <returns>The layout.</returns>
    public static AxisLayout Create(AxisOrientation orientation, LinearScale scale, ReadOnlySpan<double> values, NumberFormat format, AxisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scale);
        ArgumentNullException.ThrowIfNull(format);
        var positions = new double[values.Length];
        var labels = new string[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            positions[i] = scale.Map(values[i]);
            labels[i] = format.Format(values[i]);
        }

        return Create(orientation, scale.Range, values, positions, labels, options);
    }

    /// <summary>Lays out the axis of a UTC time scale with its ticks and the multi-scale or specified time format.</summary>
    /// <param name="orientation">The orientation.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="specifier">A d3-time-format specifier, or null for the multi-scale format.</param>
    /// <param name="options">The settings, d3's defaults when null.</param>
    /// <returns>The layout.</returns>
    public static AxisLayout Create(AxisOrientation orientation, UtcScale scale, double count = 10, string? specifier = null, AxisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scale);
        var values = scale.Ticks(count);
        var format = UtcScale.TickFormat(specifier);
        var positions = new double[values.Length];
        var labels = new string[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            positions[i] = scale.Map(values[i]);
            labels[i] = format(values[i]);
        }

        return Create(orientation, scale.Range, values, positions, labels, options);
    }

    /// <summary>Lays out the axis of a band scale: one tick at the center of each band, labelled with its key.</summary>
    /// <param name="orientation">The orientation.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="keys">The key of each band, in the scale's order.</param>
    /// <param name="options">The settings, d3's defaults when null.</param>
    /// <returns>The layout.</returns>
    public static AxisLayout Create(AxisOrientation orientation, BandScale scale, IReadOnlyList<string> keys, AxisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scale);
        ArgumentNullException.ThrowIfNull(keys);
        var settings = options ?? new AxisOptions();
        var center = Math.Max(0, scale.Bandwidth - settings.Offset * 2) / 2;
        if (scale.IsRounded)
        {
            center = JsNumber.Round(center);
        }

        var count = Math.Min(keys.Count, scale.Count);
        var values = new double[count];
        var positions = new double[count];
        for (var i = 0; i < count; i++)
        {
            values[i] = i;
            positions[i] = scale.Map(i) + center;
        }

        return Create(orientation, [scale.RangeStart, scale.RangeEnd], values, positions, keys, settings);
    }

    /// <summary>Lays out the axis of a point scale: one tick at each point, labelled with its key.</summary>
    /// <param name="orientation">The orientation.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="keys">The key of each point, in the scale's order.</param>
    /// <param name="options">The settings, d3's defaults when null.</param>
    /// <returns>The layout.</returns>
    public static AxisLayout Create(AxisOrientation orientation, PointScale scale, IReadOnlyList<string> keys, AxisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scale);
        return Create(orientation, scale.Bands, keys, options);
    }

    /// <summary>Lays out the axis of a log scale with its log ticks and its tick format, which leaves some labels empty as d3's does.</summary>
    /// <param name="orientation">The orientation.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="specifier">The tick format's specifier, or null for the scale's default.</param>
    /// <param name="options">The settings, d3's defaults when null.</param>
    /// <returns>The layout.</returns>
    public static AxisLayout Create(AxisOrientation orientation, LogScale scale, double count = 10, string? specifier = null, AxisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scale);
        var format = scale.TickFormat(count, specifier);
        return Mapped(orientation, scale.Range, scale.Ticks(count), scale.Map, format.Format, options);
    }

    /// <summary>Lays out the axis of a power scale with its ticks and tick format.</summary>
    /// <param name="orientation">The orientation.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="specifier">The tick format's specifier, or null for the scale's default.</param>
    /// <param name="options">The settings, d3's defaults when null.</param>
    /// <returns>The layout.</returns>
    public static AxisLayout Create(AxisOrientation orientation, PowScale scale, double count = 10, string? specifier = null, AxisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scale);
        var format = scale.TickFormat(count, specifier);
        return Mapped(orientation, scale.Range, scale.Ticks(count), scale.Map, format.Format, options);
    }

    /// <summary>Lays out the axis of a symlog scale with its ticks and tick format.</summary>
    /// <param name="orientation">The orientation.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="specifier">The tick format's specifier, or null for the scale's default.</param>
    /// <param name="options">The settings, d3's defaults when null.</param>
    /// <returns>The layout.</returns>
    public static AxisLayout Create(AxisOrientation orientation, SymlogScale scale, double count = 10, string? specifier = null, AxisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scale);
        var format = scale.TickFormat(count, specifier);
        return Mapped(orientation, scale.Range, scale.Ticks(count), scale.Map, format.Format, options);
    }

    /// <summary>Lays out the axis of a local time scale with its ticks and the multi-scale or specified time format in its zone.</summary>
    /// <param name="orientation">The orientation.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="specifier">A d3-time-format specifier, or null for the multi-scale format.</param>
    /// <param name="options">The settings, d3's defaults when null.</param>
    /// <returns>The layout.</returns>
    public static AxisLayout Create(AxisOrientation orientation, TimeScale scale, double count = 10, string? specifier = null, AxisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scale);
        return Mapped(orientation, scale.Range, scale.Ticks(count), scale.Map, scale.TickFormat(specifier), options);
    }

    private static AxisLayout Mapped(AxisOrientation orientation, ReadOnlySpan<double> range, double[] values, Func<double, double> map, Func<double, string> format, AxisOptions? options)
    {
        var positions = new double[values.Length];
        var labels = new string[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            positions[i] = map(values[i]);
            labels[i] = format(values[i]);
        }

        return Create(orientation, range, values, positions, labels, options);
    }
}
