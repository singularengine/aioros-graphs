using System.Buffers;

namespace Aioros.Graphs.Shapes;

/// <summary>How a pie orders its slices around the circle; the slices stay in input order either way.</summary>
public enum PieSort
{
    /// <summary>Largest value first, d3's default.</summary>
    Descending,

    /// <summary>Smallest value first.</summary>
    Ascending,

    /// <summary>In input order.</summary>
    None,
}

/// <summary>The angles of one pie slice, as d3's pie generator returns them.</summary>
/// <param name="Index">The slice's position around the circle.</param>
/// <param name="Value">The slice's value.</param>
/// <param name="StartAngle">The start angle in radians.</param>
/// <param name="EndAngle">The end angle in radians.</param>
/// <param name="PadAngle">The pad angle to draw the slice's arc with.</param>
public readonly record struct PieSlice(int Index, double Value, double StartAngle, double EndAngle, double PadAngle)
{
    /// <summary>Returns the slice as an arc with the given radii.</summary>
    /// <param name="innerRadius">The inner radius.</param>
    /// <param name="outerRadius">The outer radius.</param>
    /// <returns>The arc.</returns>
    public ArcShape ToArc(double innerRadius, double outerRadius) => new(innerRadius, outerRadius, StartAngle, EndAngle) { PadAngle = PadAngle };
}

/// <summary>The settings of a pie.</summary>
public sealed record PieOptions
{
    /// <summary>How slices are ordered around the circle.</summary>
    public PieSort Sort { get; init; }

    /// <summary>The angle of the first slice's start.</summary>
    public double StartAngle { get; init; }

    /// <summary>The angle of the last slice's end; the span is clamped to one turn either way.</summary>
    public double EndAngle { get; init; } = 2 * Math.PI;

    /// <summary>The angle between adjacent slices.</summary>
    public double PadAngle { get; init; }
}

/// <summary>The pie generator of d3-shape: the angles of the slices of a pie or donut chart.</summary>
public static class Pie
{
    private const int StackLimit = 256;

    /// <summary>Returns the slices for <paramref name="values"/>, in input order.</summary>
    /// <param name="values">The values; those not above zero get empty slices.</param>
    /// <param name="options">The settings.</param>
    /// <returns>The slices.</returns>
    public static PieSlice[] Compute(ReadOnlySpan<double> values, PieOptions options)
    {
        var slices = new PieSlice[values.Length];
        Compute(values, slices, options);
        return slices;
    }

    /// <summary>Writes the slices for <paramref name="values"/> in input order, without allocating for up to 256 values.</summary>
    /// <param name="values">The values; those not above zero get empty slices.</param>
    /// <param name="slices">At least as many slices as values.</param>
    /// <param name="options">The settings.</param>
    public static void Compute(ReadOnlySpan<double> values, Span<PieSlice> slices, PieOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var n = values.Length;
        if (slices.Length < n)
        {
            throw new ArgumentException("There must be a slice per value.", nameof(slices));
        }

        var rented = n > StackLimit ? ArrayPool<int>.Shared.Rent(n) : null;
        var index = rented is null ? stackalloc int[n] : rented.AsSpan(0, n);
        var sum = 0.0;
        for (var i = 0; i < n; i++)
        {
            index[i] = i;
            if (values[i] > 0)
            {
                sum += values[i];
            }
        }

        Sort(values, index, options.Sort);
        var a0 = options.StartAngle;
        var da = Math.Min(Arc.Tau, Math.Max(-Arc.Tau, options.EndAngle - a0));
        var p = Math.Min(Math.Abs(da) / n, options.PadAngle);
        var pa = p * (da < 0 ? -1 : 1);
        var k = sum != 0 ? (da - n * pa) / sum : 0;
        for (var i = 0; i < n; i++)
        {
            var j = index[i];
            var v = values[j];
            var a1 = a0 + (v > 0 ? v * k : 0) + pa;
            slices[j] = new PieSlice(i, v, a0, a1, p);
            a0 = a1;
        }

        if (rented is not null)
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    private static void Sort(ReadOnlySpan<double> values, Span<int> index, PieSort sort)
    {
        if (sort == PieSort.None || index.Length < 2)
        {
            return;
        }

        var descending = sort == PieSort.Descending;
        var run = InitialRun(values, index, descending);
        for (var start = run; start < index.Length; start++)
        {
            var pivot = index[start];
            var left = 0;
            var right = start;
            while (left < right)
            {
                var middle = left + ((right - left) >> 1);
                if (Compare(values[pivot], values[index[middle]], descending) < 0)
                {
                    right = middle;
                }
                else
                {
                    left = middle + 1;
                }
            }

            index[left..start].CopyTo(index[(left + 1)..]);
            index[left] = pivot;
        }
    }

    private static int InitialRun(ReadOnlySpan<double> values, Span<int> index, bool descending)
    {
        var run = 2;
        if (Compare(values[index[1]], values[index[0]], descending) < 0)
        {
            while (run < index.Length && Compare(values[index[run]], values[index[run - 1]], descending) < 0)
            {
                run++;
            }

            index[..run].Reverse();
            return run;
        }

        while (run < index.Length && Compare(values[index[run]], values[index[run - 1]], descending) >= 0)
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
