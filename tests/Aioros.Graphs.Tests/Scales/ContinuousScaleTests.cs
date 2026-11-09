using System.Text.Json;
using Aioros.Graphs.Scales;

namespace Aioros.Graphs.Tests.Scales;

public sealed class ContinuousScaleTests
{
    [Fact]
    [Trait("AC", "SCALE-17")]
    public async Task LogScalesMapAndInvertAsD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-continuous", "log"))
        {
            var scale = Log(item);
            var inputs = item.Numbers("inputs");
            var batch = new double[inputs.Length];
            scale.Map(inputs, batch);

            GoldenFixture.Same(item.Numbers("outputs"), [.. inputs.Select(scale.Map)], item.ToString());
            GoldenFixture.Same(item.Numbers("outputs"), batch, item.ToString());
            GoldenFixture.Same(item.Numbers("inverted"), [.. inputs.Select(scale.Invert)], item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "SCALE-18")]
    public async Task LogTicksMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-continuous", "log"))
        {
            var scale = Log(item);
            foreach (var ticks in item.GetProperty("ticks").EnumerateArray())
            {
                var count = ticks.Number("count");
                var expected = ticks.Numbers("ticks");
                var span = new double[expected.Length + 3];

                GoldenFixture.Same(expected, scale.Ticks(count), $"{item} count {count}");
                Assert.Equal(expected.Length, scale.Ticks(count, span));
                GoldenFixture.Same(expected, span[..expected.Length], $"{item} count {count}");
            }
        }
    }

    [Fact]
    [Trait("AC", "SCALE-19")]
    public async Task LogTickFormatsMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-continuous", "log"))
        {
            var scale = Log(item);
            AssertFormats(item, (count, specifier) => scale.TickFormat(count, specifier).Format);
        }
    }

    [Fact]
    [Trait("AC", "SCALE-20")]
    public async Task LogNiceMatchesD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-continuous", "log"))
        {
            GoldenFixture.Same(item.Numbers("niceDomain"), Log(item).Domain.ToArray(), item.ToString());
        }
    }

    [Fact]
    [Trait("AC", "SCALE-21")]
    public async Task PowAndSqrtScalesMatchD3Async()
    {
        foreach (var group in new[] { "pow", "sqrt" })
        {
            foreach (var item in await GoldenFixture.CasesAsync("scale-continuous", group))
            {
                var scale = PowScale.Create(item.Numbers("domain"), [0, 1], item.OptionalNumber("exponent") ?? 0.5);
                scale = Configure(item, r => scale.WithRange(r), r => scale.WithRangeRound(r), s => s.WithClamp(true), (s, n) => s.Nice(n));
                AssertContinuous(item, new Adapter(scale.Map, scale.Invert, scale.Ticks, (c, s) => scale.TickFormat(c, s).Format, scale.Domain.ToArray()));
            }
        }
    }

    [Fact]
    [Trait("AC", "SCALE-22")]
    public async Task SymlogScalesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-continuous", "symlog"))
        {
            var scale = SymlogScale.Create(item.Numbers("domain"), [0, 1], item.OptionalNumber("constant") ?? 1);
            scale = Configure(item, r => scale.WithRange(r), r => scale.WithRangeRound(r), s => s.WithClamp(true), (s, n) => s.Nice(n));
            AssertContinuous(item, new Adapter(scale.Map, scale.Invert, scale.Ticks, (c, s) => scale.TickFormat(c, s).Format, scale.Domain.ToArray()));
        }
    }

    [Fact]
    [Trait("AC", "SCALE-23")]
    public async Task RadialScalesMatchD3Async()
    {
        foreach (var item in await GoldenFixture.CasesAsync("scale-continuous", "radial"))
        {
            var scale = RadialScale.Create(item.Numbers("domain"), [0, 1]);
            scale = Configure(item, r => scale.WithRange(r), r => scale.WithRangeRound(r), s => s.WithClamp(true), (s, n) => s.Nice(n));
            AssertContinuous(item, new Adapter(scale.Map, scale.Invert, scale.Ticks, (c, s) => scale.TickFormat(c, s).Format, scale.Domain.ToArray()));
        }
    }

    [Fact]
    [Trait("AC", "SCALE-24")]
    public void ContinuousScalesNeedTwoStopsAndMapNaNToNaN()
    {
        Assert.Throws<ArgumentException>(() => LogScale.Create([1], [0, 1]));
        Assert.Throws<ArgumentException>(() => PowScale.Create([0, 1], [0], 2));
        Assert.Throws<ArgumentException>(() => SymlogScale.Create([], [0, 1]));
        Assert.Throws<ArgumentException>(() => RadialScale.Create([0, 1], [5]));
        Assert.True(double.IsNaN(LogScale.Create([1, 10], [0, 1]).Map(double.NaN)));
        Assert.True(double.IsNaN(PowScale.Create([0, 1], [0, 1], 2).WithClamp(true).Map(double.NaN)));
        Assert.True(double.IsNaN(SymlogScale.Create([0, 1], [0, 1]).Map(double.NaN)));
        Assert.True(double.IsNaN(RadialScale.Create([0, 1], [0, 1]).WithRangeRound([0, 10]).Map(double.NaN)));
    }

    private static LogScale Log(JsonElement item)
    {
        var scale = LogScale.Create(item.Numbers("domain"), [0, 1]).WithBase(item.OptionalNumber("base") ?? 10);
        return Configure(item, r => scale.WithRange(r), r => scale.WithRangeRound(r), s => s.WithClamp(true), (s, _) => s.Nice());
    }

    private static T Configure<T>(JsonElement item, Func<double[], T> range, Func<double[], T> rangeRound, Func<T, T> clamp, Func<T, double, T> nice)
    {
        var scale = item.TryGetProperty("round", out _) ? rangeRound(item.Numbers("range")) : range(item.Numbers("range"));
        if (item.TryGetProperty("clamp", out _))
        {
            scale = clamp(scale);
        }

        if (item.TryGetProperty("nice", out var count))
        {
            scale = nice(scale, count.ValueKind == JsonValueKind.Number ? count.GetDouble() : 10);
        }

        return scale;
    }

    private static void AssertContinuous(JsonElement item, Adapter scale)
    {
        var inputs = item.Numbers("inputs");
        GoldenFixture.Same(item.Numbers("niceDomain"), scale.Domain, item.ToString());
        GoldenFixture.Same(item.Numbers("outputs"), [.. inputs.Select(scale.Map)], item.ToString());
        GoldenFixture.Same(item.Numbers("inverted"), [.. inputs.Select(scale.Invert)], item.ToString());
        foreach (var ticks in item.GetProperty("ticks").EnumerateArray())
        {
            GoldenFixture.Same(ticks.Numbers("ticks"), scale.Ticks(ticks.Number("count")), $"{item} count {ticks.Number("count")}");
        }

        AssertFormats(item, scale.Format);
    }

    private static void AssertFormats(JsonElement item, Func<double, string?, Func<double, string>> format)
    {
        foreach (var labels in item.GetProperty("formats").EnumerateArray())
        {
            var label = format(labels.Number("count"), labels.OptionalText("specifier"));
            var ticks = labels.Numbers("ticks");
            var expected = labels.GetProperty("labels").EnumerateArray().Select(e => e.GetString()).ToList();
            for (var i = 0; i < ticks.Length; i++)
            {
                Assert.True(expected[i] == label(ticks[i]), $"{item} {labels}: tick {ticks[i]:R} expected \"{expected[i]}\", got \"{label(ticks[i])}\"");
            }
        }
    }

    private sealed record Adapter(Func<double, double> Map, Func<double, double> Invert, Func<double, double[]> Ticks, Func<double, string?, Func<double, string>> Format, double[] Domain);
}
