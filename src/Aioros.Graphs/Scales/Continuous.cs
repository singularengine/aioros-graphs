namespace Aioros.Graphs.Scales;

internal readonly struct Continuous
{
    private readonly ScaleTransform transform;
    private readonly Piecewise output;
    private readonly Piecewise input;
    private readonly double clampLow;
    private readonly double clampHigh;

    public Continuous(double[] domain, double[] range, ScaleTransform transform, bool clamp, bool round)
    {
        Domain = domain;
        Range = range;
        this.transform = transform;
        IsClamped = clamp;
        IsRounded = round;
        var transformed = new double[domain.Length];
        for (var i = 0; i < domain.Length; i++)
        {
            transformed[i] = transform.Apply(domain[i]);
        }

        output = new Piecewise(transformed, range, round);
        input = new Piecewise(range, transformed, false);
        var last = domain[Math.Min(domain.Length, range.Length) - 1];
        (clampLow, clampHigh) = domain[0] > last ? (last, domain[0]) : (domain[0], last);
    }

    public double[] Domain { get; }

    public double[] Range { get; }

    public bool IsClamped { get; }

    public bool IsRounded { get; }

    public static double[] Stops(ReadOnlySpan<double> stops, string name)
    {
        if (stops.Length < 2)
        {
            throw new ArgumentException("A continuous scale needs at least two stops.", name);
        }

        return stops.ToArray();
    }

    public double Map(double x)
    {
        if (double.IsNaN(x))
        {
            return double.NaN;
        }

        return output.Map(transform.Apply(IsClamped ? Math.Max(clampLow, Math.Min(clampHigh, x)) : x));
    }

    public void Map(ReadOnlySpan<double> values, Span<double> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            destination[i] = Map(values[i]);
        }
    }

    public double Invert(double y)
    {
        var x = transform.Invert(input.Map(y));
        return IsClamped ? Math.Max(clampLow, Math.Min(clampHigh, x)) : x;
    }
}
