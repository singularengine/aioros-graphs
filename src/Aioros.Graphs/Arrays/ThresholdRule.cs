namespace Aioros.Graphs.Arrays;

/// <summary>
/// How <see cref="Histogram"/> chooses its thresholds, as the argument of d3's <c>bin().thresholds</c>: a rule that
/// counts uniform bins (<c>thresholdSturges</c>, <c>thresholdScott</c>, <c>thresholdFreedmanDiaconis</c>), a fixed
/// count, or explicit threshold values.
/// </summary>
public readonly struct ThresholdRule
{
    private readonly RuleKind kind;
    private readonly double count;
    private readonly double[]? values;

    private ThresholdRule(RuleKind kind, double count, double[]? values)
    {
        this.kind = kind;
        this.count = count;
        this.values = values;
    }

    private enum RuleKind : byte
    {
        Sturges,
        Scott,
        FreedmanDiaconis,
        Uniform,
        Values,
    }

    /// <summary>Gets Sturges' rule, d3's default: <c>ceil(log2(n)) + 1</c> bins.</summary>
    public static ThresholdRule Sturges => default;

    /// <summary>Gets Scott's normal reference rule: bins of width <c>3.49 σ n^(-1/3)</c>.</summary>
    public static ThresholdRule Scott => new(RuleKind.Scott, 0, null);

    /// <summary>Gets the Freedman–Diaconis rule: bins of width <c>2 IQR n^(-1/3)</c>.</summary>
    public static ThresholdRule FreedmanDiaconis => new(RuleKind.FreedmanDiaconis, 0, null);

    /// <summary>Gets a value indicating whether the rule holds explicit threshold values.</summary>
    public bool IsExplicit => kind == RuleKind.Values;

    /// <summary>Returns a rule asking for about <paramref name="count"/> uniform bins on nice boundaries.</summary>
    /// <param name="count">The approximate number of bins.</param>
    /// <returns>The rule.</returns>
    public static ThresholdRule Uniform(double count) => new(RuleKind.Uniform, count, null);

    /// <summary>Returns a rule with explicit thresholds: each starts a bin.</summary>
    /// <param name="thresholds">The ascending thresholds; they are copied.</param>
    /// <returns>The rule.</returns>
    public static ThresholdRule At(ReadOnlySpan<double> thresholds) => new(RuleKind.Values, 0, thresholds.ToArray());

    /// <summary>Returns the number of uniform bins the rule asks for, as d3's threshold generators return it.</summary>
    /// <param name="values">The values to bin.</param>
    /// <param name="min">The domain's lower bound.</param>
    /// <param name="max">The domain's upper bound.</param>
    /// <returns>The count, or NaN for explicit thresholds.</returns>
    public double Count(ReadOnlySpan<double> values, double min, double max)
    {
        switch (kind)
        {
            case RuleKind.Uniform:
                return count;
            case RuleKind.Values:
                return double.NaN;
            case RuleKind.Scott:
            {
                var c = Statistics.Count(values);
                var d = Statistics.Deviation(values);
                return c != 0 && d != 0 && !double.IsNaN(d) ? Math.Ceiling((max - min) * Math.Cbrt(c) / (3.49 * d)) : 1;
            }

            case RuleKind.FreedmanDiaconis:
            {
                var c = Statistics.Count(values);
                var d = Quantiles.Of(values, 0.75) - Quantiles.Of(values, 0.25);
                return c != 0 && d != 0 && !double.IsNaN(d) ? Math.Ceiling((max - min) / (2 * d * Math.Pow(c, -1.0 / 3))) : 1;
            }

            default:
                return Math.Max(1, Math.Ceiling(Math.Log(Statistics.Count(values)) / Math.Log(2)) + 1);
        }
    }

    internal double[] Thresholds() => values ?? [];
}
