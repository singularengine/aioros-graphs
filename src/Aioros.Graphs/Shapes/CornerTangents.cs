namespace Aioros.Graphs.Shapes;

internal readonly record struct CornerTangents(double Cx, double Cy, double X01, double Y01, double X11, double Y11)
{
    public static CornerTangents Of(double x0, double y0, double x1, double y1, double r1, double rc, bool cw)
    {
        var x01 = x0 - x1;
        var y01 = y0 - y1;
        var lo = (cw ? rc : -rc) / Math.Sqrt(x01 * x01 + y01 * y01);
        var ox = lo * y01;
        var oy = -lo * x01;
        var x11 = x0 + ox;
        var y11 = y0 + oy;
        var x10 = x1 + ox;
        var y10 = y1 + oy;
        var x00 = (x11 + x10) / 2;
        var y00 = (y11 + y10) / 2;
        var dx = x10 - x11;
        var dy = y10 - y11;
        var d2 = dx * dx + dy * dy;
        var r = r1 - rc;
        var cross = x11 * y10 - x10 * y11;
        var d = (dy < 0 ? -1 : 1) * Math.Sqrt(Math.Max(0, r * r * d2 - cross * cross));
        var cx0 = (cross * dy - dx * d) / d2;
        var cy0 = (-cross * dx - dy * d) / d2;
        var cx1 = (cross * dy + dx * d) / d2;
        var cy1 = (-cross * dx + dy * d) / d2;
        var dx0 = cx0 - x00;
        var dy0 = cy0 - y00;
        var dx1 = cx1 - x00;
        var dy1 = cy1 - y00;
        if (dx0 * dx0 + dy0 * dy0 > dx1 * dx1 + dy1 * dy1)
        {
            (cx0, cy0) = (cx1, cy1);
        }

        return new CornerTangents(cx0, cy0, -ox, -oy, cx0 * (r1 / r - 1), cy0 * (r1 / r - 1));
    }
}
