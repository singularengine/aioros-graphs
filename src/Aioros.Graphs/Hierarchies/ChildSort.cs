using System.Buffers;

namespace Aioros.Graphs.Hierarchies;

internal static class ChildSort
{
    private const int RunThreshold = 8;
    private const int MergeThreshold = 64;

    public static void Sort(Span<int> items, Func<int, int, double> compare)
    {
        if (items.Length < MergeThreshold)
        {
            InsertionSort(items, compare);
            return;
        }

        var scratch = ArrayPool<int>.Shared.Rent(items.Length);
        MergeSort(items, scratch.AsSpan(0, items.Length), compare);
        ArrayPool<int>.Shared.Return(scratch);
    }

    private static void InsertionSort(Span<int> items, Func<int, int, double> compare)
    {
        var run = items.Length < RunThreshold ? 1 : Run(items, compare);
        for (var start = run; start < items.Length; start++)
        {
            var pivot = items[start];
            var left = 0;
            var right = start;
            while (left < right)
            {
                var middle = left + ((right - left) >> 1);
                if (compare(pivot, items[middle]) < 0)
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

    private static int Run(Span<int> items, Func<int, int, double> compare)
    {
        var run = 2;
        var descending = compare(items[1], items[0]) < 0;
        for (var i = 2; i < items.Length; i++)
        {
            var order = compare(items[i], items[i - 1]);
            if (descending ? order >= 0 || double.IsNaN(order) : order < 0)
            {
                break;
            }

            run++;
        }

        if (descending)
        {
            items[..run].Reverse();
        }

        return run;
    }

    private static void MergeSort(Span<int> items, Span<int> scratch, Func<int, int, double> compare)
    {
        if (items.Length < 2)
        {
            return;
        }

        var middle = items.Length >> 1;
        MergeSort(items[..middle], scratch[..middle], compare);
        MergeSort(items[middle..], scratch[middle..], compare);
        items.CopyTo(scratch);
        var left = 0;
        var right = middle;
        var k = 0;
        while (left < middle && right < items.Length)
        {
            items[k++] = compare(scratch[right], scratch[left]) < 0 ? scratch[right++] : scratch[left++];
        }

        scratch[left..middle].CopyTo(items[k..]);
        k += middle - left;
        scratch[right..items.Length].CopyTo(items[k..]);
    }
}
