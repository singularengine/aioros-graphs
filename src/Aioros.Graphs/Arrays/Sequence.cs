namespace Aioros.Graphs.Arrays;

/// <summary>Arithmetic sequences, as d3-array's <c>range</c>.</summary>
public static class Sequence
{
    /// <summary>Returns <c>start + i × step</c> for each i while below <c>ceil((stop - start) / step)</c>, as d3's <c>range(start, stop, step)</c>.</summary>
    /// <param name="start">The first value.</param>
    /// <param name="stop">The bound the values stay before; excluded.</param>
    /// <param name="step">The difference between values; negative for a descending sequence.</param>
    /// <returns>The values, empty when the step points away from the stop, is zero or NaN.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The sequence would hold more than <see cref="int.MaxValue"/> values.</exception>
    public static double[] Range(double start, double stop, double step)
    {
        var count = Math.Ceiling((stop - start) / step);
        if (!(count > 0) || double.IsInfinity(count))
        {
            return [];
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, int.MaxValue);
        var values = new double[(int)count];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = start + i * step;
        }

        return values;
    }
}
