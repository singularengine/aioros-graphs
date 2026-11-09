using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes;

/// <summary>The arc generator of d3-shape: circular and annular sectors with padding and rounded corners, centered on the origin.</summary>
public static class Arc
{
    internal const double Epsilon = 1e-12;
    internal const double HalfPi = Math.PI / 2;
    internal const double Tau = 2 * Math.PI;

    /// <summary>Writes <paramref name="arc"/> into a new path over <paramref name="sink"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="arc">The sector.</param>
    public static void Write<TSink>(ref TSink sink, in ArcShape arc)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Write(ref context, arc);
    }

    /// <summary>Writes <paramref name="arc"/> into <paramref name="context"/>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="arc">The sector.</param>
    public static void Write<TSink>(ref PathContext<TSink> context, in ArcShape arc)
        where TSink : IPathSink
    {
        var r0 = arc.InnerRadius;
        var r1 = arc.OuterRadius;
        var a0 = arc.StartAngle - HalfPi;
        var a1 = arc.EndAngle - HalfPi;
        var da = Math.Abs(a1 - a0);
        var cw = a1 > a0;
        if (r1 < r0)
        {
            (r0, r1) = (r1, r0);
        }

        if (!(r1 > Epsilon))
        {
            context.MoveTo(0, 0);
        }
        else if (da > Tau - Epsilon)
        {
            context.MoveTo(r1 * Math.Cos(a0), r1 * Math.Sin(a0));
            context.Arc(0, 0, r1, a0, a1, !cw);
            if (r0 > Epsilon)
            {
                context.MoveTo(r0 * Math.Cos(a1), r0 * Math.Sin(a1));
                context.Arc(0, 0, r0, a1, a0, cw);
            }
        }
        else
        {
            var sector = new ArcSector(r0, r1, a0, a1, da, cw, arc);
            sector.Write(ref context);
        }

        context.ClosePath();
    }

    /// <summary>Returns the midpoint of the arc's center line, as d3's <c>arc.centroid</c>.</summary>
    /// <param name="arc">The sector.</param>
    /// <returns>The centroid.</returns>
    public static (double X, double Y) Centroid(in ArcShape arc)
    {
        var r = (arc.InnerRadius + arc.OuterRadius) / 2;
        var a = (arc.StartAngle + arc.EndAngle) / 2 - Math.PI / 2;
        return (Math.Cos(a) * r, Math.Sin(a) * r);
    }

    internal static double Acos(double x) => x > 1 ? 0 : x < -1 ? Math.PI : Math.Acos(x);

    internal static double Asin(double x) => x >= 1 ? HalfPi : x <= -1 ? -HalfPi : Math.Asin(x);
}
