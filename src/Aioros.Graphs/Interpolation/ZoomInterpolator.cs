namespace Aioros.Graphs.Interpolation;

/// <summary>A view of a zoomable surface: its center and the width it shows, as d3's <c>[ux, uy, w]</c>.</summary>
/// <param name="X">The center's horizontal coordinate.</param>
/// <param name="Y">The center's vertical coordinate.</param>
/// <param name="Width">The width of the view.</param>
public readonly record struct ZoomView(double X, double Y, double Width);

/// <summary>
/// The smooth zoom between two views of van Wijk and Nuij, as d3's <c>interpolateZoom</c>, with its curvature
/// <c>rho</c> (√2 by default) and the recommended duration.
/// </summary>
public readonly struct ZoomInterpolator : IInterpolator<ZoomView>
{
    private const double Epsilon2 = 1e-12;

    private readonly ZoomView start;
    private readonly double dx;
    private readonly double dy;
    private readonly double rho;
    private readonly double rho2;
    private readonly double s;
    private readonly double d1;
    private readonly double r0;
    private readonly bool near;

    /// <summary>Initializes a new instance of the <see cref="ZoomInterpolator"/> struct with d3's default curvature √2.</summary>
    /// <param name="start">The view at 0.</param>
    /// <param name="end">The view at 1.</param>
    public ZoomInterpolator(ZoomView start, ZoomView end)
        : this(start, end, Math.Sqrt(2), 2, 4)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ZoomInterpolator"/> struct, as <c>interpolateZoom.rho(rho)</c>.</summary>
    /// <param name="start">The view at 0.</param>
    /// <param name="end">The view at 1.</param>
    /// <param name="rho">The curvature, at least 0.001.</param>
    public ZoomInterpolator(ZoomView start, ZoomView end, double rho)
        : this(start, end, Math.Max(1e-3, rho), Square(Math.Max(1e-3, rho)), Square(Square(Math.Max(1e-3, rho))))
    {
    }

    private ZoomInterpolator(ZoomView start, ZoomView end, double rho, double rho2, double rho4)
    {
        this.start = start;
        this.rho = rho;
        this.rho2 = rho2;
        dx = end.X - start.X;
        dy = end.Y - start.Y;
        var d2 = dx * dx + dy * dy;
        var (w0, w1) = (start.Width, end.Width);
        if (d2 < Epsilon2)
        {
            near = true;
            s = Math.Log(w1 / w0) / rho;
        }
        else
        {
            d1 = Math.Sqrt(d2);
            var b0 = (w1 * w1 - w0 * w0 + rho4 * d2) / (2 * w0 * rho2 * d1);
            var b1 = (w1 * w1 - w0 * w0 - rho4 * d2) / (2 * w1 * rho2 * d1);
            r0 = Math.Log(Math.Sqrt(b0 * b0 + 1) - b0);
            var r1 = Math.Log(Math.Sqrt(b1 * b1 + 1) - b1);
            s = (r1 - r0) / rho;
        }

        Duration = s * 1000 * rho / Math.Sqrt(2);
    }

    /// <summary>Gets the recommended duration in milliseconds, as d3's <c>duration</c>.</summary>
    public double Duration { get; }

    /// <inheritdoc />
    public ZoomView Interpolate(double t)
    {
        if (near)
        {
            return new ZoomView(start.X + t * dx, start.Y + t * dy, start.Width * Math.Exp(rho * t * s));
        }

        var st = t * s;
        var coshr0 = Cosh(r0);
        var u = start.Width / (rho2 * d1) * (coshr0 * Tanh(rho * st + r0) - Sinh(r0));
        return new ZoomView(start.X + u * dx, start.Y + u * dy, start.Width * coshr0 / Cosh(rho * st + r0));
    }

    private static double Square(double x) => x * x;

    private static double Cosh(double x)
    {
        x = Math.Exp(x);
        return (x + 1 / x) / 2;
    }

    private static double Sinh(double x)
    {
        x = Math.Exp(x);
        return (x - 1 / x) / 2;
    }

    private static double Tanh(double x)
    {
        x = Math.Exp(2 * x);
        return (x - 1) / (x + 1);
    }
}
