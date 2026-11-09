using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Chords;

/// <summary>
/// The ribbon generators of d3-chord: the band of a chord between its two ends, centered on the origin, and its
/// arrow form. The overloads over a <see cref="PathContext{TSink}"/> let many ribbons share one path.
/// </summary>
public static class Ribbon
{
    /// <summary>The head radius of d3's <c>ribbonArrow</c>.</summary>
    public const double DefaultHeadRadius = 10;

    private const double Epsilon = 1e-12;
    private const double HalfPi = Math.PI / 2;

    /// <summary>Writes <paramref name="ribbon"/> into a new path, as d3's <c>ribbon()</c>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="ribbon">The ribbon's ends.</param>
    public static void Write<TSink>(ref TSink sink, in RibbonShape ribbon)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        Write(ref context, ribbon);
    }

    /// <summary>Writes <paramref name="ribbon"/> into <paramref name="context"/>, as d3's <c>ribbon()</c>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="ribbon">The ribbon's ends.</param>
    public static void Write<TSink>(ref PathContext<TSink> context, in RibbonShape ribbon)
        where TSink : IPathSink => Draw(ref context, ribbon, false, 0);

    /// <summary>Writes <paramref name="ribbon"/> as an arrow into a new path, as d3's <c>ribbonArrow()</c>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="sink">Where the path goes.</param>
    /// <param name="ribbon">The ribbon's ends.</param>
    /// <param name="headRadius">The depth of the arrowhead into the target's radius.</param>
    public static void WriteArrow<TSink>(ref TSink sink, in RibbonShape ribbon, double headRadius = DefaultHeadRadius)
        where TSink : IPathSink
    {
        var context = new PathContext<TSink>(ref sink);
        WriteArrow(ref context, ribbon, headRadius);
    }

    /// <summary>Writes <paramref name="ribbon"/> as an arrow into <paramref name="context"/>, as d3's <c>ribbonArrow()</c>.</summary>
    /// <typeparam name="TSink">The sink.</typeparam>
    /// <param name="context">The path to continue.</param>
    /// <param name="ribbon">The ribbon's ends.</param>
    /// <param name="headRadius">The depth of the arrowhead into the target's radius.</param>
    public static void WriteArrow<TSink>(ref PathContext<TSink> context, in RibbonShape ribbon, double headRadius = DefaultHeadRadius)
        where TSink : IPathSink => Draw(ref context, ribbon, true, headRadius);

    private static void Draw<TSink>(ref PathContext<TSink> context, in RibbonShape ribbon, bool arrow, double headRadius)
        where TSink : IPathSink
    {
        var ap = ribbon.PadAngle / 2;
        var sr = ribbon.SourceRadius;
        var sa0 = ribbon.SourceStartAngle - HalfPi;
        var sa1 = ribbon.SourceEndAngle - HalfPi;
        var tr = ribbon.TargetRadius;
        var ta0 = ribbon.TargetStartAngle - HalfPi;
        var ta1 = ribbon.TargetEndAngle - HalfPi;
        if (ap > Epsilon)
        {
            Pad(ref sa0, ref sa1, ap);
            Pad(ref ta0, ref ta1, ap);
        }

        context.MoveTo(sr * Math.Cos(sa0), sr * Math.Sin(sa0));
        context.Arc(0, 0, sr, sa0, sa1);
        if (sa0 != ta0 || sa1 != ta1)
        {
            if (arrow)
            {
                var tr2 = tr - headRadius;
                var ta2 = (ta0 + ta1) / 2;
                context.QuadraticCurveTo(0, 0, tr2 * Math.Cos(ta0), tr2 * Math.Sin(ta0));
                context.LineTo(tr * Math.Cos(ta2), tr * Math.Sin(ta2));
                context.LineTo(tr2 * Math.Cos(ta1), tr2 * Math.Sin(ta1));
            }
            else
            {
                context.QuadraticCurveTo(0, 0, tr * Math.Cos(ta0), tr * Math.Sin(ta0));
                context.Arc(0, 0, tr, ta0, ta1);
            }
        }

        context.QuadraticCurveTo(0, 0, sr * Math.Cos(sa0), sr * Math.Sin(sa0));
        context.ClosePath();
    }

    private static void Pad(ref double a0, ref double a1, double ap)
    {
        if (Math.Abs(a1 - a0) > ap * 2 + Epsilon)
        {
            if (a1 > a0)
            {
                a0 += ap;
                a1 -= ap;
            }
            else
            {
                a0 -= ap;
                a1 += ap;
            }
        }
        else
        {
            a0 = a1 = (a0 + a1) / 2;
        }
    }
}
