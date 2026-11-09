using System.Text.Json;
using Aioros.Graphs.Scales;
using Aioros.Graphs.Tests.Time;
using Aioros.Graphs.Time;

namespace Aioros.Graphs.Tests.Scales;

public sealed class TimeScaleTests
{
    [Fact]
    [Trait("AC", "SCALE-40")]
    public async Task LocalScalesMapAndInvertAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "scale"))
        {
            var scale = Build(item);
            foreach (var output in item.GetProperty("outputs").EnumerateArray())
            {
                GoldenFixture.Same(output.Number("y"), scale.Map(output.Number("x")), $"{item} map");
                GoldenFixture.Same(output.Number("inverted"), scale.Invert(output.Number("y")), $"{item} invert");
            }
        }
    }

    [Fact]
    [Trait("AC", "SCALE-41")]
    public async Task LocalTicksMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "scale"))
        {
            var scale = Build(item);
            foreach (var ticks in item.GetProperty("counts").EnumerateArray())
            {
                var count = ticks.Number("count");
                GoldenFixture.Same(ticks.Numbers("ticks"), scale.Ticks(count), $"{item} ticks({count})");
                var span = new double[scale.MaxTickCount(count)];
                GoldenFixture.Same(ticks.Numbers("ticks"), span[..scale.Ticks(count, span)], $"{item} span ticks({count})");
            }

            foreach (var ticks in item.GetProperty("explicit").EnumerateArray())
            {
                var interval = ticks.Text("interval") switch
                {
                    "day" => TimeInterval.Day,
                    "hour" => TimeInterval.Hour,
                    "month" => TimeInterval.Month,
                    "minute15" => TimeInterval.Minute.Every(15)!.Value,
                    _ => TimeInterval.Week,
                };
                GoldenFixture.Same(ticks.Numbers("ticks"), scale.Ticks(interval.WithZone(scale.Zone)), $"{item} ticks({ticks.Text("interval")})");
            }
        }
    }

    [Fact]
    [Trait("AC", "SCALE-42")]
    public async Task LocalNiceDomainsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "scale"))
        {
            var scale = Build(item);
            foreach (var nice in item.GetProperty("nice").EnumerateArray())
            {
                GoldenFixture.Same(nice.Numbers("domain"), scale.Nice(nice.Number("count")).Domain.ToArray(), $"{item} nice({nice.Number("count")})");
            }

            GoldenFixture.Same(item.Numbers("niceInterval"), scale.Nice(TimeInterval.Day.WithZone(scale.Zone)).Domain.ToArray(), $"{item} nice(day)");
        }
    }

    [Fact]
    [Trait("AC", "SCALE-43")]
    public async Task LocalTickLabelsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("localtime", "scale"))
        {
            var scale = Build(item);
            foreach (var ticks in item.GetProperty("counts").EnumerateArray())
            {
                Assert.Equal(ticks.GetProperty("labels").EnumerateArray().Select(l => l.GetString()), ticks.Numbers("ticks").Select(scale.TickFormat()));
            }

            var tenTicks = item.GetProperty("counts")[1].Numbers("ticks");
            Assert.Equal(item.GetProperty("formatted").EnumerateArray().Select(l => l.GetString()), tenTicks.Select(scale.TickFormat("%Y-%m-%d %H:%M %Z")));
        }
    }

    private static TimeScale Build(JsonElement item) => TimeScale.Create(item.Numbers("domain"), item.Numbers("range"), LocalTimeTests.Zone(item));
}
