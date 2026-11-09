namespace Aioros.Graphs.Scales;

internal enum TransformKind : byte
{
    Identity,
    Log,
    NegativeLog,
    Pow,
    Sqrt,
    Symlog,
}

internal readonly struct ScaleTransform
{
    private readonly TransformKind kind;
    private readonly double parameter;
    private readonly double inverse;

    private ScaleTransform(TransformKind kind, double parameter, double inverse)
    {
        this.kind = kind;
        this.parameter = parameter;
        this.inverse = inverse;
    }

    public static ScaleTransform Identity => default;

    public static ScaleTransform Log(bool negative) => new(negative ? TransformKind.NegativeLog : TransformKind.Log, 0, 0);

    public static ScaleTransform Pow(double exponent) => exponent switch
    {
        1 => default,
        0.5 => new ScaleTransform(TransformKind.Sqrt, 0, 0),
        _ => new ScaleTransform(TransformKind.Pow, exponent, 1 / exponent),
    };

    public static ScaleTransform Symlog(double constant) => new(TransformKind.Symlog, constant, 0);

    public double Apply(double x) => kind switch
    {
        TransformKind.Identity => x,
        TransformKind.Log => JsMath.Log(x),
        TransformKind.NegativeLog => -JsMath.Log(-x),
        TransformKind.Pow => x < 0 ? -Math.Pow(-x, parameter) : Math.Pow(x, parameter),
        TransformKind.Sqrt => x < 0 ? -Math.Sqrt(-x) : Math.Sqrt(x),
        _ => JsMath.Sign(x) * JsMath.Log1P(Math.Abs(x / parameter)),
    };

    public double Invert(double x) => kind switch
    {
        TransformKind.Identity => x,
        TransformKind.Log => JsMath.Exp(x),
        TransformKind.NegativeLog => -JsMath.Exp(-x),
        TransformKind.Pow => x < 0 ? -Math.Pow(-x, inverse) : Math.Pow(x, inverse),
        TransformKind.Sqrt => x < 0 ? -x * x : x * x,
        _ => JsMath.Sign(x) * JsMath.ExpM1(Math.Abs(x)) * parameter,
    };
}
