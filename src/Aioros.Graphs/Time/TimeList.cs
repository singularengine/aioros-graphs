using System.Buffers;

namespace Aioros.Graphs.Time;

internal ref struct TimeList
{
    private Span<double> span;
    private double[]? rented;

    public TimeList(Span<double> initial)
    {
        span = initial;
    }

    public int Count { get; private set; }

    public void Add(double value)
    {
        if (Count == span.Length)
        {
            Grow();
        }

        span[Count++] = value;
    }

    public readonly double[] ToArray(bool reverse)
    {
        var values = span[..Count].ToArray();
        if (reverse)
        {
            Array.Reverse(values);
        }

        return values;
    }

    public void Dispose()
    {
        if (rented is not null)
        {
            ArrayPool<double>.Shared.Return(rented);
            rented = null;
        }
    }

    private void Grow()
    {
        var larger = ArrayPool<double>.Shared.Rent(Math.Max(16, span.Length * 2));
        span[..Count].CopyTo(larger);
        Dispose();
        rented = larger;
        span = larger;
    }
}
