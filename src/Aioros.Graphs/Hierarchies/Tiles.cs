using System.Buffers;

namespace Aioros.Graphs.Hierarchies;

internal sealed record SquarifyRow(int[] Children, bool Dice);

internal readonly ref struct Tiles(ReadOnlySpan<double> values, double[] x0, double[] y0, double[] x1, double[] y1)
{
    private readonly ReadOnlySpan<double> values = values;

    public static bool Truthy(double value) => value != 0 && !double.IsNaN(value);

    public void Dice(ReadOnlySpan<int> nodes, double value, double left, double top, double right, double bottom)
    {
        var k = Truthy(value) ? (right - left) / value : value;
        foreach (var node in nodes)
        {
            y0[node] = top;
            y1[node] = bottom;
            x0[node] = left;
            left += values[node] * k;
            x1[node] = left;
        }
    }

    public void Slice(ReadOnlySpan<int> nodes, double value, double left, double top, double right, double bottom)
    {
        var k = Truthy(value) ? (bottom - top) / value : value;
        foreach (var node in nodes)
        {
            x0[node] = left;
            x1[node] = right;
            y0[node] = top;
            top += values[node] * k;
            y1[node] = top;
        }
    }

    public void Binary(ReadOnlySpan<int> nodes, double value, double left, double top, double right, double bottom)
    {
        var sums = ArrayPool<double>.Shared.Rent(nodes.Length + 1);
        var sum = 0.0;
        sums[0] = 0;
        for (var i = 0; i < nodes.Length; i++)
        {
            sum += values[nodes[i]];
            sums[i + 1] = sum;
        }

        Partition(nodes, sums, 0, nodes.Length, value, left, top, right, bottom);
        ArrayPool<double>.Shared.Return(sums);
    }

    public void Squarify(ReadOnlySpan<int> nodes, double value, double ratio, double left, double top, double right, double bottom, List<SquarifyRow>? rows)
    {
        var i0 = 0;
        var n = nodes.Length;
        while (i0 < n)
        {
            var dx = right - left;
            var dy = bottom - top;
            var (i1, sumValue) = NextRow(nodes, i0, Math.Max(dy / dx, dx / dy) / (value * ratio));
            var row = nodes[i0..i1];
            var dice = dx < dy;
            rows?.Add(new SquarifyRow(row.ToArray(), dice));
            if (dice)
            {
                var start = top;
                var end = bottom;
                if (Truthy(value))
                {
                    top += dy * sumValue / value;
                    end = top;
                }

                Dice(row, sumValue, left, start, right, end);
            }
            else
            {
                var start = left;
                var end = right;
                if (Truthy(value))
                {
                    left += dx * sumValue / value;
                    end = left;
                }

                Slice(row, sumValue, start, top, end, bottom);
            }

            value -= sumValue;
            i0 = i1;
        }
    }

    private (int End, double Sum) NextRow(ReadOnlySpan<int> nodes, int i1, double alpha)
    {
        var n = nodes.Length;
        double sumValue;
        do
        {
            sumValue = values[nodes[i1++]];
        }
        while (!Truthy(sumValue) && i1 < n);

        var minValue = sumValue;
        var maxValue = sumValue;
        var beta = sumValue * sumValue * alpha;
        var minRatio = Math.Max(maxValue / beta, beta / minValue);
        for (; i1 < n; i1++)
        {
            var nodeValue = values[nodes[i1]];
            sumValue += nodeValue;
            minValue = nodeValue < minValue ? nodeValue : minValue;
            maxValue = nodeValue > maxValue ? nodeValue : maxValue;
            beta = sumValue * sumValue * alpha;
            var newRatio = Math.Max(maxValue / beta, beta / minValue);
            if (newRatio > minRatio)
            {
                sumValue -= nodeValue;
                break;
            }

            minRatio = newRatio;
        }

        return (i1, sumValue);
    }

    public void Resquarify(SquarifyRow[] rows, double value, double left, double top, double right, double bottom)
    {
        foreach (var row in rows)
        {
            var rowValue = 0.0;
            foreach (var node in row.Children)
            {
                rowValue += values[node];
            }

            if (row.Dice)
            {
                var start = top;
                var end = bottom;
                if (Truthy(value))
                {
                    top += (bottom - top) * rowValue / value;
                    end = top;
                }

                Dice(row.Children, rowValue, left, start, right, end);
            }
            else
            {
                var start = left;
                var end = right;
                if (Truthy(value))
                {
                    left += (right - left) * rowValue / value;
                    end = left;
                }

                Slice(row.Children, rowValue, start, top, end, bottom);
            }

            value -= rowValue;
        }
    }

    private void Partition(ReadOnlySpan<int> nodes, double[] sums, int i, int j, double value, double left, double top, double right, double bottom)
    {
        if (i >= j - 1)
        {
            var node = nodes[i];
            x0[node] = left;
            y0[node] = top;
            x1[node] = right;
            y1[node] = bottom;
            return;
        }

        var valueOffset = sums[i];
        var valueTarget = value / 2 + valueOffset;
        var k = i + 1;
        var hi = j - 1;
        while (k < hi)
        {
            var mid = (int)((uint)(k + hi) >> 1);
            if (sums[mid] < valueTarget)
            {
                k = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        if (valueTarget - sums[k - 1] < sums[k] - valueTarget && i + 1 < k)
        {
            --k;
        }

        var valueLeft = sums[k] - valueOffset;
        var valueRight = value - valueLeft;
        if (right - left > bottom - top)
        {
            var xk = Truthy(value) ? (left * valueRight + right * valueLeft) / value : right;
            Partition(nodes, sums, i, k, valueLeft, left, top, xk, bottom);
            Partition(nodes, sums, k, j, valueRight, xk, top, right, bottom);
        }
        else
        {
            var yk = Truthy(value) ? (top * valueRight + bottom * valueLeft) / value : bottom;
            Partition(nodes, sums, i, k, valueLeft, left, top, right, yk);
            Partition(nodes, sums, k, j, valueRight, left, yk, right, bottom);
        }
    }
}
