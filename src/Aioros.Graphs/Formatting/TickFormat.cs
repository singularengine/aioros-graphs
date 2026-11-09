using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Formatting;

/// <summary>The tick label format of a linear scale, as d3-scale's <c>tickFormat</c>: the specifier's precision derived from the tick step.</summary>
public static class TickFormat
{
    private static readonly FormatSpecifier DefaultSpecifier = FormatSpecifier.Parse(",f");

    /// <summary>Returns the format for the ticks between <paramref name="start"/> and <paramref name="stop"/>.</summary>
    /// <param name="start">The first bound of the domain.</param>
    /// <param name="stop">The last bound of the domain.</param>
    /// <param name="count">The approximate number of ticks.</param>
    /// <param name="specifier">The specifier, <c>,f</c> when null; a precision it gives is kept.</param>
    /// <param name="locale">The locale; d3's default en-US when null.</param>
    /// <returns>The format.</returns>
    /// <exception cref="FormatException">The specifier is not valid.</exception>
    public static NumberFormat Create(double start, double stop, double count, string? specifier = null, NumberLocale? locale = null)
    {
        locale ??= NumberLocale.EnUs;
        var step = Ticks.Step(start, stop, count);
        var parsed = specifier is null ? DefaultSpecifier : FormatSpecifier.Parse(specifier);
        var max = Math.Max(Math.Abs(start), Math.Abs(stop));
        var suggested = parsed.Type switch
        {
            "s" => FormatPrecision.Prefix(step, max),
            "" or "g" or "p" or "r" => FormatPrecision.Round(step, max),
            "e" => FormatPrecision.Round(step, max) - 1,
            "f" => FormatPrecision.Fixed(step),
            "%" => FormatPrecision.Fixed(step) - 2,
            _ => double.NaN,
        };
        if (parsed.Precision is null && !double.IsNaN(suggested))
        {
            parsed = parsed with { Precision = (int)suggested };
        }

        return parsed.Type == "s" ? NumberFormat.CreatePrefix(parsed, max, locale) : NumberFormat.Create(parsed, locale);
    }
}
