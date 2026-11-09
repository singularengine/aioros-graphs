using Aioros.Graphs.Arrays;

namespace Aioros.Graphs.Scales;

internal enum DomainKind : byte
{
    Linear,
    Log,
    Pow,
    Symlog,
}

internal readonly struct InterpolatedDomain
{
    private readonly DomainKind kind;
    private readonly double parameter;
    private readonly LogBase logBase;

    public InterpolatedDomain(double[] stops, DomainKind kind, double parameter)
    {
        Stops = stops;
        this.kind = kind;
        this.parameter = parameter;
        var negative = stops[0] < 0;
        logBase = new LogBase(kind == DomainKind.Log ? parameter : 10, negative);
        Transform = kind switch
        {
            DomainKind.Log => ScaleTransform.Log(negative),
            DomainKind.Pow => ScaleTransform.Pow(parameter),
            DomainKind.Symlog => ScaleTransform.Symlog(parameter),
            _ => ScaleTransform.Identity,
        };
    }

    public double[] Stops { get; }

    public ScaleTransform Transform { get; }

    public static double[] Check(ReadOnlySpan<double> stops, int count, string name)
    {
        if (stops.Length != count)
        {
            throw new ArgumentException($"The domain needs exactly {count} stops.", name);
        }

        return stops.ToArray();
    }

    public InterpolatedDomain With(double[] stops) => new(stops, kind, parameter);

    public double[] Ticks(double count) =>
        kind == DomainKind.Log ? logBase.Ticks(Stops[0], Stops[^1], count) : Arrays.Ticks.Compute(Stops[0], Stops[^1], count);

    public int Ticks(double count, Span<double> destination) =>
        kind == DomainKind.Log ? logBase.Ticks(Stops[0], Stops[^1], count, destination) : Arrays.Ticks.Compute(Stops[0], Stops[^1], count, destination);

    public TickLabelFormat TickFormat(double count, string? specifier) =>
        kind == DomainKind.Log
            ? logBase.TickFormat(Stops[0], Stops[^1], count, specifier)
            : new TickLabelFormat(Formatting.TickFormat.Create(Stops[0], Stops[^1], count, specifier));

    public double[]? Nice(double count) => kind == DomainKind.Log ? logBase.Nice(Stops) : LinearNice.Stops(Stops, count);
}
