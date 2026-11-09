using System.Globalization;
using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Scales;

internal readonly struct LogBase(double @base, bool negative)
{
    private const int PowersBelowOne = 400;
    private const int PowersAboveOne = 400;
    private static readonly double[] PowersOfTen = Powers();
    private readonly double logOfBase = JsMath.Log(@base);

    public double Base => @base;

    public bool IsInteger => @base % 1 is var remainder && (remainder == 0 || double.IsNaN(remainder));

    public double Logs(double x) => negative ? -Log(-x) : Log(x);

    public double Pows(double x) => negative ? -Pow(-x) : Pow(x);

    public int Ticks(double first, double last, double count, Span<double> destination)
    {
        var (u, v) = last < first ? (last, first) : (first, last);
        var i = Logs(u);
        var j = Logs(v);
        int total;
        if (IsInteger && j - i < count)
        {
            total = Multiples(Math.Floor(i), Math.Ceiling(j), u, v, destination);
            if (total * 2 < count)
            {
                total = Write(u, v, count, destination);
            }
        }
        else
        {
            total = Write(i, j, Math.Min(j - i, count), destination);
            if (total <= destination.Length)
            {
                for (var k = 0; k < total; k++)
                {
                    destination[k] = Pows(destination[k]);
                }
            }
        }

        if (last < first && total <= destination.Length)
        {
            destination[..total].Reverse();
        }

        return total;
    }

    public double[] Ticks(double first, double last, double count)
    {
        Span<double> buffer = stackalloc double[128];
        var total = Ticks(first, last, count, buffer);
        if (total <= buffer.Length)
        {
            return buffer[..total].ToArray();
        }

        var ticks = new double[total];
        Ticks(first, last, count, ticks);
        return ticks;
    }

    public TickLabelFormat TickFormat(double first, double last, double count, string? specifier)
    {
        specifier ??= @base == 10 ? "s" : ",";
        NumberFormat format;
        if (IsInteger)
        {
            var parsed = FormatSpecifier.Parse(specifier);
            format = NumberFormat.Create(parsed.Precision is null ? parsed with { Trim = true } : parsed);
        }
        else
        {
            format = NumberFormat.Create(specifier);
        }

        var limit = double.IsPositiveInfinity(count) ? count : Math.Max(1, @base * count / Ticks(first, last, 10).Length);
        return new TickLabelFormat(format, this, limit);
    }

    public double[] Nice(ReadOnlySpan<double> domain)
    {
        var stops = domain.ToArray();
        var (i0, i1) = stops[^1] < stops[0] ? (stops.Length - 1, 0) : (0, stops.Length - 1);
        stops[i0] = Pows(Math.Floor(Logs(stops[i0])));
        stops[i1] = Pows(Math.Ceiling(Logs(stops[i1])));
        return stops;
    }

    private static int Write(double start, double stop, double count, Span<double> destination)
    {
        var total = Arrays.Ticks.Count(start, stop, count);
        if (total <= destination.Length)
        {
            Arrays.Ticks.Compute(start, stop, count, destination);
        }

        return total;
    }

    private int Multiples(double i, double j, double u, double v, Span<double> destination)
    {
        var total = 0;
        for (; i <= j; ++i)
        {
            total = u > 0 ? Ascending(i, u, v, destination, total) : Descending(i, u, v, destination, total);
        }

        return total;
    }

    private int Ascending(double i, double u, double v, Span<double> destination, int total)
    {
        for (var k = 1.0; k < @base; ++k)
        {
            var t = i < 0 ? k / Pows(-i) : k * Pows(i);
            if (t > v)
            {
                break;
            }

            total = Add(destination, total, t, u);
        }

        return total;
    }

    private int Descending(double i, double u, double v, Span<double> destination, int total)
    {
        for (var k = @base - 1; k >= 1; --k)
        {
            var t = i > 0 ? k / Pows(-i) : k * Pows(i);
            if (t > v)
            {
                break;
            }

            total = Add(destination, total, t, u);
        }

        return total;
    }

    private static int Add(Span<double> destination, int total, double t, double u)
    {
        if (t < u)
        {
            return total;
        }

        if (total < destination.Length)
        {
            destination[total] = t;
        }

        return total + 1;
    }

    private double Log(double x)
    {
        if (@base == Math.E)
        {
            return JsMath.Log(x);
        }

        return @base switch
        {
            10 => Math.Log10(x),
            2 => Math.Log2(x),
            _ => JsMath.Log(x) / logOfBase,
        };
    }

    private double Pow(double x)
    {
        if (@base == Math.E)
        {
            return JsMath.Exp(x);
        }

        return @base == 10 ? Pow10(x) : Math.Pow(@base, x);
    }

    private static double Pow10(double x)
    {
        if (!double.IsFinite(x))
        {
            return x < 0 ? 0 : x;
        }

        if (x != Math.Floor(x) || Math.Abs(x) >= 1e21)
        {
            return double.NaN;
        }

        if (x < -PowersBelowOne)
        {
            return 0;
        }

        return x > PowersAboveOne ? double.PositiveInfinity : PowersOfTen[(int)x + PowersBelowOne];
    }

    private static double[] Powers()
    {
        var powers = new double[PowersBelowOne + PowersAboveOne + 1];
        for (var i = 0; i < powers.Length; i++)
        {
            powers[i] = double.Parse("1e" + (i - PowersBelowOne).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        return powers;
    }
}
