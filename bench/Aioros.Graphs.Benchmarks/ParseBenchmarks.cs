using Aioros.Graphs.Arrays;
using Aioros.Graphs.Formatting;
using Aioros.Graphs.Interpolation;
using BenchmarkDotNet.Attributes;

namespace Aioros.Graphs.Benchmarks;

[MemoryDiagnoser]
public class ParseBenchmarks
{
    private const int Dates = 1_000;
    private const int Rows = 10_000;
    private const double Start = 1_577_836_800_000;

    private readonly string[] utcTexts = new string[Dates];
    private readonly string[] localTexts = new string[Dates];
    private readonly string[] categories = new string[Rows];
    private readonly string[] subcategories = new string[Rows];
    private readonly double[] values = new double[Rows];
    private readonly double[] sums = new double[Rows];
    private readonly char[] text = new char[256];
    private readonly UtcTimeParse utc = UtcTimeParse.Create("%Y-%m-%dT%H:%M:%SZ");
    private readonly TimeParse local = TimeParse.Create("%Y-%m-%d %H:%M", TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));
    private readonly StringInterpolator path = new("M0,0L10,10C1,2,3,4,5,6Z", "M5,5L20,30C7,8,9,10,11,12Z");

    [GlobalSetup]
    public void Setup()
    {
        var iso = UtcTimeFormat.Create("%Y-%m-%dT%H:%M:%SZ");
        var wall = UtcTimeFormat.Create("%Y-%m-%d %H:%M");
        for (var i = 0; i < Dates; i++)
        {
            utcTexts[i] = iso.Format(Start + i * 3_600_123.0);
            localTexts[i] = wall.Format(Start + i * 3_600_123.0);
        }

        for (var i = 0; i < Rows; i++)
        {
            categories[i] = "c" + (i * 7 % 12);
            subcategories[i] = "s" + (i * 13 % 7);
            values[i] = i * 37 % 101 / 7.0;
        }
    }

    [Benchmark]
    public double UtcParse1000()
    {
        var total = 0.0;
        foreach (var t in utcTexts)
        {
            if (utc.TryParse(t, out var ms))
            {
                total += ms;
            }
        }

        return total;
    }

    [Benchmark]
    public double LocalParse1000()
    {
        var total = 0.0;
        foreach (var t in localTexts)
        {
            if (local.TryParse(t, out var ms))
            {
                total += ms;
            }
        }

        return total;
    }

    [Benchmark]
    public int InterpolateString1000()
    {
        var total = 0;
        for (var i = 0; i < 1000; i++)
        {
            total += path.Interpolate(i / 999.0).Length;
        }

        return total;
    }

    [Benchmark]
    public int InterpolateString1000ToSpan()
    {
        var total = 0;
        for (var i = 0; i < 1000; i++)
        {
            path.TryInterpolate(i / 999.0, text, out var written);
            total += written;
        }

        return total;
    }

    [Benchmark]
    public int NestedRollup10k() => Grouping.RollupNested<string, SumReducer>(values, default, categories, subcategories).Length;

    [Benchmark]
    public double FCumulativeSum10k()
    {
        Statistics.FCumulativeSum(values, sums);
        return sums[^1];
    }
}
