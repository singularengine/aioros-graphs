namespace Aioros.Graphs.Chords;

internal static class ChordOrder
{
    public static void Sort(Span<int> items, ReadOnlySpan<double> keys, ChordSort sort)
    {
        if (sort == ChordSort.None || items.Length < 2)
        {
            return;
        }

        var descending = sort == ChordSort.Descending;
        var run = InitialRun(items, keys, descending);
        for (var start = run; start < items.Length; start++)
        {
            var pivot = items[start];
            var left = 0;
            var right = start;
            while (left < right)
            {
                var middle = left + ((right - left) >> 1);
                if (Compare(keys[pivot], keys[items[middle]], descending) < 0)
                {
                    right = middle;
                }
                else
                {
                    left = middle + 1;
                }
            }

            items[left..start].CopyTo(items[(left + 1)..]);
            items[left] = pivot;
        }
    }

    private static int InitialRun(Span<int> items, ReadOnlySpan<double> keys, bool descending)
    {
        var run = 2;
        if (Compare(keys[items[1]], keys[items[0]], descending) < 0)
        {
            while (run < items.Length && Compare(keys[items[run]], keys[items[run - 1]], descending) < 0)
            {
                run++;
            }

            items[..run].Reverse();
            return run;
        }

        while (run < items.Length && Compare(keys[items[run]], keys[items[run - 1]], descending) >= 0)
        {
            run++;
        }

        return run;
    }

    private static double Compare(double a, double b, bool descending)
    {
        if (descending)
        {
            (a, b) = (b, a);
        }

        return a < b ? -1 : a > b ? 1 : a >= b ? 0 : double.NaN;
    }
}
