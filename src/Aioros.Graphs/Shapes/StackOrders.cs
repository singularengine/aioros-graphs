namespace Aioros.Graphs.Shapes;

internal static class StackOrders
{
    public static void Apply(Series series, Span<int> order, StackOrder kind)
    {
        for (var i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }

        switch (kind)
        {
            case StackOrder.Reverse:
                order.Reverse();
                break;
            case StackOrder.Ascending:
                SortBy(order, Sums(series, stackalloc double[order.Length]));
                break;
            case StackOrder.Descending:
                SortBy(order, Sums(series, stackalloc double[order.Length]));
                order.Reverse();
                break;
            case StackOrder.Appearance:
                SortBy(order, Peaks(series, stackalloc double[order.Length]));
                break;
            case StackOrder.InsideOut:
                InsideOut(series, order);
                break;
            default:
                break;
        }
    }

    private static Span<double> Sums(Series series, Span<double> sums)
    {
        for (var i = 0; i < series.Count; i++)
        {
            var sum = 0.0;
            for (var j = 0; j < series.Length; j++)
            {
                var v = series[i, j].Upper;
                if (v != 0 && !double.IsNaN(v))
                {
                    sum += v;
                }
            }

            sums[i] = sum;
        }

        return sums;
    }

    private static Span<double> Peaks(Series series, Span<double> peaks)
    {
        for (var i = 0; i < series.Count; i++)
        {
            var peak = 0;
            var max = double.NegativeInfinity;
            for (var j = 0; j < series.Length; j++)
            {
                if (series[i, j].Upper > max)
                {
                    max = series[i, j].Upper;
                    peak = j;
                }
            }

            peaks[i] = peak;
        }

        return peaks;
    }

    private static void SortBy(Span<int> order, ReadOnlySpan<double> keys)
    {
        for (var i = 1; i < order.Length; i++)
        {
            var current = order[i];
            var j = i - 1;
            while (j >= 0 && keys[current] - keys[order[j]] < 0)
            {
                order[j + 1] = order[j];
                j--;
            }

            order[j + 1] = current;
        }
    }

    private static void InsideOut(Series series, Span<int> order)
    {
        var n = order.Length;
        Span<double> sums = stackalloc double[n];
        Sums(series, sums);
        SortBy(order, Peaks(series, stackalloc double[n]));
        Span<int> tops = stackalloc int[n];
        Span<int> bottoms = stackalloc int[n];
        var (top, bottom, topCount, bottomCount) = (0.0, 0.0, 0, 0);
        for (var i = 0; i < n; i++)
        {
            var j = order[i];
            if (top < bottom)
            {
                top += sums[j];
                tops[topCount++] = j;
            }
            else
            {
                bottom += sums[j];
                bottoms[bottomCount++] = j;
            }
        }

        bottoms[..bottomCount].Reverse();
        bottoms[..bottomCount].CopyTo(order);
        tops[..topCount].CopyTo(order[bottomCount..]);
    }
}
