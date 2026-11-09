using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Scales;

/// <summary>
/// The tick labels of a log, sequential or diverging scale: a number format that, for log scales as d3's
/// <c>scaleLog().tickFormat(count, specifier)</c>, leaves empty the ticks whose leading digit is above what the count
/// allows, so crowded decades stay readable; other scales label every tick.
/// </summary>
public sealed class TickLabelFormat
{
    private readonly NumberFormat format;
    private readonly LogBase logBase;
    private readonly double limit;

    internal TickLabelFormat(NumberFormat format, LogBase logBase, double limit)
    {
        this.format = format;
        this.logBase = logBase;
        this.limit = limit;
    }

    internal TickLabelFormat(NumberFormat format)
        : this(format, default, double.PositiveInfinity)
    {
    }

    /// <summary>Labels a tick.</summary>
    /// <param name="value">The tick.</param>
    /// <returns>The label, or an empty string for a tick d3 hides.</returns>
    public string Format(double value)
    {
        if (double.IsPositiveInfinity(limit))
        {
            return format.Format(value);
        }

        var i = value / logBase.Pows(JsNumber.Round(logBase.Logs(value)));
        if (i * logBase.Base < logBase.Base - 0.5)
        {
            i *= logBase.Base;
        }

        return i <= limit ? format.Format(value) : "";
    }
}
