using Aioros.Graphs.Formatting;

namespace Aioros.Graphs.Interpolation;

internal readonly struct Channel
{
    private readonly ChannelKind kind;
    private readonly double a;
    private readonly double b;
    private readonly double y;

    private Channel(ChannelKind kind, double a, double b, double y)
    {
        this.kind = kind;
        this.a = a;
        this.b = b;
        this.y = y;
    }

    private enum ChannelKind : byte
    {
        Constant,
        Linear,
        Exponential,
    }

    public static Channel NoGamma(double a, double b)
    {
        var d = b - a;
        return IsTrue(d) ? new Channel(ChannelKind.Linear, a, d, 1) : Constant(a, b);
    }

    public static Channel Gamma(double a, double b, double gamma)
    {
        if (gamma == 1)
        {
            return NoGamma(a, b);
        }

        if (!IsTrue(b - a))
        {
            return Constant(a, b);
        }

        var start = Math.Pow(a, gamma);
        return new Channel(ChannelKind.Exponential, start, Math.Pow(b, gamma) - start, 1 / gamma);
    }

    public static Channel Hue(double a, double b)
    {
        var d = b - a;
        if (!IsTrue(d))
        {
            return Constant(a, b);
        }

        return new Channel(ChannelKind.Linear, a, d > 180 || d < -180 ? d - 360 * JsNumber.Round(d / 360) : d, 1);
    }

    public double At(double t) => kind switch
    {
        ChannelKind.Linear => a + t * b,
        ChannelKind.Exponential => Math.Pow(a + t * b, y),
        _ => a,
    };

    private static Channel Constant(double a, double b) => new(ChannelKind.Constant, double.IsNaN(a) ? b : a, 0, 1);

    private static bool IsTrue(double d) => d != 0 && !double.IsNaN(d);
}
