using System.Text.Json;
using Aioros.Graphs.Formatting;
using Aioros.Graphs.Scales;
using Aioros.Graphs.Time;

namespace Aioros.Graphs.Tests.Time;

public sealed class UtcTimeTests
{
    [Fact]
    [Trait("AC", "TIME-01")]
    public async Task CalendarFieldsMatchJavaScriptDatesAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("time", "fields"))
        {
            var t = item.Number("date");
            var because = item.ToString();
            GoldenFixture.Same(item.Number("year"), UtcCalendar.Year(t), because);
            GoldenFixture.Same(item.Number("month"), UtcCalendar.Month(t), because);
            GoldenFixture.Same(item.Number("day"), UtcCalendar.Day(t), because);
            GoldenFixture.Same(item.Number("weekday"), UtcCalendar.Weekday(t), because);
            GoldenFixture.Same(item.Number("hours"), UtcCalendar.Hours(t), because);
            GoldenFixture.Same(item.Number("minutes"), UtcCalendar.Minutes(t), because);
            GoldenFixture.Same(item.Number("seconds"), UtcCalendar.Seconds(t), because);
            GoldenFixture.Same(item.Number("milliseconds"), UtcCalendar.Milliseconds(t), because);
            var composed = UtcCalendar.Compose(UtcCalendar.Year(t), UtcCalendar.Month(t) + 14, UtcCalendar.Day(t) - 40, UtcCalendar.Hours(t) + 30, -5, 70, 1500);
            GoldenFixture.Same(item.Number("composed"), composed, because);
        }
    }

    [Fact]
    [Trait("AC", "TIME-02")]
    public async Task IntervalsFloorCeilRoundAndOffsetAsD3Async()
    {
        double[] steps = [-3, -1, 0, 1, 2.7, 13];
        foreach (var item in await GoldenFixture.CasesAsync("time", "interval"))
        {
            var interval = Interval(item);
            var t = item.Number("date");
            var because = item.ToString();
            GoldenFixture.Same(item.Number("floor"), interval.Floor(t), "floor " + because);
            GoldenFixture.Same(item.Number("ceil"), interval.Ceil(t), "ceil " + because);
            GoldenFixture.Same(item.Number("round"), interval.Round(t), "round " + because);
            GoldenFixture.Same(item.Numbers("offset"), [.. steps.Select(k => interval.Offset(t, k))], "offset " + because);
        }
    }

    [Fact]
    [Trait("AC", "TIME-03")]
    public async Task EveryReturnsNoIntervalWhereD3ReturnsNullAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("time", "every"))
        {
            Assert.True(item.GetProperty("exists").GetBoolean() == Base(item.Text("name")).Every(item.Number("step")).HasValue, item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "TIME-04")]
    public async Task CountsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("time", "counts"))
        {
            GoldenFixture.Same(item.Number("count"), Base(item.Text("name")).Count(item.Number("start"), item.Number("end")), item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "TIME-05")]
    public async Task RangesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("time", "ranges"))
        {
            var interval = Base(item.Text("name"));
            var (start, stop, step) = (item.Number("start"), item.Number("stop"), item.Number("step"));
            var expected = item.Numbers("values");
            var span = new double[expected.Length + 3];

            GoldenFixture.Same(expected, interval.Range(start, stop, step), item.ToString());
            GoldenFixture.Same(expected, span[..interval.Range(start, stop, step, span)], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "TIME-06")]
    public async Task TicksMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("time", "ticks"))
        {
            var (start, stop, count) = (item.Number("start"), item.Number("stop"), item.Number("count"));

            var expected = item.Numbers("ticks");
            GoldenFixture.Same(expected, UtcTicks.Ticks(start, stop, count), item.ToString());
            Assert.Equal(item.GetProperty("hasInterval").GetBoolean(), UtcTicks.TickInterval(start, stop, count).HasValue);
            var bound = UtcTicks.MaxCount(start, stop, count);
            Assert.True(bound >= expected.Length, $"{item}: bound {bound}");
            var span = new double[Math.Min(bound, 100_000)];
            GoldenFixture.Same(expected, span[..UtcTicks.Ticks(start, stop, count, span)], "span " + item);
        }
    }

    [Fact]
    [Trait("AC", "TIME-07")]
    public async Task NiceDomainsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("time", "nice"))
        {
            var (start, stop) = UtcTicks.Nice(item.Number("start"), item.Number("stop"), item.Number("count"));

            GoldenFixture.Same(item.Numbers("domain"), [start, stop], item.ToString());
        }
    }

    private static UtcInterval Interval(JsonElement item)
    {
        var step = item.Number("step");
        var interval = Base(item.Text("name"));
        return step == 1 ? interval : interval.Every(step) ?? throw new InvalidOperationException(item.ToString());
    }

    private static UtcInterval Base(string name) => name switch
    {
        "millisecond" => UtcInterval.Millisecond,
        "second" => UtcInterval.Second,
        "minute" => UtcInterval.Minute,
        "hour" => UtcInterval.Hour,
        "day" => UtcInterval.Day,
        "unixDay" => UtcInterval.UnixDay,
        "week" => UtcInterval.Week,
        "monday" => UtcInterval.Monday,
        "tuesday" => UtcInterval.Tuesday,
        "wednesday" => UtcInterval.Wednesday,
        "thursday" => UtcInterval.Thursday,
        "friday" => UtcInterval.Friday,
        "saturday" => UtcInterval.Saturday,
        "month" => UtcInterval.Month,
        "year" => UtcInterval.Year,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    [Fact]
    [Trait("AC", "TIME-08")]
    public void TicksAndLabelsAreWrittenWithoutAllocating()
    {
        var year = UtcScale.Create([Date(2024, 1, 1), Date(2025, 1, 1)], [0, 960]);
        var twoYears = UtcScale.Create([Date(2024, 1, 1), Date(2025, 12, 31)], [0, 960]);
        var format = UtcTimeFormat.Create("%Y-%m-%d");
        WriteAxis(year, format, 8);
        WriteAxis(twoYears, format, 10);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var written = WriteAxis(year, format, 8) + WriteAxis(twoYears, format, 10);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(written > 0);
        Assert.Equal(0, allocated);
    }

    private static int WriteAxis(UtcScale scale, UtcTimeFormat format, double count)
    {
        Span<double> ticks = stackalloc double[64];
        Span<char> label = stackalloc char[64];
        var total = 0;
        var n = scale.Ticks(count, ticks);
        foreach (var tick in ticks[..n])
        {
            total += UtcTimeFormat.TryMultiScale(tick, label, out var a) ? a : 0;
            total += format.TryFormat(tick, label, out var b) ? b : 0;
        }

        return total;
    }

    private static double Date(int year, int month, int day) => new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
}
