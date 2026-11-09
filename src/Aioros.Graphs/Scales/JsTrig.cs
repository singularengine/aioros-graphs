namespace Aioros.Graphs.Scales;

internal static class JsTrig
{
    private const double InvPio2 = 6.36619772367581382433e-01;
    private const double Pio2One = 1.57079632673412561417e+00;
    private const double Pio2OneTail = 6.07710050650619224932e-11;
    private const double Pio2Two = 6.07710050630396597660e-11;
    private const double Pio2TwoTail = 2.02226624879595063154e-21;
    private const double Pio2Three = 2.02226624871116645580e-21;
    private const double Pio2ThreeTail = 8.47842766036889956997e-32;
    private const double Two24 = 1.67772160000000000000e+07;
    private const double TwoMinus24 = 5.96046447753906250000e-08;

    private static readonly int[] TwoOverPi =
    [
        0xA2F983, 0x6E4E44, 0x1529FC, 0x2757D1, 0xF534DD, 0xC0DB62, 0x95993C,
        0x439041, 0xFE5163, 0xABDEBB, 0xC561B7, 0x246E3A, 0x424DD2, 0xE00649,
        0x2EEA09, 0xD1921C, 0xFE1DEB, 0x1CB129, 0xA73EE8, 0x8235F5, 0x2EBB44,
        0x84E99C, 0x7026B4, 0x5F7E41, 0x3991D6, 0x398353, 0x39F49C, 0x845F8B,
        0xBDF928, 0x3B1FF8, 0x97FFDE, 0x05980F, 0xEF2F11, 0x8B5A0A, 0x6D1F6D,
        0x367ECF, 0x27CB09, 0xB74F46, 0x3F669E, 0x5FEA2D, 0x7527BA, 0xC7EBE5,
        0xF17B3D, 0x0739F7, 0x8A5292, 0xEA6BFB, 0x5FB11F, 0x8D5D08, 0x560330,
        0x46FC7B, 0x6BABF0, 0xCFBC20, 0x9AF436, 0x1DA9E3, 0x91615E, 0xE61B08,
        0x659985, 0x5F14A0, 0x68408D, 0xFFD880, 0x4D7327, 0x310606, 0x1556CA,
        0x73A8C9, 0x60E27B, 0xC08C6B,
    ];

    private static readonly int[] NPio2High =
    [
        0x3FF921FB, 0x400921FB, 0x4012D97C, 0x401921FB, 0x401F6A7A, 0x4022D97C,
        0x4025FDBB, 0x402921FB, 0x402C463A, 0x402F6A7A, 0x4031475C, 0x4032D97C,
        0x40346B9C, 0x4035FDBB, 0x40378FDB, 0x403921FB, 0x403AB41B, 0x403C463A,
        0x403DD85A, 0x403F6A7A, 0x40407E4C, 0x4041475C, 0x4042106C, 0x4042D97C,
        0x4043A28C, 0x40446B9C, 0x404534AC, 0x4045FDBB, 0x4046C6CB, 0x40478FDB,
        0x404858EB, 0x404921FB,
    ];

    private static readonly double[] Pio2Parts =
    [
        1.57079625129699707031e+00, 7.54978941586159635335e-08, 5.39030252995776476554e-15, 3.28200341580791294123e-22,
        1.27065575308067607349e-29, 1.22933308981111328932e-36, 2.73370053816464559624e-44, 2.16741683877804819444e-51,
    ];

    public static double Sin(double x)
    {
        var ix = High(x) & 0x7FFFFFFF;
        if (ix <= 0x3FE921FB)
        {
            return KernelSin(x, 0, false);
        }

        if (ix >= 0x7FF00000)
        {
            return double.NaN;
        }

        var n = RemPio2(x, out var y0, out var y1);
        return (n & 3) switch
        {
            0 => KernelSin(y0, y1, true),
            1 => KernelCos(y0, y1),
            2 => -KernelSin(y0, y1, true),
            _ => -KernelCos(y0, y1),
        };
    }

    public static double Cos(double x)
    {
        var ix = High(x) & 0x7FFFFFFF;
        if (ix <= 0x3FE921FB)
        {
            return KernelCos(x, 0);
        }

        if (ix >= 0x7FF00000)
        {
            return double.NaN;
        }

        var n = RemPio2(x, out var y0, out var y1);
        return (n & 3) switch
        {
            0 => KernelCos(y0, y1),
            1 => -KernelSin(y0, y1, true),
            2 => -KernelCos(y0, y1),
            _ => KernelSin(y0, y1, true),
        };
    }

    private static double KernelSin(double x, double y, bool tail)
    {
        const double s1 = -1.66666666666666324348e-01;
        const double s2 = 8.33333333332248946124e-03;
        const double s3 = -1.98412698298579493134e-04;
        const double s4 = 2.75573137070700676789e-06;
        const double s5 = -2.50507602534068634195e-08;
        const double s6 = 1.58969099521155010221e-10;
        if ((High(x) & 0x7FFFFFFF) < 0x3E400000 && (int)x == 0)
        {
            return x;
        }

        var z = x * x;
        var v = z * x;
        var r = s2 + z * (s3 + z * (s4 + z * (s5 + z * s6)));
        return tail ? x - ((z * (0.5 * y - v * r) - y) - v * s1) : x + v * (s1 + z * r);
    }

