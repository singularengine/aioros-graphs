namespace Aioros.Graphs.Arrays;

/// <summary>The minimum and maximum of values, as d3-array's <c>extent</c>: NaN values are ignored.</summary>
public static class Extent
{
    /// <summary>Returns the minimum and maximum of <paramref name="values"/>.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The bounds, or (NaN, NaN) when no value is a number.</returns>
    public static (double Min, double Max) Of(ReadOnlySpan<double> values)
    {
        var min = double.NaN;
        var max = double.NaN;
        var i = 0;
        for (; i < values.Length; i++)
        {
            if (!double.IsNaN(values[i]))
            {
                min = max = values[i];
                break;
            }
        }

        for (i++; i < values.Length; i++)
        {
            var value = values[i];
            if (min > value)
            {
                min = value;
            }

            if (max < value)
            {
                max = value;
            }
        }

        return (min, max);
    }
}
