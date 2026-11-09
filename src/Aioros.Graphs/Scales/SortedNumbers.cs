namespace Aioros.Graphs.Scales;

internal static class SortedNumbers
{
    public static double[] From(ReadOnlySpan<double> values)
    {
        var count = 0;
        foreach (var value in values)
        {
            if (!double.IsNaN(value))
            {
                count++;
            }
        }

        var sorted = new double[count];
        var zeros = new List<double>();
        count = 0;
        foreach (var value in values)
        {
            if (!double.IsNaN(value))
            {
                sorted[count++] = value;
                if (value == 0)
                {
                    zeros.Add(value);
                }
            }
        }

        Array.Sort(sorted);
        var first = Array.IndexOf(sorted, 0.0);
        for (var i = 0; i < zeros.Count; i++)
        {
            sorted[first + i] = zeros[i];
        }

        return sorted;
    }
}
