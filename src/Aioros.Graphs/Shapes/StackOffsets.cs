namespace Aioros.Graphs.Shapes;

internal static class StackOffsets
{
    public static void Apply(Series series, ReadOnlySpan<int> order, StackOffset offset)
    {
        switch (offset)
        {
            case StackOffset.Expand:
                Expand(series);
                None(series, order);
                break;
            case StackOffset.Diverging:
                Diverging(series, order);
                break;
            case StackOffset.Silhouette:
                Silhouette(series, order);
                None(series, order);
                break;
            case StackOffset.Wiggle:
                Wiggle(series, order);
                None(series, order);
                break;
            default:
                None(series, order);
                break;
        }
    }

    private static double OrZero(double value) => double.IsNaN(value) ? 0 : value;

    private static void None(Series series, ReadOnlySpan<int> order)
    {
        for (var i = 1; i < series.Count; i++)
        {
            for (var j = 0; j < series.Length; j++)
            {
                var below = series[order[i - 1], j];
                ref var point = ref series[order[i], j];
                var lower = double.IsNaN(below.Upper) ? below.Lower : below.Upper;
                point = new StackPoint(lower, point.Upper + lower);
            }
        }
    }

    private static void Expand(Series series)
    {
        for (var j = 0; j < series.Length; j++)
        {
            var total = 0.0;
            for (var i = 0; i < series.Count; i++)
            {
                total += OrZero(series[i, j].Upper);
            }

            if (total == 0)
            {
                continue;
            }

            for (var i = 0; i < series.Count; i++)
            {
                ref var point = ref series[i, j];
                point = point with { Upper = point.Upper / total };
            }
        }
    }

    private static void Diverging(Series series, ReadOnlySpan<int> order)
    {
        for (var j = 0; j < series.Length; j++)
        {
            var (positive, negative) = (0.0, 0.0);
            for (var i = 0; i < series.Count; i++)
            {
                ref var point = ref series[order[i], j];
                var dy = point.Upper - point.Lower;
                if (dy > 0)
                {
                    point = new StackPoint(positive, positive + dy);
                    positive = point.Upper;
                }
                else if (dy < 0)
                {
                    point = new StackPoint(negative + dy, negative);
                    negative = point.Lower;
                }
                else
                {
                    point = new StackPoint(0, dy);
                }
            }
        }
    }

    private static void Silhouette(Series series, ReadOnlySpan<int> order)
    {
        for (var j = 0; j < series.Length; j++)
        {
            var total = 0.0;
            for (var i = 0; i < series.Count; i++)
            {
                total += OrZero(series[i, j].Upper);
            }

            ref var point = ref series[order[0], j];
            var lower = -total / 2;
            point = new StackPoint(lower, point.Upper + lower);
        }
    }

    private static void Wiggle(Series series, ReadOnlySpan<int> order)
    {
        if (series.Length == 0)
        {
            return;
        }

        var y = 0.0;
        var j = 1;
        for (; j < series.Length; j++)
        {
            var (s1, s2) = (0.0, 0.0);
            for (var i = 0; i < series.Count; i++)
            {
                var current = OrZero(series[order[i], j].Upper);
                var previous = OrZero(series[order[i], j - 1].Upper);
                var s3 = (current - previous) / 2;
                for (var k = 0; k < i; k++)
                {
                    s3 += OrZero(series[order[k], j].Upper) - OrZero(series[order[k], j - 1].Upper);
                }

                s1 += current;
                s2 += s3 * current;
            }

            Shift(series, order[0], j - 1, y);
            if (s1 != 0)
            {
                y -= s2 / s1;
            }
        }

        Shift(series, order[0], j - 1, y);
    }

    private static void Shift(Series series, int index, int point, double y)
    {
        ref var p = ref series[index, point];
        p = new StackPoint(y, p.Upper + y);
    }
}
