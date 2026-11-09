using System.Text.Json;
using Aioros.Graphs.Scales;
using Aioros.Graphs.Time;

namespace Aioros.Graphs.Tests.Scales;

public sealed class UtcScaleTests
{
    [Fact]
    [Trait("AC", "SCALE-12")]
    public async Task LinearTickLabelsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale", "linearFormat"))
        {
            var scale = LinearScale.Create(item.Numbers("domain"), [0, 1]);
            var count = item.Number("count");
            var format = scale.TickFormat(count, item.OptionalText("specifier"));

            Assert.Equal(item.GetProperty("labels").EnumerateArray().Select(l => l.GetString()), scale.Ticks(count).Select(format.Format));
        }
    }

    [Fact]
    [Trait("AC", "SCALE-13")]
    public async Task MappingAndInversionMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale", "utc"))
        {
            var scale = Build(item);

            GoldenFixture.Same(item.Numbers("outputs"), [.. item.Numbers("inputs").Select(scale.Map)], item.ToString());
            foreach (var inverted in item.GetProperty("inverted").EnumerateArray())
            {
                GoldenFixture.Same(inverted.Number("x"), scale.Invert(inverted.Number("y")), $"{item} invert {inverted}");
            }
        }
    }

    [Fact]
    [Trait("AC", "SCALE-14")]
    public async Task TicksMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale", "utc"))
        {
            var scale = Build(item);
            foreach (var ticks in item.GetProperty("ticks").EnumerateArray())
            {
                GoldenFixture.Same(ticks.Numbers("ticks"), scale.Ticks(ticks.Number("count")), $"{item} ticks({ticks.Number("count")})");
                var span = new double[scale.MaxTickCount(ticks.Number("count"))];
                GoldenFixture.Same(ticks.Numbers("ticks"), span[..scale.Ticks(ticks.Number("count"), span)], $"{item} span ticks({ticks.Number("count")})");
            }

            foreach (var ticks in item.GetProperty("explicit").EnumerateArray())
            {
                var interval = ticks.Text("interval") switch
                {
                    "day" => UtcInterval.Day,
                    "hour" => UtcInterval.Hour,
                    "month" => UtcInterval.Month,
                    "minute15" => UtcInterval.Minute.Every(15)!.Value,
                    "year" => UtcInterval.Year,
                    _ => UtcInterval.Week,
                };
                GoldenFixture.Same(ticks.Numbers("ticks"), scale.Ticks(interval), $"{item} ticks({ticks.Text("interval")})");
            }
        }
    }

    [Fact]
    [Trait("AC", "SCALE-15")]
    public async Task NiceDomainsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale", "utc"))
        {
            var scale = Build(item);
            foreach (var nice in item.GetProperty("nice").EnumerateArray())
            {
                GoldenFixture.Same(nice.Numbers("domain"), scale.Nice(nice.Number("count")).Domain.ToArray(), $"{item} nice({nice.Number("count")})");
            }
        }
    }

    [Fact]
    [Trait("AC", "SCALE-16")]
    public async Task TickLabelsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale", "utc"))
        {
            foreach (var ticks in item.GetProperty("ticks").EnumerateArray())
            {
                var labels = ticks.Numbers("ticks").Select(UtcScale.TickFormat());
                Assert.Equal(ticks.GetProperty("labels").EnumerateArray().Select(l => l.GetString()), labels);
            }

            var tenTicks = item.GetProperty("ticks")[2].Numbers("ticks");
            Assert.Equal(item.GetProperty("formatted").EnumerateArray().Select(l => l.GetString()), tenTicks.Select(UtcScale.TickFormat("%Y-%m-%d %H:%M")));
        }
    }

    private static UtcScale Build(JsonElement item)
    {
        var scale = UtcScale.Create(item.Numbers("domain"), [0, 1]);
        scale = item.TryGetProperty("round", out _) ? scale.WithRangeRound(item.Numbers("range")) : scale.WithRange(item.Numbers("range"));
        return item.TryGetProperty("clamp", out _) ? scale.WithClamp(true) : scale;
    }
}
