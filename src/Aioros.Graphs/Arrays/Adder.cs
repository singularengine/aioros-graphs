namespace Aioros.Graphs.Arrays;

/// <summary>
/// A full-precision running sum, as d3-array's <c>Adder</c> (Shewchuk's partials, after Python's <c>fsum</c>): add
/// numbers one by one and read the exactly rounded total at any time.
/// </summary>
public sealed class Adder
{
    private readonly double[] partials = new double[33];
    private int count;

    /// <summary>Gets the sum of the numbers added so far, as d3's <c>adder.valueOf()</c>.</summary>
    public double Value => Statistics.Total(partials, count);

    /// <summary>Adds <paramref name="value"/>, as d3's <c>adder.add(value)</c>.</summary>
    /// <param name="value">The number to add.</param>
    /// <returns>This adder.</returns>
    public Adder Add(double value)
    {
        count = Statistics.Add(partials, count, value);
        return this;
    }
}
