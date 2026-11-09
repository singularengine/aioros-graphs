namespace Aioros.Graphs.Arrays;

/// <summary>
/// Summary statistics of values, as d3-array's <c>sum</c>, <c>mean</c>, <c>median</c>, <c>min</c>, <c>max</c>,
/// <c>minIndex</c>, <c>maxIndex</c>, <c>variance</c>, <c>deviation</c>, <c>fsum</c> and <c>cumsum</c>: NaN values are
/// ignored, and a result d3 leaves undefined is NaN (or -1 for an index).
/// </summary>
public static class Statistics
{
    /// <summary>Returns the sum of the values, as d3's <c>sum</c>.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The sum, 0 when there are no numbers.</returns>
    public static double Sum(ReadOnlySpan<double> values)
    {
        var sum = 0.0;
        foreach (var value in values)
        {
            if (value != 0 && !double.IsNaN(value))
            {
                sum += value;
            }
        }

        return sum;
    }

    /// <summary>Returns the mean of the values, as d3's <c>mean</c>.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The mean, or NaN when there are no numbers.</returns>
    public static double Mean(ReadOnlySpan<double> values)
    {
        var count = 0;
        var sum = 0.0;
        foreach (var value in values)
        {
            if (!double.IsNaN(value))
            {
                ++count;
                sum += value;
            }
        }

        return count > 0 ? sum / count : double.NaN;
    }

    /// <summary>Returns the median of the values, as d3's <c>median</c>.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The median, or NaN when there are no numbers.</returns>
    public static double Median(ReadOnlySpan<double> values) => Quantiles.Of(values, 0.5);

    /// <summary>Returns the smallest value, the first of equal ones, as d3's <c>min</c>.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The minimum, or NaN when there are no numbers.</returns>
    public static double Min(ReadOnlySpan<double> values)
    {
        var i = MinIndex(values);
        return i < 0 ? double.NaN : values[i];
    }

    /// <summary>Returns the largest value, the first of equal ones, as d3's <c>max</c>.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The maximum, or NaN when there are no numbers.</returns>
    public static double Max(ReadOnlySpan<double> values)
    {
        var i = MaxIndex(values);
        return i < 0 ? double.NaN : values[i];
    }

    /// <summary>Returns the index of the smallest value, as d3's <c>minIndex</c>.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The index, or -1 when there are no numbers.</returns>
    public static int MinIndex(ReadOnlySpan<double> values)
    {
        var index = -1;
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (index < 0 ? !double.IsNaN(value) : values[index] > value)
            {
                index = i;
            }
        }

        return index;
    }

    /// <summary>Returns the index of the largest value, as d3's <c>maxIndex</c>.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The index, or -1 when there are no numbers.</returns>
    public static int MaxIndex(ReadOnlySpan<double> values)
    {
        var index = -1;
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (index < 0 ? !double.IsNaN(value) : values[index] < value)
            {
                index = i;
            }
        }

        return index;
    }

    /// <summary>Returns the unbiased sample variance with Welford's algorithm, as d3's <c>variance</c>.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The variance, or NaN with fewer than two numbers.</returns>
    public static double Variance(ReadOnlySpan<double> values)
    {
        var count = 0;
        var mean = 0.0;
        var sum = 0.0;
        foreach (var value in values)
        {
            if (!double.IsNaN(value))
            {
                var delta = value - mean;
                mean += delta / ++count;
                sum += delta * (value - mean);
            }
        }

        return count > 1 ? sum / (count - 1) : double.NaN;
    }

    /// <summary>Returns the standard deviation, the square root of <see cref="Variance"/>, as d3's <c>deviation</c>.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The deviation, or NaN with fewer than two numbers.</returns>
    public static double Deviation(ReadOnlySpan<double> values)
    {
        var variance = Variance(values);
        return variance != 0 && !double.IsNaN(variance) ? Math.Sqrt(variance) : variance;
    }

    /// <summary>Returns the full-precision sum of the values (Python's <c>fsum</c>), as d3's <c>fsum</c>.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The sum.</returns>
    public static double FSum(ReadOnlySpan<double> values)
    {
        Span<double> partials = stackalloc double[33];
        var n = 0;
        foreach (var value in values)
        {
            if (value != 0 && !double.IsNaN(value))
            {
                n = Add(partials, n, value);
            }
        }

        return Total(partials, n);
    }

    /// <summary>Writes the running sums of the values, NaN counted as zero, as d3's <c>cumsum</c>.</summary>
    /// <param name="values">The values.</param>
    /// <param name="destination">At least as many values as <paramref name="values"/>.</param>
    /// <exception cref="ArgumentException">The destination is shorter than the values.</exception>
    public static void CumulativeSum(ReadOnlySpan<double> values, Span<double> destination)
    {
        if (destination.Length < values.Length)
        {
            throw new ArgumentException("The destination is shorter than the values.", nameof(destination));
        }

        var sum = 0.0;
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            destination[i] = sum += double.IsNaN(value) ? 0 : value;
        }
    }

    /// <summary>Writes the running full-precision sums of the values, NaN counted as zero, as d3's <c>fcumsum</c>.</summary>
    /// <param name="values">The values.</param>
    /// <param name="destination">At least as many values as <paramref name="values"/>.</param>
    /// <exception cref="ArgumentException">The destination is shorter than the values.</exception>
    public static void FCumulativeSum(ReadOnlySpan<double> values, Span<double> destination)
    {
        if (destination.Length < values.Length)
        {
            throw new ArgumentException("The destination is shorter than the values.", nameof(destination));
        }

        Span<double> partials = stackalloc double[33];
        var n = 0;
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            n = Add(partials, n, double.IsNaN(value) ? 0 : value);
            destination[i] = Total(partials, n);
        }
    }

    internal static int Count(ReadOnlySpan<double> values)
    {
        var count = 0;
        foreach (var value in values)
        {
            if (!double.IsNaN(value))
            {
                ++count;
            }
        }

        return count;
    }

    internal static int Add(Span<double> partials, int n, double x)
    {
        var i = 0;
        for (var j = 0; j < n && j < 32; j++)
        {
            var y = partials[j];
            var hi = x + y;
            var lo = Math.Abs(x) < Math.Abs(y) ? x - (hi - y) : y - (hi - x);
            if (lo != 0 && !double.IsNaN(lo))
            {
                partials[i++] = lo;
            }

            x = hi;
        }

        partials[i] = x;
        return i + 1;
    }

    internal static double Total(ReadOnlySpan<double> partials, int n)
    {
        var hi = 0.0;
        if (n <= 0)
        {
            return hi;
        }

        double lo = 0;
        hi = partials[--n];
        while (n > 0)
        {
            var x = hi;
            var y = partials[--n];
            hi = x + y;
            lo = y - (hi - x);
            if (lo != 0 && !double.IsNaN(lo))
            {
                break;
            }
        }

        if (n > 0 && ((lo < 0 && partials[n - 1] < 0) || (lo > 0 && partials[n - 1] > 0)))
        {
            var y = lo * 2;
            var x = hi + y;
            if (y == x - hi)
            {
                hi = x;
            }
        }

        return hi;
    }
}
