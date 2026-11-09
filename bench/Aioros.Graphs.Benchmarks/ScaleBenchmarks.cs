using Aioros.Graphs.Arrays;
using Aioros.Graphs.Scales;
using BenchmarkDotNet.Attributes;

namespace Aioros.Graphs.Benchmarks;

[MemoryDiagnoser]
public class ScaleBenchmarks
{
    private const int Points = 10_000;

    private readonly double[] values = new double[Points];
    private readonly double[] output = new double[Points];
    private readonly int[] bins = new int[Points];
    private readonly double[] ticks = new double[64];
    private LogScale log = LogScale.Create([1, 10], [0, 1]);
    private PowScale pow = PowScale.Create([0, 1], [0, 1], 2);

    [GlobalSetup]
    public void Setup()
    {
        var walk = 100.0;
        for (var i = 0; i < Points; i++)
        {
            walk += Math.Sin(i * 0.37) * 0.8 + Math.Cos(i * 0.011) * 0.3;
            values[i] = walk;
        }

        log = LogScale.Create([1, 1e6], [0, 960]);
        pow = PowScale.Create([values.Min(), values.Max()], [500, 0], 2);
    }

    [Benchmark]
    public int LogTicks()
    {
        return log.Ticks(10, ticks);
    }

    [Benchmark]
    public int LogTicksAndLabels()
    {
        var n = log.Ticks(10, ticks);
        var format = log.TickFormat(10);
        var length = 0;
        for (var i = 0; i < n; i++)
        {
            length += format.Format(ticks[i]).Length;
        }

        return length;
    }

    [Benchmark]
    public double LogMap10k()
    {
        log.Map(values, output);
        return output[^1];
    }

    [Benchmark]
    public double PowMap10k()
    {
        pow.Map(values, output);
        return output[^1];
    }

    [Benchmark]
    public int BinSturges10k() => Histogram.Compute(values, bins, ThresholdRule.Sturges).Length;

    [Benchmark]
    public double Quantile10k() => Quantiles.Of(values, 0.9);
}