    private static double KernelCos(double x, double y)
    {
        const double c1 = 4.16666666666666019037e-02;
        const double c2 = -1.38888888888741095749e-03;
        const double c3 = 2.48015872894767294178e-05;
        const double c4 = -2.75573143513906633035e-07;
        const double c5 = 2.08757232129817482790e-09;
        const double c6 = -1.13596475577881948265e-11;
        var ix = High(x) & 0x7FFFFFFF;
        if (ix < 0x3E400000 && (int)x == 0)
        {
            return 1.0;
        }

        var z = x * x;
        var r = z * (c1 + z * (c2 + z * (c3 + z * (c4 + z * (c5 + z * c6)))));
        if (ix < 0x3FD33333)
        {
            return 1.0 - (0.5 * z - (z * r - x * y));
        }

        var qx = ix > 0x3FE90000 ? 0.28125 : BitConverter.Int64BitsToDouble((long)(ix - 0x00200000) << 32);
        var iz = 0.5 * z - qx;
        var a = 1.0 - qx;
        return a - (iz - (z * r - x * y));
    }

    private static int RemPio2(double x, out double y0, out double y1)
    {
        var hx = High(x);
        var ix = hx & 0x7FFFFFFF;
        if (ix < 0x4002D97C)
        {
            return Near(x, hx, ix, out y0, out y1);
        }

        if (ix <= 0x413921FB)
        {
            return Medium(x, hx, ix, out y0, out y1);
        }

        var low = (uint)BitConverter.DoubleToInt64Bits(x);
        var e0 = (ix >> 20) - 1046;
        var z = BitConverter.Int64BitsToDouble(((long)(ix - (int)((uint)e0 << 20)) << 32) | low);
        Span<double> tx = stackalloc double[3];
        for (var i = 0; i < 2; i++)
        {
            tx[i] = (int)z;
            z = (z - tx[i]) * Two24;
        }

        tx[2] = z;
        var nx = 3;
        while (tx[nx - 1] == 0)
        {
            nx--;
        }

        var n = LargeRemPio2.Reduce(tx[..nx], e0, out y0, out y1);
        if (hx < 0)
        {
            y0 = -y0;
            y1 = -y1;
            return -n;
        }

        return n;
    }

    private static int Near(double x, int hx, int ix, out double y0, out double y1)
    {
        if (hx > 0)
        {
            var z = x - Pio2One;
            if (ix != 0x3FF921FB)
            {
                y0 = z - Pio2OneTail;
                y1 = (z - y0) - Pio2OneTail;
            }
            else
            {
                z -= Pio2Two;
                y0 = z - Pio2TwoTail;
                y1 = (z - y0) - Pio2TwoTail;
            }

            return 1;
        }

        var w = x + Pio2One;
        if (ix != 0x3FF921FB)
        {
            y0 = w + Pio2OneTail;
            y1 = (w - y0) + Pio2OneTail;
        }
        else
        {
            w += Pio2Two;
            y0 = w + Pio2TwoTail;
            y1 = (w - y0) + Pio2TwoTail;
        }

        return -1;
    }

    private static int Medium(double x, int hx, int ix, out double y0, out double y1)
    {
        var t = Math.Abs(x);
        var n = (int)(t * InvPio2 + 0.5);
        var fn = (double)n;
        var r = t - fn * Pio2One;
        var w = fn * Pio2OneTail;
        y0 = r - w;
        if (n >= 32 || ix == NPio2High[n - 1])
        {
            var j = ix >> 20;
            if (j - ((High(y0) >> 20) & 0x7FF) > 16)
            {
                t = r;
                w = fn * Pio2Two;
                r = t - w;
                w = fn * Pio2TwoTail - ((t - r) - w);
                y0 = r - w;
                if (j - ((High(y0) >> 20) & 0x7FF) > 49)
                {
                    t = r;
                    w = fn * Pio2Three;
                    r = t - w;
                    w = fn * Pio2ThreeTail - ((t - r) - w);
                    y0 = r - w;
                }
            }
        }

        y1 = (r - y0) - w;
        if (hx < 0)
        {
            y0 = -y0;
            y1 = -y1;
            return -n;
        }

        return n;
    }

    private static int High(double x) => (int)(BitConverter.DoubleToInt64Bits(x) >> 32);

    private static class LargeRemPio2
    {
        private const int Jk = 4;

