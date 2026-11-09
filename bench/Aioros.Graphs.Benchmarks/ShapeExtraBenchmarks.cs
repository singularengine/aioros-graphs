using System.Buffers;
using Aioros.Graphs.Paths;
using Aioros.Graphs.Scales;
using Aioros.Graphs.Shapes;
using Aioros.Graphs.Svg;
using BenchmarkDotNet.Attributes;

namespace Aioros.Graphs.Benchmarks;

[MemoryDiagnoser]
public class ShapeExtraBenchmarks
{
    private const int Points = 10_000;
    private const int Marks = 1_000;

    private static readonly SymbolType[] Types = [SymbolType.Circle, SymbolType.Triangle, SymbolType.Star, SymbolType.Cross];

    private readonly ArrayBufferWriter<byte> buffer = new(1 << 20);
    private readonly double[] x = new double[Points];
    private readonly double[] y = new double[Points];
    private readonly double[] angle = new double[Points];
    private readonly double[] radius = new double[Points];

    [GlobalSetup]
    public void Setup()
    {
        var values = new double[Points];
        var index = new double[Points];
        var walk = 100.0;
        for (var i = 0; i < Points; i++)
        {
            walk += Math.Sin(i * 0.37) * 0.8 + Math.Cos(i * 0.011) * 0.3;
            values[i] = walk;
            index[i] = i;
        }

        LinearScale.Create([0, Points - 1], [0, 960]).Map(index, x);
        LinearScale.Create([values.Min(), values.Max()], [500, 0]).Nice().Map(values, y);
        LinearScale.Create([0, Points], [0, 2 * Math.PI]).Map(index, angle);
        LinearScale.Create([values.Min(), values.Max()], [50, 250]).Map(values, radius);
    }

    [Benchmark]
    public int Symbols1000ToSvg()
    {
        buffer.ResetWrittenCount();
        var writer = new SvgPathWriter(buffer);
        var context = new PathContext<SvgPathWriter>(ref writer);
        for (var i = 0; i < Marks; i++)
        {
            Symbol.Write(ref context, Types[i & 3], 64, x[i * 10], y[i * 10]);
        }

        return buffer.WrittenCount;
    }

    [Benchmark]
    public int LineRadial10kToSvg()
    {
        buffer.ResetWrittenCount();
        var writer = new SvgPathWriter(buffer);
        LineRadial.Write(ref writer, Curve.Linear, angle, radius);
        return buffer.WrittenCount;
    }

    [Benchmark]
    public int LinksHorizontal1000ToSvg()
    {
        buffer.ResetWrittenCount();
        var writer = new SvgPathWriter(buffer);
        var context = new PathContext<SvgPathWriter>(ref writer);
        for (var i = 0; i < Marks; i++)
        {
            Link.Horizontal(ref context, x[i * 10], y[i * 10], x[i * 10] + 80, y[(i * 10 + 5000) % Points]);
        }

        return buffer.WrittenCount;
    }

    [Benchmark]
    public int LineBasisClosed10kToSvg()
    {
        buffer.ResetWrittenCount();
        var writer = new SvgPathWriter(buffer);
        Line.Write(ref writer, Curve.BasisClosed, x, y);
        return buffer.WrittenCount;
    }

    [Benchmark]
    public int LineCatmullRomClosed10kToSvg()
    {
        buffer.ResetWrittenCount();
        var writer = new SvgPathWriter(buffer);
        Line.Write(ref writer, Curve.CatmullRomClosed(), x, y);
        return buffer.WrittenCount;
    }

    [Benchmark]
    public int LineBundle10kToSvg()
    {
        buffer.ResetWrittenCount();
        var writer = new SvgPathWriter(buffer);
        Line.Write(ref writer, Curve.Bundle(), x, y);
        return buffer.WrittenCount;
    }
}
