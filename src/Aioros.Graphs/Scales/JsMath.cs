namespace Aioros.Graphs.Scales;

internal static class JsMath
{
    private const double Ln2Hi = 6.93147180369123816490e-01;
    private const double Ln2Lo = 1.90821492927058770002e-10;
    private const double Two54 = 1.80143985094819840000e+16;
    private const double InvLn2 = 1.44269504088896338700e+00;
    private const double OverflowThreshold = 7.09782712893383973096e+02;
    private const double UnderflowThreshold = -7.45133219101941108420e+02;
    private const double Huge = 1.0e+300;
    private const double Tiny = 1.0e-300;

    public static double Sign(double x) => x > 0 ? 1 : x < 0 ? -1 : x;

    public static double Log(double x)
    {
        var bits = BitConverter.DoubleToInt64Bits(x);
        var hx = (int)(bits >> 32);
        var lx = (uint)bits;
        var k = 0;
        if (hx < 0x00100000)
        {
            if (((hx & 0x7fffffff) | (int)lx) == 0)
            {
                return double.NegativeInfinity;
            }

            if (hx < 0)
            {
                return double.NaN;
            }

            k -= 54;
            x *= Two54;
            hx = High(x);
        }

        if (hx >= 0x7ff00000)
        {
            return x + x;
        }

        k += (hx >> 20) - 1023;
        hx &= 0x000fffff;
        var i = (hx + 0x95f64) & 0x100000;
        x = WithHigh(x, hx | (i ^ 0x3ff00000));
        k += i >> 20;
        return LogTail(x - 1.0, k, hx);
    }

    public static double Exp(double x)
    {
        var hx = (uint)High(x);
        var negative = (hx >> 31) != 0;
        hx &= 0x7fffffff;
        if (hx >= 0x40862E42)
        {
            return ExpLarge(x, hx, negative);
        }

        if (hx <= 0x3fd62e42)
        {
            return hx < 0x3e300000 ? 1.0 + x : ExpTail(x, 0, 0, 0);
        }

        if (x == 1.0)
        {
            return Math.E;
        }

        var (hi, lo, k) = ReduceExp(x, hx, negative);
        return ExpTail(hi - lo, k, hi, lo);
    }

    public static double Log1P(double x)
    {
        var hx = High(x);
        if (hx < 0x3FDA827A && Small(x, hx) is { } small)
        {
            return small;
        }

        if (hx >= 0x7ff00000)
        {
            return x + x;
        }

        var (k, f, hu, c) = Reduce(x, hx);
        return Log1PTail(f, k, hu, c);
    }

    public static double ExpM1(double x)
    {
        var hx = (uint)High(x);
        var negative = (hx & 0x80000000) != 0;
        hx &= 0x7fffffff;
        if (hx >= 0x4043687A && Large(x, hx, negative) is { } large)
        {
            return large;
        }

        if (hx <= 0x3fd62e42)
        {
            if (hx < 0x3c900000)
            {
                var t = Huge + x;
                return x - (t - (Huge + x));
            }

            return ExpM1Tail(x, 0, 0);
        }

        var (hi, lo, k) = ReduceExp(x, hx, negative);
        var reduced = hi - lo;
        return ExpM1Tail(reduced, k, hi - reduced - lo);
    }

    private static double ExpLarge(double x, uint hx, bool negative)
    {
        if (hx >= 0x7ff00000)
        {
            return double.IsNaN(x) ? x + x : negative ? 0.0 : x;
        }

        if (x > OverflowThreshold)
        {
            return double.PositiveInfinity;
        }

        return x < UnderflowThreshold ? 0 : Exp(x, hx, negative);
    }

    private static double Exp(double x, uint hx, bool negative)
    {
        var (hi, lo, k) = ReduceExp(x, hx, negative);
        return ExpTail(hi - lo, k, hi, lo);
    }

    private static (double Hi, double Lo, int K) ReduceExp(double x, uint hx, bool negative)
    {
        if (hx < 0x3FF0A2B2)
        {
            return negative ? (x + Ln2Hi, -Ln2Lo, -1) : (x - Ln2Hi, Ln2Lo, 1);
        }

        var k = (int)(InvLn2 * x + (negative ? -0.5 : 0.5));
        double t = k;
        return (x - t * Ln2Hi, t * Ln2Lo, k);
    }

    private static double LogTail(double f, int k, int hx)
    {
        double dk = k;
        if ((0x000fffff & (2 + hx)) < 3)
        {
            if (f == 0)
            {
                return k == 0 ? 0 : dk * Ln2Hi + dk * Ln2Lo;
            }

            var r = f * f * (0.5 - 0.33333333333333333 * f);
            return k == 0 ? f - r : dk * Ln2Hi - ((r - dk * Ln2Lo) - f);
        }

        var s = f / (2.0 + f);
        var z = s * s;
        var w = z * z;
        var t1 = w * (3.999999999940941908e-01 + w * (2.222219843214978396e-01 + w * 1.531383769920937332e-01));
        var t2 = z * (6.666666666666735130e-01 + w * (2.857142874366239149e-01 + w * (1.818357216161805012e-01 + w * 1.479819860511658591e-01)));
        var rr = t2 + t1;
        if (((hx - 0x6147a) | (0x6b851 - hx)) > 0)
        {
            var hfsq = 0.5 * f * f;
            return k == 0 ? f - (hfsq - s * (hfsq + rr)) : dk * Ln2Hi - ((hfsq - (s * (hfsq + rr) + dk * Ln2Lo)) - f);
        }

        return k == 0 ? f - s * (f - rr) : dk * Ln2Hi - ((s * (f - rr) - dk * Ln2Lo) - f);
    }

