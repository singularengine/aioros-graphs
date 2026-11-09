using Aioros.Graphs.Paths;

namespace Aioros.Graphs.Shapes.Curves;

internal struct CatmullRomHistory
{
    private const double Epsilon = 1e-12;

    private readonly double alpha;
    private double x0;
    private double x1;
    private double y0;
    private double y1;
    private double l01A;
    private double l12A;
    private double l23A;
    private double l01TwoA;
    private double l12TwoA;
    private double l23TwoA;

    public CatmullRomHistory(double alpha)
    {
        this.alpha = alpha;
    }

    public double X2 { get; private set; }

    public double Y2 { get; private set; }

    public void Reset()
    {
        x0 = x1 = X2 = y0 = y1 = Y2 = double.NaN;
        l01A = l12A = l23A = l01TwoA = l12TwoA = l23TwoA = 0;
    }

    public void Measure(double x, double y)
    {
        var x23 = X2 - x;
        var y23 = Y2 - y;
        l23TwoA = Math.Pow(x23 * x23 + y23 * y23, alpha);
        l23A = Math.Sqrt(l23TwoA);
    }

    public void Shift(double x, double y)
    {
        l01A = l12A;
        l12A = l23A;
        l01TwoA = l12TwoA;
        l12TwoA = l23TwoA;
        x0 = x1;
        x1 = X2;
        X2 = x;
        y0 = y1;
        y1 = Y2;
        Y2 = y;
    }

    public readonly void Segment<TSink>(ref PathContext<TSink> context, double x, double y)
        where TSink : IPathSink
    {
        var (cx1, cy1, cx2, cy2) = (x1, y1, X2, Y2);
        if (l01A > Epsilon)
        {
            var a = 2 * l01TwoA + 3 * l01A * l12A + l12TwoA;
            var n = 3 * l01A * (l01A + l12A);
            cx1 = (cx1 * a - x0 * l12TwoA + X2 * l01TwoA) / n;
            cy1 = (cy1 * a - y0 * l12TwoA + Y2 * l01TwoA) / n;
        }

        if (l23A > Epsilon)
        {
            var b = 2 * l23TwoA + 3 * l23A * l12A + l12TwoA;
            var m = 3 * l23A * (l23A + l12A);
            cx2 = (cx2 * b + x1 * l23TwoA - x * l12TwoA) / m;
            cy2 = (cy2 * b + y1 * l23TwoA - y * l12TwoA) / m;
        }

        context.BezierCurveTo(cx1, cy1, cx2, cy2, X2, Y2);
    }
}
