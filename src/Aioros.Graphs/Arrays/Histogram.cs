namespace Aioros.Graphs.Arrays;

/// <summary>One bin of a histogram: the values from <paramref name="X0"/> inclusive to <paramref name="X1"/> exclusive (inclusive for the last bin).</summary>
/// <param name="X0">The lower bound.</param>
/// <param name="X1">The upper bound.</param>
/// <param name="Count">The number of values in the bin.</param>
public readonly record struct Bin(double X0, double X1, int Count);

/// <summary>Histograms, as d3-array's <c>bin</c>: values grouped into bins between thresholds.</summary>
public static class Histogram
{
    /// <summary>Bins <paramref name="values"/>.</summary>
    /// <param name="values">The values; NaN values and values outside the domain are skipped.</param>
    /// <param name="thresholds">The rule choosing the thresholds; Sturges' rule is d3's default.</param>
    /// <param name="domain">The domain, or null for the values' extent niced to the tick step, as d3's default.</param>
    /// <returns>The bins.</returns>
    public static Bin[] Compute(ReadOnlySpan<double> values, ThresholdRule thresholds = default, (double Min, double Max)? domain = null) =>
        Bins(values, [], thresholds, domain);

    /// <summary>Bins <paramref name="values"/> and writes the bin of each value.</summary>
    /// <param name="values">The values; NaN values and values outside the domain are skipped.</param>
    /// <param name="binOfValue">At least as many entries as values: the index of each value's bin, or -1.</param>
    /// <param name="thresholds">The rule choosing the thresholds.</param>
    /// <param name="domain">The domain, or null for the values' extent niced to the tick step.</param>
    /// <returns>The bins.</returns>
    /// <exception cref="ArgumentException"><paramref name="binOfValue"/> is shorter than the values.</exception>
    public static Bin[] Compute(ReadOnlySpan<double> values, Span<int> binOfValue, ThresholdRule thresholds = default, (double Min, double Max)? domain = null)
    {
        if (binOfValue.Length < values.Length)
        {
            throw new ArgumentException("The span of bin indices is shorter than the values.", nameof(binOfValue));
        }

        return Bins(values, binOfValue, thresholds, domain);
    }

    private static Bin[] Bins(ReadOnlySpan<double> values, Span<int> binOfValue, ThresholdRule rule, (double Min, double Max)? domain)
    {
        var (x0, x1) = domain ?? Extent.Of(values);
        var step = double.NaN;
        double[] tz;
        if (rule.IsExplicit)
        {
            tz = rule.Thresholds();
        }
        else
        {
            (tz, x0, x1, step) = Uniform(values, rule, x0, x1, domain is null);
        }

        var edges = Within(tz, x0, x1);
        var bins = new Bin[edges.Length + 1];
        var counts = new int[bins.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var bin = Place(values[i], edges, x0, x1, step);
            if (bin >= 0)
            {
                counts[bin]++;
            }

            if (binOfValue.Length > 0)
            {
                binOfValue[i] = bin;
            }
        }

        for (var i = 0; i < bins.Length; i++)
        {
            bins[i] = new Bin(i > 0 ? edges[i - 1] : x0, i < edges.Length ? edges[i] : x1, counts[i]);
        }

        return bins;
    }

    private static ReadOnlySpan<double> Within(double[] tz, double x0, double x1)
    {
        var a = 0;
        var b = tz.Length;
        while (a < tz.Length && tz[a] <= x0)
        {
            ++a;
        }

        while (b > 0 && tz[b - 1] > x1)
        {
            --b;
        }

        return tz.AsSpan(a, Math.Max(0, b - a));
    }

    private static (double[] Thresholds, double X0, double X1, double Step) Uniform(ReadOnlySpan<double> values, ThresholdRule rule, double x0, double x1, bool niced)
    {
        var tn = rule.Count(values, x0, x1);
        var max = x1;
        if (niced)
        {
            (x0, x1) = Ticks.Nice(x0, x1, tn);
        }

        var tz = Ticks.Compute(x0, x1, tn);
        var step = tz.Length > 0 && tz[0] <= x0 ? Ticks.Increment(x0, x1, tn) : double.NaN;
        if (tz.Length > 0 && tz[^1] >= x1)
        {
            if (max >= x1 && niced)
            {
                var extension = Ticks.Increment(x0, x1, tn);
                if (extension > 0 && double.IsFinite(extension))
                {
                    x1 = (Math.Floor(x1 / extension) + 1) * extension;
                }
                else if (extension < 0 && double.IsFinite(extension))
                {
                    x1 = (Math.Ceiling(x1 * -extension) + 1) / -extension;
                }
            }
            else
            {
                tz = tz[..^1];
            }
        }

        return (tz, x0, x1, step);
    }

    private static int Place(double x, ReadOnlySpan<double> edges, double x0, double x1, double step)
    {
        if (!(x0 <= x && x <= x1))
        {
            return -1;
        }

        var m = edges.Length;
        if (!double.IsFinite(step))
        {
            return Bisect.Right(edges, x, 0, m);
        }

        if (step > 0)
        {
            return (int)Math.Min(m, Math.Floor((x - x0) / step));
        }

        if (step < 0)
        {
            var j = (int)Math.Floor((x0 - x) * step);
            return Math.Min(m, j + (j < m && edges[j] <= x ? 1 : 0));
        }

        return -1;
    }
}
