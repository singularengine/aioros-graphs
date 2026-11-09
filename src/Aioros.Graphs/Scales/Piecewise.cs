using Aioros.Graphs.Arrays;
using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Scales;

internal readonly struct Piecewise
{
    private readonly double[] from;
    private readonly double[] to;
    private readonly int pieces;
    private readonly double a;
    private readonly double span;
    private readonly double r0;
    private readonly double r1;
    private readonly bool round;

    public Piecewise(double[] domain, double[] range, bool round)
    {
        this.round = round;
        pieces = Math.Min(domain.Length, range.Length) - 1;
        if (pieces > 1)
        {
            from = domain;
            to = range;
            if (domain[pieces] < domain[0])
            {
                from = [.. domain];
                to = [.. range];
                Array.Reverse(from);
                Array.Reverse(to);
            }

            return;
        }

        from = domain;
        to = range;
        var (d0, d1, s0, s1) = domain[1] < domain[0] ? (domain[1], domain[0], range[1], range[0]) : (domain[0], domain[1], range[0], range[1]);
        a = d0;
        span = d1 - d0;
        r0 = s0;
        r1 = s1;
    }

    public double Map(double x)
    {
        if (pieces > 1)
        {
            var i = Bisect.Right(from, x, 1, pieces) - 1;
            return Interpolate(to[i], to[i + 1], Normalize(from[i], from[i + 1], x));
        }

        var t = span != 0 ? (x - a) / span : double.IsNaN(span) ? double.NaN : 0.5;
        return Interpolate(r0, r1, t);
    }

    private static double Normalize(double a, double b, double x)
    {
        var span = b - a;
        return span != 0 ? (x - a) / span : double.IsNaN(span) ? double.NaN : 0.5;
    }

    private double Interpolate(double start, double end, double t)
    {
        var value = start * (1 - t) + end * t;
        return round ? JsNumber.Round(value) : value;
    }
}
