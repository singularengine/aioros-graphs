using System.Text.Json;
using Aioros.Graphs.Time;

namespace Aioros.Graphs.Tests.Time;

public sealed class LocalTimeTests
{
    [Fact]
    [Trait("AC", "TIME-09")]
    public async Task LocalFieldsMatchJavaScriptDatesAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "fields"))
        {
            var zone = Zone(item);
            var t = item.Number("date");
            var because = item.ToString();
            GoldenFixture.Same(item.Number("offset"), LocalCalendar.TimezoneOffset(t, zone), because);
            GoldenFixture.Same(item.Number("year"), LocalCalendar.Year(t, zone), because);
            GoldenFixture.Same(item.Number("month"), LocalCalendar.Month(t, zone), because);
            GoldenFixture.Same(item.Number("day"), LocalCalendar.Day(t, zone), because);
            GoldenFixture.Same(item.Number("weekday"), LocalCalendar.Weekday(t, zone), because);
            GoldenFixture.Same(item.Number("hours"), LocalCalendar.Hours(t, zone), because);
            GoldenFixture.Same(item.Number("minutes"), LocalCalendar.Minutes(t, zone), because);
            GoldenFixture.Same(item.Number("seconds"), LocalCalendar.Seconds(t, zone), because);
            GoldenFixture.Same(item.Number("milliseconds"), LocalCalendar.Milliseconds(t, zone), because);
        }
    }

    [Fact]
    [Trait("AC", "TIME-10")]
    public async Task LocalFieldsComposeAsJavaScriptDatesAsync()
    {
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "compose"))
        {
            var f = item.Numbers("fields");
            double Field(int i, double fallback) => i < f.Length ? f[i] : fallback;
            var actual = LocalCalendar.Compose(Zone(item), f[0], f[1], Field(2, 1), Field(3, 0), Field(4, 0), Field(5, 0), Field(6, 0));
            GoldenFixture.Same(item.Number("date"), actual, item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "TIME-11")]
    public async Task LocalIntervalsFloorCeilRoundAndOffsetAsD3Async()
    {
        double[] steps = [-3, -1, 0, 1, 2.7, 13];
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "interval"))
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
    [Trait("AC", "TIME-12")]
    public async Task LocalCountsAndRangesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "counts"))
        {
            var interval = Base(item.Text("name")).WithZone(Zone(item));
            GoldenFixture.Same(item.Number("count"), interval.Count(item.Number("start"), item.Number("end")), item.ToString());
        }

        foreach (var item in await GoldenFixture.CasesAsync("localtime", "ranges"))
        {
            var interval = Base(item.Text("name")).WithZone(Zone(item));
            var (start, stop, step) = (item.Number("start"), item.Number("stop"), item.Number("step"));
            var expected = item.Numbers("values");
            var span = new double[expected.Length + 3];
            GoldenFixture.Same(expected, interval.Range(start, stop, step), item.ToString());
            GoldenFixture.Same(expected, span[..interval.Range(start, stop, step, span)], "span " + item);
        }
    }

    [Fact]
    [Trait("AC", "TIME-13")]
    public async Task LocalTicksMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "ticks"))
        {
            var zone = Zone(item);
            var (start, stop, count) = (item.Number("start"), item.Number("stop"), item.Number("count"));
            var expected = item.Numbers("ticks");
            GoldenFixture.Same(expected, TimeTicks.Ticks(start, stop, count, zone), item.ToString());
            GoldenFixture.Same(item.Numbers("reversed"), TimeTicks.Ticks(stop, start, count, zone), "reversed " + item);
            Assert.Equal(item.GetProperty("hasInterval").GetBoolean(), TimeTicks.TickInterval(start, stop, count, zone).HasValue);
            var bound = TimeTicks.MaxCount(start, stop, count, zone);
            Assert.True(bound >= expected.Length, $"{item}: bound {bound}");
            var span = new double[Math.Min(bound, 100_000)];
            GoldenFixture.Same(expected, span[..TimeTicks.Ticks(start, stop, count, span, zone)], "span " + item);
        }
    }

    [Fact]
    [Trait("AC", "TIME-14")]
    public void TheLocalZoneIsTheDefault()
    {
        var local = TimeZoneInfo.Local;
        var t = 1_700_000_000_123.0;

        Assert.Equal(local, TimeInterval.Day.Zone);
        Assert.Equal(TimeInterval.Day.WithZone(local).Floor(t), TimeInterval.Day.Floor(t));
        Assert.Equal(TimeTicks.Ticks(t, t + 9e8, 10, local), TimeTicks.Ticks(t, t + 9e8, 10));
        Assert.Throws<ArgumentNullException>(() => TimeInterval.Day.WithZone(null!));
        Assert.Throws<ArgumentNullException>(() => LocalCalendar.Year(t, null!));
    }

    internal static TimeZoneInfo Zone(JsonElement item) => TimeZoneInfo.FindSystemTimeZoneById(item.Text("zone"));

    private static TimeInterval Interval(JsonElement item)
    {
        var step = item.Number("step");
        var interval = Base(item.Text("name")).WithZone(Zone(item));
        return step == 1 ? interval : interval.Every(step) ?? throw new InvalidOperationException(item.ToString());
    }

    private static TimeInterval Base(string name) => name switch
    {
        "second" => TimeInterval.Second,
        "minute" => TimeInterval.Minute,
        "hour" => TimeInterval.Hour,
        "day" => TimeInterval.Day,
        "week" => TimeInterval.Week,
        "monday" => TimeInterval.Monday,
        "tuesday" => TimeInterval.Tuesday,
        "wednesday" => TimeInterval.Wednesday,
        "thursday" => TimeInterval.Thursday,
        "friday" => TimeInterval.Friday,
        "saturday" => TimeInterval.Saturday,
        "month" => TimeInterval.Month,
        "year" => TimeInterval.Year,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };
}
