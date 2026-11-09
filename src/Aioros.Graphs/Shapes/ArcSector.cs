using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes;

internal readonly struct ArcSector
{
    private const double Epsilon = Arc.Epsilon;

    private readonly double r0;
    private readonly double r1;
    private readonly bool cw;
    private readonly double a01;
    private readonly double a11;
    private readonly double a00;
    private readonly double a10;
    private readonly double da0;
    private readonly double da1;
    private readonly double rc;
    private readonly double rc0;
    private readonly double rc1;
    private readonly double x01;
    private readonly double y01;
    private readonly double x10;
    private readonly double y10;
    private readonly double x11;
    private readonly double y11;
    private readonly double x00;
    private readonly double y00;

    public ArcSector(double r0, double r1, double a0, double a1, double da, bool cw, in ArcShape arc)
    {
        this.r0 = r0;
        this.r1 = r1;
        this.cw = cw;
        (a01, a11, a00, a10, da0, da1) = (a0, a1, a0, a1, da, da);
        var ap = arc.PadAngle / 2;
        var rp = ap > Epsilon ? (double.IsNaN(arc.PadRadius) ? Math.Sqrt(r0 * r0 + r1 * r1) : arc.PadRadius) : 0;
        rc = Math.Min(Math.Abs(r1 - r0) / 2, arc.CornerRadius);
        (rc0, rc1) = (rc, rc);
        if (rp > Epsilon)
        {
            (a00, a10, da0) = Pad(a0, a1, da, Arc.Asin(rp / r0 * Math.Sin(ap)), cw);
            (a01, a11, da1) = Pad(a0, a1, da, Arc.Asin(rp / r1 * Math.Sin(ap)), cw);
        }

        x01 = r1 * Math.Cos(a01);
        y01 = r1 * Math.Sin(a01);
        x10 = r0 * Math.Cos(a10);
        y10 = r0 * Math.Sin(a10);
        if (rc > Epsilon)
        {
            x11 = r1 * Math.Cos(a11);
            y11 = r1 * Math.Sin(a11);
            x00 = r0 * Math.Cos(a00);
            y00 = r0 * Math.Sin(a00);
            if (da < Math.PI)
            {
                (rc0, rc1) = LimitCorners();
            }
        }
    }

    public void Write<TSink>(ref PathContext<TSink> context)
        where TSink : IPathSink
    {
        if (!(da1 > Epsilon))
        {
            context.MoveTo(x01, y01);
        }
        else if (rc1 > Epsilon)
        {
            var t0 = CornerTangents.Of(x00, y00, x01, y01, r1, rc1, cw);
            var t1 = CornerTangents.Of(x11, y11, x10, y10, r1, rc1, cw);
            context.MoveTo(t0.Cx + t0.X01, t0.Cy + t0.Y01);
            Corners(ref context, t0, t1, rc1, r1, !cw);
        }
        else
        {
            context.MoveTo(x01, y01);
            context.Arc(0, 0, r1, a01, a11, !cw);
        }

        if (!(r0 > Epsilon) || !(da0 > Epsilon))
        {
            context.LineTo(x10, y10);
        }
        else if (rc0 > Epsilon)
        {
            var t0 = CornerTangents.Of(x10, y10, x11, y11, r0, -rc0, cw);
            var t1 = CornerTangents.Of(x01, y01, x00, y00, r0, -rc0, cw);
            context.LineTo(t0.Cx + t0.X01, t0.Cy + t0.Y01);
            Corners(ref context, t0, t1, rc0, r0, cw);
        }
        else
        {
            context.Arc(0, 0, r0, a10, a00, cw);
        }
    }

    private static (double Start, double End, double Span) Pad(double a0, double a1, double da, double p, bool cw)
    {
        var span = da - p * 2;
        if (span > Epsilon)
        {
            p *= cw ? 1 : -1;
            return (a0 + p, a1 - p, span);
        }

        var middle = (a0 + a1) / 2;
        return (middle, middle, 0);
    }

    private static bool Intersect(double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3, out double x, out double y)
    {
        var x10 = x1 - x0;
        var y10 = y1 - y0;
        var x32 = x3 - x2;
        var y32 = y3 - y2;
        var t = y32 * x10 - x32 * y10;
        if (t * t < Epsilon)
        {
            x = y = 0;
            return false;
        }

        t = (x32 * (y0 - y2) - y32 * (x0 - x2)) / t;
        x = x0 + t * x10;
        y = y0 + t * y10;
        return true;
    }

    private (double Inner, double Outer) LimitCorners()
    {
        if (!Intersect(x01, y01, x00, y00, x11, y11, x10, y10, out var ocx, out var ocy))
        {
            return (0, 0);
        }

        var ax = x01 - ocx;
        var ay = y01 - ocy;
        var bx = x11 - ocx;
        var by = y11 - ocy;
        var kc = 1 / Math.Sin(Arc.Acos((ax * bx + ay * by) / (Math.Sqrt(ax * ax + ay * ay) * Math.Sqrt(bx * bx + by * by))) / 2);
        var lc = Math.Sqrt(ocx * ocx + ocy * ocy);
        return (Math.Min(rc, (r0 - lc) / (kc - 1)), Math.Min(rc, (r1 - lc) / (kc + 1)));
    }

    private void Corners<TSink>(ref PathContext<TSink> context, in CornerTangents t0, in CornerTangents t1, double corner, double ring, bool ringCounterclockwise)
        where TSink : IPathSink
    {
        if (corner < rc)
        {
            context.Arc(t0.Cx, t0.Cy, corner, Math.Atan2(t0.Y01, t0.X01), Math.Atan2(t1.Y01, t1.X01), !cw);
            return;
        }

        context.Arc(t0.Cx, t0.Cy, corner, Math.Atan2(t0.Y01, t0.X01), Math.Atan2(t0.Y11, t0.X11), !cw);
        context.Arc(0, 0, ring, Math.Atan2(t0.Cy + t0.Y11, t0.Cx + t0.X11), Math.Atan2(t1.Cy + t1.Y11, t1.Cx + t1.X11), ringCounterclockwise);
        context.Arc(t1.Cx, t1.Cy, corner, Math.Atan2(t1.Y11, t1.X11), Math.Atan2(t1.Y01, t1.X01), !cw);
    }
}
