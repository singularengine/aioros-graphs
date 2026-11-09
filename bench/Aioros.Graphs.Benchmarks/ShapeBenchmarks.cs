using System.Buffers;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Scales;
using Aioros.Graphs.Shapes;
using Aioros.Graphs.Svg;
using BenchmarkDotNet.Attributes;

namespace Aioros.Graphs.Benchmarks;

[MemoryDiagnoser]
public class ShapeBenchmarks
{
    private const int Points = 10_000;

    private readonly ArrayBufferWriter<byte> buffer = new(1 << 20);
    private readonly double[] values = new double[Points];
    private readonly double[] index = new double[Points];
    private readonly double[] x = new double[Points];
    private readonly double[] y = new double[Points];
    private readonly double[] pieValues = [12, 7, 3.5, 22, 9, 1.25, 15, 4, 6, 18, 2, 11];
    private readonly PieSlice[] slices = new PieSlice[12];
    private LinearScale yScale = LinearScale.Create([0, 1], [0, 1]);

    [GlobalSetup]
    public void Setup()
    {
        var walk = 100.0;
        for (var i = 0; i < Points; i++)
        {
            walk += Math.Sin(i * 0.37) * 0.8 + Math.Cos(i * 0.011) * 0.3;
            values[i] = walk;
            index[i] = i;
        }

        var xScale = LinearScale.Create([0, Points - 1], [0, 960]);
        yScale = LinearScale.Create([values.Min(), values.Max()], [500, 0]).Nice();
        xScale.Map(index, x);
        yScale.Map(values, y);
    }

    [Benchmark]
    public int LinearTicks()
    {
        Span<double> ticks = stackalloc double[32];
        return yScale.Ticks(10, ticks);
    }

    [Benchmark]
    public double LinearMap10k()
    {
        yScale.Map(values, y);
        return y[^1];
    }

    [Benchmark]
    public int LineLinear10kToSvg()
    {
        buffer.ResetWrittenCount();
        var writer = new SvgPathWriter(buffer);
        Line.Write(ref writer, Curve.Linear, x, y);
        return buffer.WrittenCount;
    }

    [Benchmark]
    public int LineMonotoneX10kToSvg()
    {
        buffer.ResetWrittenCount();
        var writer = new SvgPathWriter(buffer);
        Line.Write(ref writer, Curve.MonotoneX, x, y);
        return buffer.WrittenCount;
    }

    [Benchmark]
    public int AreaNatural10kToSvg()
    {
        buffer.ResetWrittenCount();
        var writer = new SvgPathWriter(buffer);
        Area.Write(ref writer, Curve.Natural, x, [500], y);
        return buffer.WrittenCount;
    }

    [Benchmark]
    public int LineMonotoneX10kToCounter()
    {
        var counter = default(CountingSink);
        Line.Write(ref counter, Curve.MonotoneX, x, y);
        return counter.Commands;
    }

    [Benchmark]
    public int PieAndArcs12ToSvg()
    {
        buffer.ResetWrittenCount();
        Pie.Compute(pieValues, slices, PieOptions);
        foreach (var slice in slices)
        {
            var writer = new SvgPathWriter(buffer);
            Arc.Write(ref writer, slice.ToArc(60, 100) with { CornerRadius = 4 });
        }

        return buffer.WrittenCount;
    }

    private static readonly PieOptions PieOptions = new() { PadAngle = 0.02 };

    private struct CountingSink : IPathSink
    {
        public int Commands;

        public void MoveTo(double x, double y) => Commands++;

        public void LineTo(double x, double y) => Commands++;

        public void QuadraticCurveTo(double x1, double y1, double x, double y) => Commands++;

        public void BezierCurveTo(double x1, double y1, double x2, double y2, double x, double y) => Commands++;

        public void ArcTo(double radius, bool largeArc, bool sweep, double x, double y) => Commands++;

        public void Rect(double x, double y, double width, double height) => Commands++;

        public void ClosePath() => Commands++;
    }
}