        public static int Reduce(ReadOnlySpan<double> x, int e0, out double y0, out double y1)
        {
            var jx = x.Length - 1;
            var jv = Math.Max(0, (e0 - 3) / 24);
            var q0 = e0 - 24 * (jv + 1);
            Span<double> f = stackalloc double[20];
            Span<double> q = stackalloc double[20];
            Span<int> iq = stackalloc int[20];
            var j = jv - jx;
            for (var i = 0; i <= jx + Jk; i++, j++)
            {
                f[i] = j < 0 ? 0 : TwoOverPi[j];
            }

            for (var i = 0; i <= Jk; i++)
            {
                q[i] = Product(x, f, jx + i);
            }

            var jz = Jk;
            int n, ih;
            double z;
            while (true)
            {
                (n, ih, z) = Distill(q, iq, jz, q0);
                var k = Recompute(iq, jz, z);
                if (k == 0)
                {
                    break;
                }

                for (var i = jz + 1; i <= jz + k; i++)
                {
                    f[jx + i] = TwoOverPi[jv + i];
                    q[i] = Product(x, f, jx + i);
                }

                jz += k;
            }

            jz = Chop(iq, q, z, jz, ref q0);
            Compress(q, jz, ih, out y0, out y1);
            return n & 7;
        }

        private static double Product(ReadOnlySpan<double> x, ReadOnlySpan<double> f, int top)
        {
            var fw = 0.0;
            for (var j = 0; j < x.Length; j++)
            {
                fw += x[j] * f[top - j];
            }

            return fw;
        }

        private static (int N, int Ih, double Z) Distill(ReadOnlySpan<double> q, Span<int> iq, int jz, int q0)
        {
            var z = q[jz];
            for (int i = 0, j = jz; j > 0; i++, j--)
            {
                var fw = (double)(int)(TwoMinus24 * z);
                iq[i] = (int)(z - Two24 * fw);
                z = q[j - 1] + fw;
            }

            z = Math.ScaleB(z, q0);
            z -= 8.0 * Math.Floor(z * 0.125);
            var n = (int)z;
            z -= n;
            var ih = 0;
            if (q0 > 0)
            {
                var i = iq[jz - 1] >> (24 - q0);
                n += i;
                iq[jz - 1] -= i << (24 - q0);
                ih = iq[jz - 1] >> (23 - q0);
            }
            else if (q0 == 0)
            {
                ih = iq[jz - 1] >> 23;
            }
            else if (z >= 0.5)
            {
                ih = 2;
            }

            if (ih > 0)
            {
                n += 1;
                z = Complement(iq, jz, q0, ih, z);
            }

            return (n, ih, z);
        }

        private static double Complement(Span<int> iq, int jz, int q0, int ih, double z)
        {
            var carry = 0;
            for (var i = 0; i < jz; i++)
            {
                var j = iq[i];
                if (carry == 0)
                {
                    if (j != 0)
                    {
                        carry = 1;
                        iq[i] = 0x1000000 - j;
                    }
                }
                else
                {
                    iq[i] = 0xFFFFFF - j;
                }
            }

            if (q0 == 1)
            {
                iq[jz - 1] &= 0x7FFFFF;
            }
            else if (q0 == 2)
            {
                iq[jz - 1] &= 0x3FFFFF;
            }

            if (ih == 2)
            {
                z = 1.0 - z;
                if (carry != 0)
                {
                    z -= Math.ScaleB(1.0, q0);
                }
            }

            return z;
        }

        private static int Recompute(ReadOnlySpan<int> iq, int jz, double z)
        {
            if (z != 0)
            {
                return 0;
            }

            var j = 0;
            for (var i = jz - 1; i >= Jk; i--)
            {
                j |= iq[i];
            }

            if (j != 0)
            {
                return 0;
            }

            var k = 1;
            while (Jk >= k && iq[Jk - k] == 0)
            {
                k++;
            }

            return k;
        }

        private static int Chop(Span<int> iq, Span<double> q, double z, int jz, ref int q0)
        {
            if (z == 0.0)
            {
                jz -= 1;
                q0 -= 24;
                while (iq[jz] == 0)
                {
                    jz--;
                    q0 -= 24;
                }
            }
            else
            {
                z = Math.ScaleB(z, -q0);
                if (z >= Two24)
                {
                    var fw = (double)(int)(TwoMinus24 * z);
                    iq[jz] = (int)(z - Two24 * fw);
                    jz += 1;
                    q0 += 24;
                    iq[jz] = (int)fw;
                }
                else
                {
                    iq[jz] = (int)z;
                }
            }

            var scale = Math.ScaleB(1.0, q0);
            for (var i = jz; i >= 0; i--)
            {
                q[i] = scale * iq[i];
                scale *= TwoMinus24;
            }

            return jz;
        }

        private static void Compress(ReadOnlySpan<double> q, int jz, int ih, out double y0, out double y1)
        {
            Span<double> fq = stackalloc double[20];
            for (var i = jz; i >= 0; i--)
            {
                var fw = 0.0;
                for (var k = 0; k <= Jk && k <= jz - i; k++)
                {
                    fw += Pio2Parts[k] * q[i + k];
                }

                fq[jz - i] = fw;
            }

            var sum = 0.0;
            for (var i = jz; i >= 0; i--)
            {
                sum += fq[i];
            }

            y0 = ih == 0 ? sum : -sum;
            var rest = fq[0] - sum;
            for (var i = 1; i <= jz; i++)
            {
                rest += fq[i];
            }

            y1 = ih == 0 ? rest : -rest;
        }
    }
}
