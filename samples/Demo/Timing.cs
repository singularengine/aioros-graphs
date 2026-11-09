using System.Diagnostics;

namespace Aioros.Graphs.Demo;

internal sealed record ChartResult(string Id, string Svg, string[] Paths, string[] Texts, double MedianNs, long Iterations, int Samples);

internal sealed record RunResult(string Runtime, string Machine, string Date, double[] LoadAverage, ChartResult[] Charts);

internal static class Timing
{
    private const int Samples = 11;

    public static (double MedianNs, long Iterations) Measure(Func<Scene> run)
    {
        var warm = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(warm).TotalMilliseconds < 300)
        {
            run();
        }

        long iterations = 1;
        while (Elapsed(run, iterations).TotalMilliseconds < 10)
        {
            iterations *= 2;
        }

        var samples = new double[Samples];
        for (var k = 0; k < Samples; k++)
        {
            samples[k] = Elapsed(run, iterations).TotalNanoseconds / iterations;
        }

        Array.Sort(samples);
        return (samples[Samples / 2], iterations);
    }

    private static TimeSpan Elapsed(Func<Scene> run, long iterations)
    {
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < iterations; i++)
        {
            run();
        }

        return Stopwatch.GetElapsedTime(start);
    }
}
