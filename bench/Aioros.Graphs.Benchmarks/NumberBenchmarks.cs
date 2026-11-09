using Aioros.Graphs.Formatting;
using BenchmarkDotNet.Attributes;

namespace Aioros.Graphs.Benchmarks;

[MemoryDiagnoser]
public class NumberBenchmarks
{
    private readonly double[] values = new double[1000];
    private readonly byte[] output = new byte[JsNumber.MaxShortestLength];

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = 437.123456789 * Math.Sin(i) + i * 0.731;
        }
    }

    [Benchmark]
    public int Rounded1000()
    {
        var total = 0;
        foreach (var value in values)
        {
            total += JsNumber.WriteRounded(value, 3, output);
        }

        return total;
    }

    [Benchmark]
    public int Shortest1000()
    {
        var total = 0;
        foreach (var value in values)
        {
            total += JsNumber.WriteShortest(value, output);
        }

        return total;
    }
}