    private static double ExpTail(double x, int k, double hi, double lo)
    {
        var t = x * x;
        var twopk = k >= -1021
            ? BitConverter.Int64BitsToDouble((long)(0x3ff00000 + (k << 20)) << 32)
            : BitConverter.Int64BitsToDouble((long)(0x3ff00000 + ((k + 1000) << 20)) << 32);
        var c = x - t * (1.66666666666666019037e-01 + t * (-2.77777777770155933842e-03 + t * (6.61375632143793436117e-05
            + t * (-1.65339022054652515390e-06 + t * 4.13813679705723846039e-08))));
        if (k == 0)
        {
            return 1.0 - ((x * c) / (c - 2.0) - x);
        }

        var y = 1.0 - ((lo - (x * c) / (2.0 - c)) - hi);
        if (k < -1021)
        {
            return y * twopk * 9.33263618503218878990e-302;
        }

        return k == 1024 ? y * 2.0 * 8.98846567431158e307 : y * twopk;
    }

    private static double? Small(double x, int hx)
    {
        var ax = hx & 0x7fffffff;
        if (ax >= 0x3ff00000)
        {
            return x == -1.0 ? double.NegativeInfinity : double.NaN;
        }

        if (ax < 0x3e200000)
        {
            return Two54 + x > 0 && ax < 0x3c900000 ? x : x - x * x * 0.5;
        }

        return hx > 0 || hx <= unchecked((int)0xbfd2bec4) ? Log1PTail(x, 0, 1, 0) : null;
    }

    private static double? Large(double x, uint hx, bool negative)
    {
        if (hx >= 0x40862E42)
        {
            if (hx >= 0x7ff00000)
            {
                return double.IsNaN(x) ? x + x : negative ? -1.0 : x;
            }

            if (x > OverflowThreshold)
            {
                return double.PositiveInfinity;
            }
        }

        return negative && x + Tiny < 0.0 ? Tiny - 1.0 : null;
    }

    private static (int K, double F, int Hu, double C) Reduce(double x, int hx)
    {
        double u;
        int hu;
        int k;
        double c;
        if (hx < 0x43400000)
        {
            u = 1.0 + x;
            hu = High(u);
            k = (hu >> 20) - 1023;
            c = k > 0 ? 1.0 - (u - x) : x - (u - 1.0);
            c /= u;
        }
        else
        {
            u = x;
            hu = High(u);
            k = (hu >> 20) - 1023;
            c = 0;
        }

        hu &= 0x000fffff;
        if (hu < 0x6a09e)
        {
            u = WithHigh(u, hu | 0x3ff00000);
        }
        else
        {
            k += 1;
            u = WithHigh(u, hu | 0x3fe00000);
            hu = (0x00100000 - hu) >> 2;
        }

        return (k, u - 1.0, hu, c);
    }

    private static double Log1PTail(double f, int k, int hu, double c)
    {
        var hfsq = 0.5 * f * f;
        if (hu == 0)
        {
            if (f == 0)
            {
                return k == 0 ? 0 : k * Ln2Hi + (c + k * Ln2Lo);
            }

            var r = hfsq * (1.0 - 0.66666666666666666 * f);
            return k == 0 ? f - r : k * Ln2Hi - ((r - (k * Ln2Lo + c)) - f);
        }

        var s = f / (2.0 + f);
        var z = s * s;
        var rr = z * (6.666666666666735130e-01 + z * (3.999999999940941908e-01 + z * (2.857142874366239149e-01 + z * (2.222219843214978396e-01
            + z * (1.818357216161805012e-01 + z * (1.531383769920937332e-01 + z * 1.479819860511658591e-01))))));
        return k == 0 ? f - (hfsq - s * (hfsq + rr)) : k * Ln2Hi - ((hfsq - (s * (hfsq + rr) + (k * Ln2Lo + c))) - f);
    }

    private static double ExpM1Tail(double x, int k, double c)
    {
        var hfx = 0.5 * x;
        var hxs = x * hfx;
        var r1 = 1.0 + hxs * (-3.33333333333331316428e-02 + hxs * (1.58730158725481460165e-03 + hxs * (-7.93650757867487942473e-05
            + hxs * (4.00821782732936239552e-06 + hxs * -2.01099218183624371326e-07))));
        var t = 3.0 - r1 * hfx;
        var e = hxs * ((r1 - t) / (6.0 - x * t));
        if (k == 0)
        {
            return x - (x * e - hxs);
        }

        var twopk = BitConverter.Int64BitsToDouble((long)(0x3ff00000 + (k << 20)) << 32);
        e = x * (e - c) - c;
        e -= hxs;
        if (k == -1)
        {
            return 0.5 * (x - e) - 0.5;
        }

        if (k == 1)
        {
            return x < -0.25 ? -2.0 * (e - (x + 0.5)) : 1.0 + 2.0 * (x - e);
        }

        if (k <= -2 || k > 56)
        {
            var y = 1.0 - (e - x);
            y = k == 1024 ? y * 2.0 * 8.98846567431158e307 : y * twopk;
            return y - 1.0;
        }

        if (k < 20)
        {
            var t1 = BitConverter.Int64BitsToDouble((long)(0x3ff00000 - (0x200000 >> k)) << 32);
            return (t1 - (e - x)) * twopk;
        }

        var t2 = BitConverter.Int64BitsToDouble((long)((0x3ff - k) << 20) << 32);
        return (x - (e + t2) + 1.0) * twopk;
    }

    private static int High(double x) => (int)(BitConverter.DoubleToInt64Bits(x) >> 32);

    private static double WithHigh(double x, int high) =>
        BitConverter.Int64BitsToDouble(((long)high << 32) | (BitConverter.DoubleToInt64Bits(x) & 0xffffffffL));
}
