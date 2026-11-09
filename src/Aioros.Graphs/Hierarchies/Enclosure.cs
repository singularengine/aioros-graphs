namespace Aioros.Graphs.Hierarchies;

/// <summary>A circle: its center and radius.</summary>
/// <param name="X">The center's x.</param>
/// <param name="Y">The center's y.</param>
/// <param name="R">The radius.</param>
public readonly record struct Circle(double X, double Y, double R);

internal struct Lcg
{
    private const long A = 1664525;
    private const long C = 1013904223;
    private const long M = 4294967296;

    private long state;

    public static Lcg Create() => new() { state = 1 };

    public double Next()
    {
        state = (A * state + C) % M;
        return state / (double)M;
    }
}

internal static class Enclosure
{
    public static bool Enclose(Span<Circle> circles, ref Lcg random, out Circle enclosing)
    {
        Shuffle(circles, ref random);
        var basis = default(Basis);
        var found = false;
        enclosing = default;
        var i = 0;
        while (i < circles.Length)
        {
            var p = circles[i];
            if (found && EnclosesWeak(enclosing, p))
            {
                i++;
                continue;
            }

            basis = Extend(basis, p);
            enclosing = basis.Enclosing();
            found = true;
            i = 0;
        }

        return found;
    }

    private static void Shuffle(Span<Circle> circles, ref Lcg random)
    {
        var m = circles.Length;
        while (m != 0)
        {
            var i = (int)(random.Next() * m--);
            (circles[m], circles[i]) = (circles[i], circles[m]);
        }
    }

    private static Basis Extend(Basis basis, Circle p)
    {
        if (EnclosesWeakAll(p, basis))
        {
            return new Basis(p);
        }

        for (var i = 0; i < basis.Count; i++)
        {
            if (EnclosesNot(p, basis[i]) && EnclosesWeakAll(Enclose2(basis[i], p), basis))
            {
                return new Basis(basis[i], p);
            }
        }

        for (var i = 0; i < basis.Count - 1; i++)
        {
            for (var j = i + 1; j < basis.Count; j++)
            {
                if (EnclosesNot(Enclose2(basis[i], basis[j]), p)
                    && EnclosesNot(Enclose2(basis[i], p), basis[j])
                    && EnclosesNot(Enclose2(basis[j], p), basis[i])
                    && EnclosesWeakAll(Enclose3(basis[i], basis[j], p), basis))
                {
                    return new Basis(basis[i], basis[j], p);
                }
            }
        }

        throw new InvalidOperationException("No enclosing basis.");
    }

    private static bool EnclosesNot(Circle a, Circle b)
    {
        var dr = a.R - b.R;
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return dr < 0 || dr * dr < dx * dx + dy * dy;
    }

    private static bool EnclosesWeak(Circle a, Circle b)
    {
        var dr = a.R - b.R + Math.Max(Math.Max(a.R, b.R), 1) * 1e-9;
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return dr > 0 && dr * dr > dx * dx + dy * dy;
    }

    private static bool EnclosesWeakAll(Circle a, Basis basis)
    {
        for (var i = 0; i < basis.Count; i++)
        {
            if (!EnclosesWeak(a, basis[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static Circle Enclose2(Circle a, Circle b)
    {
        double x1 = a.X, y1 = a.Y, r1 = a.R;
        double x2 = b.X, y2 = b.Y, r2 = b.R;
        double x21 = x2 - x1, y21 = y2 - y1, r21 = r2 - r1;
        var l = Math.Sqrt(x21 * x21 + y21 * y21);
        return new Circle((x1 + x2 + x21 / l * r21) / 2, (y1 + y2 + y21 / l * r21) / 2, (l + r1 + r2) / 2);
    }

    private static Circle Enclose3(Circle a, Circle b, Circle c)
    {
        double x1 = a.X, y1 = a.Y, r1 = a.R;
        double x2 = b.X, y2 = b.Y, r2 = b.R;
        double x3 = c.X, y3 = c.Y, r3 = c.R;
        var a2 = x1 - x2;
        var a3 = x1 - x3;
        var b2 = y1 - y2;
        var b3 = y1 - y3;
        var c2 = r2 - r1;
        var c3 = r3 - r1;
        var d1 = x1 * x1 + y1 * y1 - r1 * r1;
        var d2 = d1 - x2 * x2 - y2 * y2 + r2 * r2;
        var d3 = d1 - x3 * x3 - y3 * y3 + r3 * r3;
        var ab = a3 * b2 - a2 * b3;
        var xa = (b2 * d3 - b3 * d2) / (ab * 2) - x1;
        var xb = (b3 * c2 - b2 * c3) / ab;
        var ya = (a3 * d2 - a2 * d3) / (ab * 2) - y1;
        var yb = (a2 * c3 - a3 * c2) / ab;
        var qa = xb * xb + yb * yb - 1;
        var qb = 2 * (r1 + xa * xb + ya * yb);
        var qc = xa * xa + ya * ya - r1 * r1;
        var r = -(Math.Abs(qa) > 1e-6 ? (qb + Math.Sqrt(qb * qb - 4 * qa * qc)) / (2 * qa) : qc / qb);
        return new Circle(x1 + xa + xb * r, y1 + ya + yb * r, r);
    }

    private readonly struct Basis
    {
        private readonly Circle first;
        private readonly Circle second;
        private readonly Circle third;

        public Basis(Circle first) => (this.first, Count) = (first, 1);

        public Basis(Circle first, Circle second) => (this.first, this.second, Count) = (first, second, 2);

        public Basis(Circle first, Circle second, Circle third) => (this.first, this.second, this.third, Count) = (first, second, third, 3);

        public int Count { get; }

        public Circle this[int index] => index switch
        {
            0 => first,
            1 => second,
            _ => third,
        };

        public Circle Enclosing() => Count switch
        {
            1 => first,
            2 => Enclose2(first, second),
            _ => Enclose3(first, second, third),
        };
    }
}
