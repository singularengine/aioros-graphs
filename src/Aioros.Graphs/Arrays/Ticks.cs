using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Arrays;

/// <summary>
/// Nicely-rounded values between two numbers, as d3-array's <c>ticks</c>, <c>tickIncrement</c> and <c>tickStep</c>:
/// multiples of 1, 2, 5 and powers of ten, with as many values as possible near the requested count.
/// </summary>
public static class Ticks
{
    private static readonly double E10 = Math.Sqrt(50);
    private static readonly double E5 = Math.Sqrt(10);
    private static readonly double E2 = Math.Sqrt(2);

    /// <summary>Returns the ticks between <paramref name="start"/> and <paramref name="stop"/>, both inclusive.</summary>
    /// <param name="start">The first bound; the ticks are in reverse order when it is greater than <paramref name="stop"/>.</param>
    /// <param name="stop">The second bound.</param>
    /// <param name="count">The approximate number of ticks wanted.</param>
    /// <returns>The ticks, empty when <paramref name="count"/> is not positive.</returns>
    public static double[] Compute(double start, double stop, double count)
    {
        var ticks = new double[Count(start, stop, count)];
        Compute(start, stop, count, ticks);
        return ticks;
    }

    /// <summary>Writes the ticks between <paramref name="start"/> and <paramref name="stop"/> without allocating.</summary>
    /// <param name="start">The first bound.</param>
    /// <param name="stop">The second bound.</param>
    /// <param name="count">The approximate number of ticks wanted.</param>
    /// <param name="destination">At least <see cref="Count"/> values.</param>
    /// <returns>The number of ticks written.</returns>
    public static int Compute(double start, double stop, double count, Span<double> destination)
    {
        if (!(count > 0))
        {
            return 0;
        }

        if (start == stop)
        {
            destination[0] = start;
            return 1;
        }

        var reverse = stop < start;
        var (i1, i2, increment) = reverse ? Spec(stop, start, count) : Spec(start, stop, count);
        if (!(i2 >= i1))
        {
            return 0;
        }

        var n = (int)(i2 - i1 + 1);
        for (var i = 0; i < n; i++)
        {
            var k = reverse ? i2 - i : i1 + i;
            destination[i] = increment < 0 ? k / -increment : k * increment;
        }

        return n;
    }

    /// <summary>Returns how many ticks <see cref="Compute(double, double, double)"/> returns.</summary>
    /// <param name="start">The first bound.</param>
    /// <param name="stop">The second bound.</param>
    /// <param name="count">The approximate number of ticks wanted.</param>
    /// <returns>The number of ticks.</returns>
    public static int Count(double start, double stop, double count)
    {
        if (!(count > 0))
        {
            return 0;
        }

        if (start == stop)
        {
            return 1;
        }

        var (i1, i2, _) = stop < start ? Spec(stop, start, count) : Spec(start, stop, count);
        return i2 >= i1 ? (int)(i2 - i1 + 1) : 0;
    }

    /// <summary>
    /// Returns the tick step as d3's <c>tickIncrement</c>: a positive power-of-ten multiple, or the negative inverse
    /// of the step when it is below one, so that it stays an exact integer.
    /// </summary>
    /// <param name="start">The lower bound.</param>
    /// <param name="stop">The upper bound.</param>
    /// <param name="count">The approximate number of ticks wanted.</param>
    /// <returns>The increment.</returns>
    public static double Increment(double start, double stop, double count) => Spec(start, stop, count).Increment;

    /// <summary>Returns the signed difference between adjacent ticks, as d3's <c>tickStep</c>.</summary>
    /// <param name="start">The first bound.</param>
    /// <param name="stop">The second bound.</param>
    /// <param name="count">The approximate number of ticks wanted.</param>
    /// <returns>The step, negative when <paramref name="stop"/> is below <paramref name="start"/>.</returns>
    public static double Step(double start, double stop, double count)
    {
        var reverse = stop < start;
        var increment = reverse ? Increment(stop, start, count) : Increment(start, stop, count);
        return (reverse ? -1 : 1) * (increment < 0 ? 1 / -increment : increment);
    }

    /// <summary>Extends a span to nicely-rounded bounds, as d3's <c>nice(start, stop, count)</c>: repeated until the tick increment stops changing.</summary>
    /// <param name="start">The lower bound.</param>
    /// <param name="stop">The upper bound.</param>
    /// <param name="count">The approximate number of ticks.</param>
    /// <returns>The extended bounds, or the bounds themselves when no finite increment fits.</returns>
    public static (double Start, double Stop) Nice(double start, double stop, double count)
    {
        var previous = double.NaN;
        while (true)
        {
            var step = Increment(start, stop, count);
            if (step == previous || step == 0 || !double.IsFinite(step))
            {
                return (start, stop);
            }

            if (step > 0)
            {
                start = Math.Floor(start / step) * step;
                stop = Math.Ceiling(stop / step) * step;
            }
            else
            {
                start = Math.Ceiling(start * step) / step;
                stop = Math.Floor(stop * step) / step;
            }

            previous = step;
        }
    }

    private static (double I1, double I2, double Increment) Spec(double start, double stop, double count)
    {
        var step = (stop - start) / Math.Max(0, count);
        var power = Math.Floor(Math.Log10(step));
        var error = step / Math.Pow(10, power);
        var factor = error >= E10 ? 10 : error >= E5 ? 5 : error >= E2 ? 2 : 1;
        var (i1, i2, increment) = power < 0 ? Inverse(start, stop, Math.Pow(10, -power) / factor) : Direct(start, stop, Math.Pow(10, power) * factor);
        return i2 < i1 && 0.5 <= count && count < 2 ? Spec(start, stop, count * 2) : (i1, i2, increment);
    }

    private static (double I1, double I2, double Increment) Inverse(double start, double stop, double increment)
    {
        var i1 = JsNumber.Round(start * increment);
        var i2 = JsNumber.Round(stop * increment);
        if (i1 / increment < start)
        {
            ++i1;
        }

        if (i2 / increment > stop)
        {
            --i2;
        }

        return (i1, i2, -increment);
    }

    private static (double I1, double I2, double Increment) Direct(double start, double stop, double increment)
    {
        var i1 = JsNumber.Round(start / increment);
        var i2 = JsNumber.Round(stop / increment);
        if (i1 * increment < start)
        {
            ++i1;
        }

        if (i2 * increment > stop)
        {
            --i2;
        }

        return (i1, i2, increment);
    }
}
