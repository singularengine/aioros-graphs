namespace Aioros.Graphs.Arrays;

/// <summary>Binary search in ascending values, as d3-array's <c>bisectLeft</c>, <c>bisectRight</c> and <c>bisectCenter</c>.</summary>
public static class Bisect
{
    /// <summary>Returns the first index at which <paramref name="x"/> could be inserted keeping the order, before equal values.</summary>
    /// <param name="values">The ascending values.</param>
    /// <param name="x">The value to place.</param>
    /// <returns>The index, or the length when <paramref name="x"/> is NaN.</returns>
    public static int Left(ReadOnlySpan<double> values, double x) => Left(values, x, 0, values.Length);

    /// <summary>Returns the first index in [<paramref name="lo"/>, <paramref name="hi"/>] at which <paramref name="x"/> could be inserted, before equal values.</summary>
    /// <param name="values">The ascending values.</param>
    /// <param name="x">The value to place.</param>
    /// <param name="lo">The lowest index considered.</param>
    /// <param name="hi">The index past the last one considered.</param>
    /// <returns>The index, or <paramref name="hi"/> when <paramref name="x"/> is NaN.</returns>
    public static int Left(ReadOnlySpan<double> values, double x, int lo, int hi)
    {
        if (lo < hi && double.IsNaN(x))
        {
            return hi;
        }

        while (lo < hi)
        {
            var mid = (int)((uint)(lo + hi) >> 1);
            if (values[mid] < x)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    /// <summary>Returns the last index at which <paramref name="x"/> could be inserted keeping the order, after equal values.</summary>
    /// <param name="values">The ascending values.</param>
    /// <param name="x">The value to place.</param>
    /// <returns>The index, or the length when <paramref name="x"/> is NaN.</returns>
    public static int Right(ReadOnlySpan<double> values, double x) => Right(values, x, 0, values.Length);

    /// <summary>Returns the last index in [<paramref name="lo"/>, <paramref name="hi"/>] at which <paramref name="x"/> could be inserted, after equal values.</summary>
    /// <param name="values">The ascending values.</param>
    /// <param name="x">The value to place.</param>
    /// <param name="lo">The lowest index considered.</param>
    /// <param name="hi">The index past the last one considered.</param>
    /// <returns>The index, or <paramref name="hi"/> when <paramref name="x"/> is NaN.</returns>
    public static int Right(ReadOnlySpan<double> values, double x, int lo, int hi)
    {
        if (lo < hi && double.IsNaN(x))
        {
            return hi;
        }

        while (lo < hi)
        {
            var mid = (int)((uint)(lo + hi) >> 1);
            if (values[mid] <= x)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    /// <summary>Returns the index of the value closest to <paramref name="x"/>, the lower one on ties.</summary>
    /// <param name="values">The ascending values.</param>
    /// <param name="x">The value to look for.</param>
    /// <returns>The index.</returns>
    public static int Center(ReadOnlySpan<double> values, double x)
    {
        var i = Left(values, x, 0, values.Length - 1);
        return i > 0 && values[i - 1] - x > -(values[i] - x) ? i - 1 : i;
    }
}
