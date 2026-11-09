using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Scales;

internal static class LinearNice
{
    public static double[]? Stops(ReadOnlySpan<double> domain, double count)
    {
        var stops = domain.ToArray();
        var (i0, i1) = (0, stops.Length - 1);
        var (start, stop) = (stops[i0], stops[i1]);
        if (stop < start)
        {
            (start, stop) = (stop, start);
            (i0, i1) = (i1, i0);
        }

        var previous = double.NaN;
        for (var iteration = 0; iteration < 10; iteration++)
        {
            var step = Ticks.Increment(start, stop, count);
            if (step == previous)
            {
                stops[i0] = start;
                stops[i1] = stop;
                return stops;
            }

            if (step > 0)
            {
                start = Math.Floor(start / step) * step;
                stop = Math.Ceiling(stop / step) * step;
            }
            else if (step < 0)
            {
                start = Math.Ceiling(start * step) / step;
                stop = Math.Floor(stop * step) / step;
            }
            else
            {
                return null;
            }

            previous = step;
        }

        return null;
    }
}
