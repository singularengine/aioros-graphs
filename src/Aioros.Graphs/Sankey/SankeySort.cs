namespace Aioros.Graphs.Sankey;

internal interface ISankeyComparer
{
    double Compare(int a, int b);
}

internal static class SankeySort
{
    public static void Sort<TComparer>(Span<int> items, TComparer comparer)
        where TComparer : struct, ISankeyComparer
    {
        if (items.Length < 2)
        {
            return;
        }

        for (var start = InitialRun(items, comparer); start < items.Length; start++)
        {
            var pivot = items[start];
            var left = 0;
            var right = start;
            while (left < right)
            {
                var middle = left + ((right - left) >> 1);
                if (comparer.Compare(pivot, items[middle]) < 0)
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

    private static int InitialRun<TComparer>(Span<int> items, TComparer comparer)
        where TComparer : struct, ISankeyComparer
    {
        var run = 2;
        if (comparer.Compare(items[1], items[0]) < 0)
        {
            while (run < items.Length && comparer.Compare(items[run], items[run - 1]) < 0)
            {
                run++;
            }

            items[..run].Reverse();
            return run;
        }

        while (run < items.Length && comparer.Compare(items[run], items[run - 1]) >= 0)
        {
            run++;
        }

        return run;
    }
}

internal readonly struct BreadthComparer(double[] y0) : ISankeyComparer
{
    public double Compare(int a, int b) => y0[a] - y0[b];
}

internal readonly struct LinkBreadthComparer(double[] y0, int[] ends) : ISankeyComparer
{
    public double Compare(int a, int b)
    {
        var d = y0[ends[a]] - y0[ends[b]];
        return d != 0 && !double.IsNaN(d) ? d : a - b;
    }
}

internal readonly struct CustomComparer(SankeyComparison comparison, SankeyLayout layout) : ISankeyComparer
{
    public double Compare(int a, int b) => comparison(layout, a, b);
}
