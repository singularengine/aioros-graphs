using System.Buffers;

namespace Aioros.Graphs.Arrays;

/// <summary>Quantiles of values, as d3-array's <c>quantile</c>, <c>quantileSorted</c>, <c>quantileIndex</c> and <c>median</c>.</summary>
public static class Quantiles
{
    /// <summary>Returns the <paramref name="p"/>-quantile of <paramref name="values"/>, NaN values ignored, as d3's <c>quantile</c>.</summary>
    /// <param name="values">The values, in any order.</param>
    /// <param name="p">The quantile, from 0 to 1.</param>
    /// <returns>The interpolated quantile, or NaN when no value is a number or <paramref name="p"/> is NaN.</returns>
    public static double Of(ReadOnlySpan<double> values, double p)
    {
        var buffer = ArrayPool<double>.Shared.Rent(Math.Max(1, values.Length));
        try
        {
            var n = 0;
            foreach (var value in values)
            {
                if (!double.IsNaN(value))
                {
                    buffer[n++] = value;
                }
            }

            return OfNumbers(buffer.AsSpan(0, n), p);
        }
        finally
        {
            ArrayPool<double>.Shared.Return(buffer);
        }
    }

    /// <summary>Returns the <paramref name="p"/>-quantile of values already sorted in ascending order, as d3's <c>quantileSorted</c>.</summary>
    /// <param name="sorted">The ascending values.</param>
    /// <param name="p">The quantile, from 0 to 1.</param>
    /// <returns>The interpolated quantile, or NaN when there are no values or <paramref name="p"/> is NaN.</returns>
    public static double OfSorted(ReadOnlySpan<double> sorted, double p)
    {
        var n = sorted.Length;
        if (n == 0 || double.IsNaN(p))
        {
            return double.NaN;
        }

        if (p <= 0 || n < 2)
        {
            return sorted[0];
        }

        if (p >= 1)
        {
            return sorted[n - 1];
        }

        var i = (n - 1) * p;
        var i0 = (int)Math.Floor(i);
        var value0 = sorted[i0];
        return value0 + (sorted[i0 + 1] - value0) * (i - i0);
    }

    /// <summary>Returns the index of the value at the <paramref name="p"/>-quantile, as d3's <c>quantileIndex</c>.</summary>
    /// <param name="values">The values, in any order; NaN values are never chosen.</param>
    /// <param name="p">The quantile, from 0 to 1.</param>
    /// <returns>The index, or -1 when there is none or <paramref name="p"/> is NaN.</returns>
    public static int IndexOf(ReadOnlySpan<double> values, double p)
    {
        if (double.IsNaN(p))
        {
            return -1;
        }

        if (p <= 0)
        {
            return Statistics.MinIndex(values);
        }

        if (p >= 1)
        {
            return Statistics.MaxIndex(values);
        }

        var numbers = values.ToArray();
        var index = ArrayPool<int>.Shared.Rent(Math.Max(1, numbers.Length));
        try
        {
            for (var k = 0; k < numbers.Length; k++)
            {
                index[k] = k;
            }

            var j = numbers.Length - 1;
            var i = (int)Math.Floor(j * p);
            QuickSelect.Select(index.AsSpan(0, numbers.Length), i, 0, j, new AscendingByValue(numbers));
            return Greatest(index.AsSpan(0, Math.Max(0, i + 1)), numbers);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(index);
        }
    }

    internal static double OfNumbers(Span<double> numbers, double p)
    {
        var n = numbers.Length;
        if (n == 0 || double.IsNaN(p))
        {
            return double.NaN;
        }

        if (p <= 0 || n < 2)
        {
            return Statistics.Min(numbers);
        }

        if (p >= 1)
        {
            return Statistics.Max(numbers);
        }

        var i = (n - 1) * p;
        var i0 = (int)Math.Floor(i);
        QuickSelect.Select(numbers, i0, 0, n - 1, default(AscendingDefined));
        var value0 = Statistics.Max(numbers[..(i0 + 1)]);
        var value1 = Statistics.Min(numbers[(i0 + 1)..]);
        return value0 + (value1 - value0) * (i - i0);
    }

    private static int Greatest(ReadOnlySpan<int> index, double[] numbers)
    {
        var found = -1;
        foreach (var i in index)
        {
            var value = numbers[i];
            if (found < 0 ? !double.IsNaN(value) : value > numbers[found])
            {
                found = i;
            }
        }

        return found;
    }
}
