namespace Aioros.Graphs.Formatting;

/// <summary>The precisions d3-format suggests for a step between values: <c>precisionFixed</c>, <c>precisionPrefix</c> and <c>precisionRound</c>.</summary>
public static class FormatPrecision
{
    /// <summary>Returns the fraction digits of a fixed-point format (<c>f</c>) that tell values <paramref name="step"/> apart.</summary>
    /// <param name="step">The step between values.</param>
    /// <returns>The precision, or NaN for a zero or non-finite step.</returns>
    public static double Fixed(double step) => Math.Max(0, -NumberText.Exponent(Math.Abs(step)));

    /// <summary>Returns the precision of an SI-prefix format (<see cref="NumberFormat.CreatePrefix(string, double)"/>) chosen from <paramref name="value"/>.</summary>
    /// <param name="step">The step between values.</param>
    /// <param name="value">The reference value.</param>
    /// <returns>The precision, or NaN.</returns>
    public static double Prefix(double step, double value) =>
        Math.Max(0, Math.Max(-8, Math.Min(8, Math.Floor(NumberText.Exponent(value) / 3))) * 3 - NumberText.Exponent(Math.Abs(step)));

    /// <summary>Returns the significant digits of a rounded format (<c>r</c>) that tell values <paramref name="step"/> apart up to <paramref name="max"/>.</summary>
    /// <param name="step">The step between values.</param>
    /// <param name="max">The largest value.</param>
    /// <returns>The precision, or NaN.</returns>
    public static double Round(double step, double max)
    {
        step = Math.Abs(step);
        max = Math.Abs(max) - step;
        return Math.Max(0, NumberText.Exponent(max) - NumberText.Exponent(step)) + 1;
    }
}
