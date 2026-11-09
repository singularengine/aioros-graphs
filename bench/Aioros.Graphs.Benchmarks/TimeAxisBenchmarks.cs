using Aioros.Graphs.Formatting;
using Aioros.Graphs.Scales;
using BenchmarkDotNet.Attributes;

namespace Aioros.Graphs.Benchmarks;

[MemoryDiagnoser]
public class TimeAxisBenchmarks
{
    private static readonly double Start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
    private static readonly double YearEnd = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
    private static readonly double TwoYearsEnd = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

    private readonly UtcScale year = UtcScale.Create([Start, YearEnd], [0, 960]);
    private readonly UtcScale twoYears = UtcScale.Create([Start, TwoYearsEnd], [0, 960]);
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private readonly NumberFormat linearFormat = TickFormat.Create(0, 1234, 10);
    private readonly TimeScale localYear = TimeScale.Create([Start, YearEnd], [0, 960], Berlin);
    private double[] yearTicks = [];
    private double[] localTicks = [];

    [GlobalSetup]
    public void Setup()
    {
        yearTicks = year.Ticks(8);
        localTicks = localYear.Ticks(8);
    }

    [Benchmark]
    public int UtcTicksYear8() => year.Ticks(8).Length;

    [Benchmark]
    public int UtcTicksTwoYears10() => twoYears.Ticks(10).Length;

    [Benchmark]
    public int MultiScaleLabels13()
    {
        var total = 0;
        foreach (var tick in yearTicks)
        {
            total += UtcTimeFormat.MultiScale(tick).Length;
        }

        return total;
    }

    [Benchmark]
    public int LocalTicksYear8() => localYear.Ticks(8).Length;

    [Benchmark]
    public int LocalMultiScaleLabels()
    {
        var total = 0;
        foreach (var tick in localTicks)
        {
            total += TimeFormat.MultiScale(tick, Berlin).Length;
        }

        return total;
    }

    [Benchmark]
    public UtcTimeFormat UtcFormatCreate() => UtcTimeFormat.Create("%Y-%m-%d");

    [Benchmark]
    public NumberFormat LinearTickFormatCreate() => TickFormat.Create(0, 1234, 10);

    [Benchmark]
    public NumberFormat LinearTickFormatCreateSi() => TickFormat.Create(0, 1234, 10, ".2~s");

    [Benchmark]
    public NumberFormat LinearTickFormatCreateCurrency() => TickFormat.Create(0, 1234, 10, "$,f");

    [Benchmark]
    public int LinearTickLabel() => linearFormat.Format(1200).Length;
}
